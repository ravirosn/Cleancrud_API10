using System.Data;
using Apcloudpms.Application.Common;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Domain.Entities;
using Apcloudpms.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using Apcloud.Contracts.Common;

namespace Apcloudpms.Infrastructure.Services;

public sealed class ApprovalWorkflowService(
    AppDbContext context,
    IApprovalNotificationQueue notificationQueue) : IApprovalWorkflowService
{
    private const string PermitTypeCategory = "PERMIT_TYPE";
    private const string PermitStatusCategory = "PERMIT_STATUS";
    private const string RiskStatusCategory = "RISK_ASSESSMENT_STATUS";
    private const string RiskAssessmentApproved = "APPROVED";
    private const string RiskAssessmentDraft = "DRAFT";
    private const string RiskSubmitted = "SUBMITTED_FOR_APPROVAL";
    private const string PermitFinalized = "FINALIZED_FOR_APPROVAL";
    private const string PermitSubmitted = "PERMIT_SUBMITTED_FOR_APPROVAL";
    private const string PermitApproved = "PERMIT_APPROVED";
    private const string PermitRejected = "PERMIT_REJECTED";

    public async Task<IReadOnlyList<ApprovalWorkflowDto>> GetAsync(
        int? permitTypeListItemId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.ApprovalWorkflows.AsNoTracking()
            .Include(x => x.PermitTypeListItem)
            .Include(x => x.Levels).ThenInclude(x => x.PrimaryApproverRole)
            .Include(x => x.Levels).ThenInclude(x => x.AlternateApproverRole)
            .AsSplitQuery();

        if (permitTypeListItemId.HasValue)
            query = query.Where(x => x.PermitTypeListItemId == permitTypeListItemId.Value);

        var workflows = await query.OrderBy(x => x.PermitTypeListItem.Name)
            .ToListAsync(cancellationToken);
        return workflows.Select(ToDto).ToList();
    }

    public async Task<ApprovalWorkflowDto?> SaveAsync(
        int permitTypeListItemId,
        ApprovalWorkflowRequestDto request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        ValidateLevels(request.Levels);

        var permitTypeExists = await context.ListItems.AnyAsync(x =>
            x.Id == permitTypeListItemId && x.IsActive &&
            x.ListItemCategory.Code == PermitTypeCategory, cancellationToken);
        if (!permitTypeExists)
            return null;

        var roleIds = request.Levels
            .SelectMany(x => new int?[] { x.PrimaryApproverRoleId, x.AlternateApproverRoleId })
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var validRoleCount = await context.Roles.CountAsync(
            x => roleIds.Contains(x.Id) && x.IsActive, cancellationToken);
        if (validRoleCount != roleIds.Count)
            throw new ArgumentException("Every primary and alternate role must be an active role.");

        var workflow = await context.ApprovalWorkflows
            .Include(x => x.Levels)
            .SingleOrDefaultAsync(x => x.PermitTypeListItemId == permitTypeListItemId,
                cancellationToken);
        var now = DateTime.UtcNow;
        if (workflow is null)
        {
            workflow = new ApprovalWorkflow
            {
                PermitTypeListItemId = permitTypeListItemId,
                CreatedByUserId = userId,
                CreatedAtUtc = now
            };
            context.ApprovalWorkflows.Add(workflow);
        }
        else
        {
            context.ApprovalWorkflowLevels.RemoveRange(workflow.Levels);
            workflow.UpdatedAtUtc = now;
        }

        workflow.Name = request.Name.Trim();
        workflow.IsActive = request.IsActive;
        workflow.Levels = request.Levels.OrderBy(x => x.LevelNumber).Select(x =>
            new ApprovalWorkflowLevel
            {
                LevelNumber = x.LevelNumber,
                PrimaryApproverRoleId = x.PrimaryApproverRoleId,
                AlternateApproverRoleId = x.AlternateApproverRoleId
            }).ToList();

        await context.SaveChangesAsync(cancellationToken);

        return (await GetAsync(permitTypeListItemId, cancellationToken)).Single();
    }

    public async Task<ReturnMessageModel> SubmitRiskAssessmentAsync(
        int riskAssessmentId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var result = Failure(
            "Risk assessment was not found.",
            StatusCodes.Status404NotFound);
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var risk = await context.RiskAssessments
                .Include(x => x.RiskAssessmentStatusListItem).ThenInclude(x => x.ListItemCategory)
                .Include(x => x.PermitApplications)
                .SingleOrDefaultAsync(x => x.Id == riskAssessmentId, cancellationToken);
            if (risk is null)
                return;
            if (!IsStatus(risk.RiskAssessmentStatusListItem, RiskStatusCategory, RiskAssessmentDraft))
            {
                result = Failure(
                    "Only a Draft risk assessment can be submitted.",
                    StatusCodes.Status409Conflict);
                return;
            }
            if (risk.PermitApplications.Count == 0)
            {
                result = Failure(
                    "The risk assessment has no child permit applications.",
                    StatusCodes.Status409Conflict);
                return;
            }

            var permitTypeIds = risk.PermitApplications.Select(x => x.PermitTypeListItemId)
                .Distinct().ToList();
            var workflows = await context.ApprovalWorkflows
                .Include(x => x.Levels)
                .Where(x => x.IsActive && permitTypeIds.Contains(x.PermitTypeListItemId))
                .ToDictionaryAsync(x => x.PermitTypeListItemId, cancellationToken);
            var missingTypeIds = permitTypeIds.Where(x => !workflows.ContainsKey(x)).ToArray();
            if (missingTypeIds.Length > 0)
            {
                result = Failure(
                    $"Active approval workflow is missing for permit type id(s): {string.Join(", ", missingTypeIds)}.",
                    StatusCodes.Status409Conflict);
                return;
            }

            var riskSubmittedId = await GetStatusIdAsync(
                RiskStatusCategory, RiskSubmitted, cancellationToken);
            var permitFinalizedId = await GetStatusIdAsync(
                PermitStatusCategory, PermitFinalized, cancellationToken);
            var permitSubmittedId = await GetStatusIdAsync(
                PermitStatusCategory, PermitSubmitted, cancellationToken);
            if (risk.PermitApplications.Any(x => x.PermitStatusListItemId != permitFinalizedId))
            {
                result = Failure(
                    "Every related permit application must be in FINALIZED_FOR_APPROVAL status.",
                    StatusCodes.Status409Conflict);
                return;
            }

            var now = DateTime.UtcNow;
            risk.RiskAssessmentStatusListItemId = riskSubmittedId;
            risk.ModifiedBy = userId;
            risk.UpdatedAtUtc = now;

            var firstApprovals = new List<PermitApproval>();
            foreach (var permit in risk.PermitApplications)
            {
                permit.PermitStatusListItemId = permitSubmittedId;
                permit.SubmittedAtUtc = now;
                permit.UpdatedByUserId = userId;
                permit.UpdatedAtUtc = now;

                foreach (var level in workflows[permit.PermitTypeListItemId].Levels
                             .OrderBy(x => x.LevelNumber))
                {
                    var approval = new PermitApproval
                    {
                        PermitApplicationId = permit.Id,
                        LevelNumber = level.LevelNumber,
                        PrimaryApproverRoleId = level.PrimaryApproverRoleId,
                        AlternateApproverRoleId = level.AlternateApproverRoleId,
                        Status = level.LevelNumber == 1 ? ApprovalState.Pending : ApprovalState.Waiting,
                        CreatedAtUtc = now
                    };
                    context.PermitApprovals.Add(approval);
                    if (level.LevelNumber == 1)
                        firstApprovals.Add(approval);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            foreach (var approval in firstApprovals)
                await AddNotificationsAsync(approval, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            result = Success(
                "Risk assessment and child permits were submitted for approval.");
        });

        if (result.IsSuccess)
            notificationQueue.Signal();
        return result;
    }

    public async Task<PermitApprovalPagedResponseDto> GetPendingAsync(
        int userId,
        PermitApprovalQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var roleIds = await context.UserRoles.Where(x => x.UserId == userId && x.IsActive)
            .Select(x => x.RoleId).ToListAsync(cancellationToken);

        var pendingQuery = context.PermitApprovals.AsNoTracking()
            .Where(x => x.Status == ApprovalState.Pending &&
                (roleIds.Contains(x.PrimaryApproverRoleId) ||
                 (x.AlternateApproverRoleId.HasValue && roleIds.Contains(x.AlternateApproverRoleId.Value)) ||
                 x.AssignedUsers.Any(a => a.UserId == userId && a.IsActive)));

        var totalRecords = await pendingQuery.LongCountAsync(cancellationToken);
        var offset = ((long)query.PageNumber - 1) * query.PageSize;
        var data = offset >= totalRecords
            ? []
            : await pendingQuery
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Skip((int)offset)
            .Take(query.PageSize)
            .Select(x => new PermitApprovalDto(
                x.Id, x.PermitApplicationId, x.PermitApplication.PermitNumber,
                 x.PermitApplication.PermitTypeListItem.Name, x.LevelNumber, x.Status,
                 x.PrimaryApproverRole.Name, x.AlternateApproverRole == null ? null : x.AlternateApproverRole.Name,
                 x.AssignedUsers.Where(a => a.IsActive).OrderBy(a => a.User.DisplayName ?? a.User.UserName)
                     .Select(a => new AssignedApproverUserDto(
                         a.UserId, a.User.UserName, a.User.DisplayName, a.User.Email)).ToList(),
                 x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var totalPages = totalRecords == 0
            ? 0
            : (totalRecords + query.PageSize - 1L) / query.PageSize;
        return new PermitApprovalPagedResponseDto(
            data,
            totalRecords,
            totalPages,
            query.PageNumber,
            query.PageSize,
            query.PageNumber > 1,
            query.PageNumber < totalPages);
    }

    public async Task<AdminPendingApprovalPagedResponseDto> GetAdminPendingAssignmentsAsync(
        AdminPendingApprovalQueryDto query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var search = Normalize(query.Search);
        var hasLevelSearch = byte.TryParse(search, out var searchedLevel) &&
            searchedLevel is >= 1 and <= 5;

        var pendingQuery = context.PermitApprovals.AsNoTracking()
            .Where(x => x.Status == ApprovalState.Pending);
        if (search is not null)
        {
            pendingQuery = pendingQuery.Where(x =>
                x.PermitApplication.PermitNumber.Contains(search) ||
                (x.PermitApplication.PreRiskAssessmentNumber != null &&
                    x.PermitApplication.PreRiskAssessmentNumber.Contains(search)) ||
                (x.PermitApplication.RiskAssessment != null &&
                    x.PermitApplication.RiskAssessment.PreRiskAssessmentNumber.Contains(search)) ||
                x.PermitApplication.PermitIssuerName.Contains(search) ||
                x.PermitApplication.PermitReceiverName.Contains(search) ||
                x.PermitApplication.PermitTypeListItem.Name.Contains(search) ||
                x.PermitApplication.PermitStatusListItem.Name.Contains(search) ||
                x.PrimaryApproverRole.Name.Contains(search) ||
                (x.AlternateApproverRole != null &&
                    x.AlternateApproverRole.Name.Contains(search)) ||
                x.AssignedUsers.Any(a => a.IsActive &&
                    (a.User.UserName.Contains(search) ||
                     (a.User.DisplayName != null && a.User.DisplayName.Contains(search)) ||
                     (a.User.Email != null && a.User.Email.Contains(search)))) ||
                (hasLevelSearch && x.LevelNumber == searchedLevel));
        }

        var totalRecords = await pendingQuery.LongCountAsync(cancellationToken);
        var offset = ((long)query.PageNumber - 1) * query.PageSize;
        var data = offset >= totalRecords
            ? []
            : await pendingQuery
                .OrderByDescending(x =>
                    x.AssignedUsers.Where(a => a.IsActive)
                        .Select(a => (DateTime?)a.AssignedAtUtc).Max() ??
                    x.Notifications.Select(n => (DateTime?)n.CreatedAtUtc).Max() ??
                    x.CreatedAtUtc)
                .ThenByDescending(x => x.Id)
                .Skip((int)offset)
                .Take(query.PageSize)
                .Select(x => new AdminPendingApprovalDto(
                    x.Id,
                    x.PermitApplication.RiskAssessmentId,
                    x.PermitApplication.RiskAssessment == null
                        ? x.PermitApplication.PreRiskAssessmentNumber
                        : x.PermitApplication.RiskAssessment.PreRiskAssessmentNumber,
                    x.PermitApplication.RiskAssessment == null
                        ? null
                        : x.PermitApplication.RiskAssessment.RiskAssessmentStatusListItem.Name,
                    x.PermitApplicationId,
                    x.PermitApplication.PermitNumber,
                    x.PermitApplication.PermitTypeListItem.Name,
                    x.PermitApplication.PermitStatusListItem.Name,
                    x.LevelNumber,
                    x.Status,
                    x.PrimaryApproverRoleId,
                    x.PrimaryApproverRole.Name,
                    x.AlternateApproverRoleId,
                    x.AlternateApproverRole == null ? null : x.AlternateApproverRole.Name,
                    x.AssignedUsers.Where(a => a.IsActive)
                        .OrderByDescending(a => a.AssignedAtUtc)
                        .ThenBy(a => a.User.DisplayName ?? a.User.UserName)
                        .Select(a => new AdminAssignedApproverUserDto(
                            a.UserId,
                            a.User.UserName,
                            a.User.DisplayName,
                            a.User.Email,
                            a.AssignedAtUtc,
                            a.AssignedByUserId,
                            a.AssignedByUser.UserName))
                        .ToList(),
                    x.AssignedUsers.Where(a => a.IsActive)
                        .Select(a => (DateTime?)a.AssignedAtUtc).Max() ??
                    x.Notifications.Select(n => (DateTime?)n.CreatedAtUtc).Max() ??
                    x.CreatedAtUtc))
                .ToListAsync(cancellationToken);

        var totalPages = GetTotalPages(totalRecords, query.PageSize);
        return new AdminPendingApprovalPagedResponseDto(
            data,
            totalRecords,
            totalPages,
            query.PageNumber,
            query.PageSize,
            totalRecords > 0 && query.PageNumber > 1,
            query.PageNumber < totalPages);
    }

    public async Task<ApprovedPermitPagedResponseDto> GetApprovedAsync(
        int userId,
        PermitApprovalHistoryQueryDto query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var result = await GetDecisionHistoryAsync(
            userId, ApprovalState.Approved, query, cancellationToken);
        var data = result.Items.Select(x => new ApprovedPermitDto(
            x.PreRiskAssessmentNumber,
            x.PermitNumber,
            x.IssuedDate,
            x.PermitIssuerName,
            x.PermitReceiverName,
            x.PermitType,
            x.PermitStatus,
            x.DecisionDate,
            x.Remarks)).ToList();
        var totalPages = GetTotalPages(result.TotalRecords, query.PageSize);
        return new ApprovedPermitPagedResponseDto(
            data,
            result.TotalRecords,
            totalPages,
            query.PageNumber,
            query.PageSize,
            result.TotalRecords > 0 && query.PageNumber > 1,
            query.PageNumber < totalPages);
    }

    public async Task<RejectedPermitPagedResponseDto> GetRejectedAsync(
        int userId,
        PermitApprovalHistoryQueryDto query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var result = await GetDecisionHistoryAsync(
            userId, ApprovalState.Rejected, query, cancellationToken);
        var data = result.Items.Select(x => new RejectedPermitDto(
            x.PreRiskAssessmentNumber,
            x.PermitNumber,
            x.IssuedDate,
            x.PermitIssuerName,
            x.PermitReceiverName,
            x.PermitType,
            x.PermitStatus,
            x.DecisionDate,
            x.Remarks)).ToList();
        var totalPages = GetTotalPages(result.TotalRecords, query.PageSize);
        return new RejectedPermitPagedResponseDto(
            data,
            result.TotalRecords,
            totalPages,
            query.PageNumber,
            query.PageSize,
            result.TotalRecords > 0 && query.PageNumber > 1,
            query.PageNumber < totalPages);
    }

    public async Task<ReturnMessageModel> DecideAsync(
        long permitApprovalId,
        ApprovalDecisionRequestDto request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var result = Failure("Approval was not found.", StatusCodes.Status404NotFound);
        var strategy = context.Database.CreateExecutionStrategy();
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                var approval = await context.PermitApprovals
                    .Include(x => x.PermitApplication).ThenInclude(x => x.RiskAssessment)
                    .SingleOrDefaultAsync(x => x.Id == permitApprovalId, cancellationToken);
                if (approval is null)
                    return;
                if (approval.Status != ApprovalState.Pending)
                {
                    result = Failure(
                        "This approval is no longer pending.",
                        StatusCodes.Status409Conflict);
                    return;
                }

                var eligibleByRole = await context.UserRoles.AnyAsync(x =>
                    x.UserId == userId && x.IsActive &&
                    (x.RoleId == approval.PrimaryApproverRoleId ||
                     x.RoleId == approval.AlternateApproverRoleId), cancellationToken);
                var eligibleByAssignment = !eligibleByRole &&
                    await context.PermitApprovalAssignees.AnyAsync(x =>
                        x.PermitApprovalId == approval.Id && x.UserId == userId && x.IsActive,
                        cancellationToken);
                if (!eligibleByRole && !eligibleByAssignment)
                {
                    result = Failure(
                        "The user is not assigned to an approver role or directly assigned to this level.",
                        StatusCodes.Status403Forbidden);
                    return;
                }

                var now = DateTime.UtcNow;
                var decision = request.Decision.Trim().ToUpperInvariant();
                approval.Status = decision;
                approval.ActionedByUserId = userId;
                approval.Comments = string.IsNullOrWhiteSpace(request.Comments) ? null : request.Comments.Trim();
                approval.ActionedAtUtc = now;

                if (decision == ApprovalState.Rejected)
                {
                    var rejectedPermitId = await GetStatusIdAsync(
                        PermitStatusCategory, PermitRejected, cancellationToken);
                    var rejectedRiskId = await GetStatusIdAsync(
                        RiskStatusCategory, PermitRejected, cancellationToken);
                    approval.PermitApplication.PermitStatusListItemId = rejectedPermitId;
                    approval.PermitApplication.UpdatedAtUtc = now;
                    approval.PermitApplication.UpdatedByUserId = userId;
                    if (approval.PermitApplication.RiskAssessment is not null)
                    {
                        approval.PermitApplication.RiskAssessment.RiskAssessmentStatusListItemId = rejectedRiskId;
                        approval.PermitApplication.RiskAssessment.ModifiedBy = userId;
                        approval.PermitApplication.RiskAssessment.UpdatedAtUtc = now;
                        var riskId = approval.PermitApplication.RiskAssessment.Id;
                        var openApprovals = await context.PermitApprovals
                            .Where(x => x.PermitApplication.RiskAssessmentId == riskId &&
                                (x.Status == ApprovalState.Pending || x.Status == ApprovalState.Waiting))
                            .ToListAsync(cancellationToken);
                        foreach (var open in openApprovals.Where(x => x.Id != approval.Id))
                            open.Status = ApprovalState.Cancelled;
                    }
                }
                else
                {
                    var next = await context.PermitApprovals
                        .Where(x => x.PermitApplicationId == approval.PermitApplicationId &&
                            x.LevelNumber > approval.LevelNumber && x.Status == ApprovalState.Waiting)
                        .OrderBy(x => x.LevelNumber).FirstOrDefaultAsync(cancellationToken);
                    if (next is not null)
                    {
                        next.Status = ApprovalState.Pending;
                        await AddNotificationsAsync(next, cancellationToken);
                    }
                    else
                    {
                        var approvedPermitId = await GetStatusIdAsync(
                            PermitStatusCategory, PermitApproved, cancellationToken);
                        approval.PermitApplication.PermitStatusListItemId = approvedPermitId;
                        approval.PermitApplication.UpdatedAtUtc = now;
                        approval.PermitApplication.UpdatedByUserId = userId;

                        if (approval.PermitApplication.RiskAssessment is not null)
                        {
                            var riskId = approval.PermitApplication.RiskAssessment.Id;
                            var allOtherApproved = await context.PermitApplications
                                .Where(x => x.RiskAssessmentId == riskId &&
                                    x.Id != approval.PermitApplicationId)
                                .AllAsync(x => x.PermitStatusListItemId == approvedPermitId,
                                    cancellationToken);
                            if (allOtherApproved)
                            {
                                var approvedRiskId = await GetStatusIdAsync(
                                    RiskStatusCategory, RiskAssessmentApproved, cancellationToken);
                                approval.PermitApplication.RiskAssessment.RiskAssessmentStatusListItemId = approvedRiskId;
                                approval.PermitApplication.RiskAssessment.ModifiedBy = userId;
                                approval.PermitApplication.RiskAssessment.UpdatedAtUtc = now;
                            }
                        }
                    }
                }

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                result = Success("Approval decision recorded.");
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            result = Failure(
                "This approval was already actioned by another approver.",
                StatusCodes.Status409Conflict);
        }

        if (result.IsSuccess)
            notificationQueue.Signal();
        return result;
    }

    public async Task<(int StatusCode, string? Error, IReadOnlyList<AlternateApproverAssignmentDto> Data)>
        AssignAlternateUsersAsync(
            AlternateApproverAssignmentRequestDto request,
            int assignedByUserId,
            CancellationToken cancellationToken = default)
    {
        var hasRisk = request.RiskAssessmentId.HasValue;
        var hasPermit = request.PermitApplicationId.HasValue;
        if (hasRisk == hasPermit)
            return (StatusCodes.Status400BadRequest,
                "Specify exactly one of riskAssessmentId or permitApplicationId.", []);

        var userIds = request.UserIds.Where(x => x > 0).Distinct().ToList();
        if (userIds.Count == 0 || userIds.Count != request.UserIds.Distinct().Count())
            return (StatusCodes.Status400BadRequest,
                "userIds must contain unique positive user IDs.", []);

        var isSuperAdmin = await context.UserRoles.AnyAsync(x =>
            x.UserId == assignedByUserId && x.IsActive && x.Role.IsActive &&
            (x.Role.NormalizedName == "SUPERADMIN" || x.Role.NormalizedName == "ADMIN"),
            cancellationToken);
        if (!isSuperAdmin)
            return (StatusCodes.Status403Forbidden,
                "Only a SuperAdmin can assign alternate approver users.", []);

        (int StatusCode, string? Error, IReadOnlyList<AlternateApproverAssignmentDto> Data) result =
            (StatusCodes.Status500InternalServerError, "Alternate approvers could not be assigned.", []);
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);
            var users = await context.Users.Where(x => userIds.Contains(x.Id) && x.IsActive)
                .OrderBy(x => x.DisplayName ?? x.UserName)
                .ToListAsync(cancellationToken);
            if (users.Count != userIds.Count)
            {
                result = (StatusCodes.Status400BadRequest,
                    "Every assigned approver must be an active user.", []);
                return;
            }

            var approvalsQuery = context.PermitApprovals
                .Include(x => x.PermitApplication)
                .Include(x => x.AssignedUsers)
                .Where(x => x.Status == ApprovalState.Pending &&
                    x.LevelNumber == request.LevelNumber);
            approvalsQuery = hasRisk
                ? approvalsQuery.Where(x =>
                    x.PermitApplication.RiskAssessmentId == request.RiskAssessmentId)
                : approvalsQuery.Where(x =>
                    x.PermitApplicationId == request.PermitApplicationId);

            var approvals = await approvalsQuery.OrderBy(x => x.PermitApplicationId)
                .ToListAsync(cancellationToken);
            if (approvals.Count == 0)
            {
                result = (StatusCodes.Status409Conflict,
                    "No pending approval exists for the selected item and level.", []);
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var approval in approvals)
            {
                foreach (var existing in approval.AssignedUsers.Where(x =>
                             x.IsActive && !userIds.Contains(x.UserId)))
                {
                    existing.IsActive = false;
                    existing.RevokedByUserId = assignedByUserId;
                    existing.RevokedAtUtc = now;
                }

                foreach (var user in users)
                {
                    var assignment = approval.AssignedUsers.SingleOrDefault(x => x.UserId == user.Id);
                    var newlyAssigned = assignment is null || !assignment.IsActive;
                    if (assignment is null)
                    {
                        assignment = new PermitApprovalAssignee
                        {
                            UserId = user.Id,
                            AssignedByUserId = assignedByUserId,
                            AssignedAtUtc = now
                        };
                        approval.AssignedUsers.Add(assignment);
                    }
                    else if (!assignment.IsActive)
                    {
                        assignment.IsActive = true;
                        assignment.AssignedByUserId = assignedByUserId;
                        assignment.AssignedAtUtc = now;
                        assignment.RevokedByUserId = null;
                        assignment.RevokedAtUtc = null;
                    }

                    if (newlyAssigned)
                        await AddNotificationForUserAsync(approval, user.Id, cancellationToken);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var assignedDtos = users.Select(ToAssignedUserDto).ToList();
            var data = approvals.Select(x => new AlternateApproverAssignmentDto(
                x.Id, x.PermitApplicationId, x.PermitApplication.PermitNumber,
                x.PermitApplication.RiskAssessmentId, x.LevelNumber, assignedDtos)).ToList();
            result = (StatusCodes.Status200OK, null, data);
        });

        if (result.StatusCode == StatusCodes.Status200OK)
            notificationQueue.Signal();
        return result;
    }

    public async Task<IReadOnlyList<ApprovalNotificationDto>> GetNotificationsAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await context.ApprovalNotifications.AsNoTracking()
            .Where(x => x.RecipientUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .Select(x => new ApprovalNotificationDto(
                x.Id, x.PermitApprovalId, x.Title, x.Message, x.Status,
                x.CreatedAtUtc, x.SentAtUtc, x.ReadAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<bool> MarkNotificationReadAsync(
        long notificationId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var notification = await context.ApprovalNotifications.SingleOrDefaultAsync(
            x => x.Id == notificationId && x.RecipientUserId == userId, cancellationToken);
        if (notification is null)
            return false;
        notification.ReadAtUtc ??= DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<PermitDecisionHistoryResult> GetDecisionHistoryAsync(
        int userId,
        string approvalStatus,
        PermitApprovalHistoryQueryDto query,
        CancellationToken cancellationToken)
    {
        var connection = (SqlConnection)context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;
        if (shouldCloseConnection)
            await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.SpPermitApprovalHistoryGet";
            command.CommandType = CommandType.StoredProcedure;
            Add(command, "@ActionedByUserId", SqlDbType.Int, userId);
            Add(command, "@ApprovalStatus", SqlDbType.VarChar, approvalStatus, 20);
            Add(command, "@PageNumber", SqlDbType.Int, query.PageNumber);
            Add(command, "@PageSize", SqlDbType.Int, query.PageSize);
            Add(command, "@SearchTerm", SqlDbType.NVarChar, Normalize(query.Search), 200);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException(
                    "dbo.SpPermitApprovalHistoryGet did not return the total record count.");

            var totalRecords = reader.GetInt64(reader.GetOrdinal("TotalRecords"));
            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException(
                    "dbo.SpPermitApprovalHistoryGet did not return the paged records.");

            var items = new List<PermitDecisionHistoryItem>();
            var preRiskNumberOrdinal = reader.GetOrdinal("PreRiskAssessmentNumber");
            var permitNumberOrdinal = reader.GetOrdinal("PermitNumber");
            var issuedDateOrdinal = reader.GetOrdinal("IssuedDate");
            var issuerOrdinal = reader.GetOrdinal("PermitIssuerName");
            var receiverOrdinal = reader.GetOrdinal("PermitReceiverName");
            var permitTypeOrdinal = reader.GetOrdinal("PermitType");
            var permitStatusOrdinal = reader.GetOrdinal("PermitStatus");
            var decisionDateOrdinal = reader.GetOrdinal("DecisionDate");
            var remarksOrdinal = reader.GetOrdinal("Remarks");

            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new PermitDecisionHistoryItem(
                    reader.IsDBNull(preRiskNumberOrdinal)
                        ? null
                        : reader.GetString(preRiskNumberOrdinal),
                    reader.GetString(permitNumberOrdinal),
                    DateOnly.FromDateTime(reader.GetDateTime(issuedDateOrdinal)),
                    reader.GetString(issuerOrdinal),
                    reader.GetString(receiverOrdinal),
                    reader.GetString(permitTypeOrdinal),
                    reader.GetString(permitStatusOrdinal),
                    reader.IsDBNull(decisionDateOrdinal)
                        ? null
                        : reader.GetDateTime(decisionDateOrdinal),
                    reader.IsDBNull(remarksOrdinal) ? null : reader.GetString(remarksOrdinal)));
            }

            return new PermitDecisionHistoryResult(items, totalRecords);
        }
        finally
        {
            if (shouldCloseConnection)
                await context.Database.CloseConnectionAsync();
        }
    }

    private async Task AddNotificationsAsync(
        PermitApproval approval,
        CancellationToken cancellationToken)
    {
        var roleIds = new[] { approval.PrimaryApproverRoleId, approval.AlternateApproverRoleId }
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var recipients = await context.UserRoles
            .Where(x => x.IsActive && x.User.IsActive && roleIds.Contains(x.RoleId))
            .Select(x => x.UserId)
            .Concat(context.PermitApprovalAssignees
                .Where(x => x.PermitApprovalId == approval.Id && x.IsActive && x.User.IsActive)
                .Select(x => x.UserId))
            .Distinct().ToListAsync(cancellationToken);
        var permitNumber = await context.PermitApplications
            .Where(x => x.Id == approval.PermitApplicationId)
            .Select(x => x.PermitNumber).SingleAsync(cancellationToken);

        foreach (var recipient in recipients)
        {
            context.ApprovalNotifications.Add(new ApprovalNotification
            {
                PermitApproval = approval,
                RecipientUserId = recipient,
                Title = $"Permit {permitNumber} requires approval",
                Message = $"Permit {permitNumber} is waiting for level {approval.LevelNumber} approval.",
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private async Task AddNotificationForUserAsync(
        PermitApproval approval,
        int userId,
        CancellationToken cancellationToken)
    {
        var existing = await context.ApprovalNotifications.SingleOrDefaultAsync(x =>
            x.PermitApprovalId == approval.Id && x.RecipientUserId == userId,
            cancellationToken);
        if (existing is not null)
        {
            existing.Status = NotificationState.Pending;
            existing.AttemptCount = 0;
            existing.LastError = null;
            existing.CreatedAtUtc = DateTime.UtcNow;
            existing.SentAtUtc = null;
            existing.ReadAtUtc = null;
            return;
        }

        var permitNumber = approval.PermitApplication?.PermitNumber ??
            await context.PermitApplications.Where(x => x.Id == approval.PermitApplicationId)
                .Select(x => x.PermitNumber).SingleAsync(cancellationToken);
        context.ApprovalNotifications.Add(new ApprovalNotification
        {
            PermitApproval = approval,
            RecipientUserId = userId,
            Title = $"Permit {permitNumber} requires approval",
            Message = $"Permit {permitNumber} is waiting for level {approval.LevelNumber} approval.",
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private static AssignedApproverUserDto ToAssignedUserDto(User user) =>
        new(user.Id, user.UserName, user.DisplayName, user.Email);

    private async Task<int> GetStatusIdAsync(
        string categoryCode,
        string systemName,
        CancellationToken cancellationToken) =>
        await context.ListItems.Where(x => x.IsActive && x.Code == systemName &&
                x.ListItemCategory.Code == categoryCode)
            .Select(x => x.Id).SingleAsync(cancellationToken);

    private static bool IsStatus(ListItem item, string categoryCode, string systemName) =>
        item.Code == systemName && item.ListItemCategory.Code == categoryCode;

    private static long GetTotalPages(long totalRecords, int pageSize) =>
        totalRecords == 0 ? 0 : (totalRecords + pageSize - 1L) / pageSize;

    private static void Add(
        SqlCommand command,
        string name,
        SqlDbType type,
        object? value,
        int? size = null)
    {
        var parameter = new SqlParameter(name, type) { Value = value ?? DBNull.Value };
        if (size.HasValue)
            parameter.Size = size.Value;
        command.Parameters.Add(parameter);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ReturnMessageModel Success(string message) => new()
    {
        IsSuccess = true,
        ReturnMessage = message,
        HttpStatusCode = StatusCodes.Status200OK
    };

    private static ReturnMessageModel Failure(string message, int statusCode) => new()
    {
        IsSuccess = false,
        ReturnMessage = message,
        HttpStatusCode = statusCode
    };

    private static void ValidateLevels(IReadOnlyCollection<ApprovalWorkflowLevelRequestDto> levels)
    {
        var ordered = levels.OrderBy(x => x.LevelNumber).ToArray();
        if (ordered.Length is < 1 or > 5 ||
            ordered.Select(x => (int)x.LevelNumber)
                .SequenceEqual(Enumerable.Range(1, ordered.Length)) == false)
            throw new ArgumentException(
                "A workflow must contain between 1 and 5 sequential levels, starting at level 1, exactly once.");
        if (ordered.Any(x => x.AlternateApproverRoleId == x.PrimaryApproverRoleId))
            throw new ArgumentException("The alternate role must differ from the primary role.");
    }

    private static ApprovalWorkflowDto ToDto(ApprovalWorkflow workflow) => new(
        workflow.Id,
        workflow.PermitTypeListItemId,
        workflow.PermitTypeListItem.Code,
        workflow.PermitTypeListItem.Name,
        workflow.Name,
        workflow.IsActive,
        workflow.Levels.OrderBy(x => x.LevelNumber).Select(x =>
            new ApprovalWorkflowLevelDto(
                x.Id, x.LevelNumber, x.PrimaryApproverRoleId, x.PrimaryApproverRole.Name,
                x.AlternateApproverRoleId, x.AlternateApproverRole?.Name)).ToList());

    private sealed record PermitDecisionHistoryResult(
        IReadOnlyList<PermitDecisionHistoryItem> Items,
        long TotalRecords);

    private sealed record PermitDecisionHistoryItem(
        string? PreRiskAssessmentNumber,
        string PermitNumber,
        DateOnly IssuedDate,
        string PermitIssuerName,
        string PermitReceiverName,
        string PermitType,
        string PermitStatus,
        DateTime? DecisionDate,
        string? Remarks);
}
