using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using CleanCrud.Domain.Entities;
using CleanCrud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CleanCrud.Infrastructure.Services;

public sealed class ApprovalWorkflowService(
    AppDbContext context,
    IApprovalNotificationQueue notificationQueue) : IApprovalWorkflowService
{
    private const string PermitTypeCategory = "PERMIT_TYPE";
    private const string PermitStatusCategory = "PERMIT_STATUS";
    private const string RiskStatusCategory = "RISK_ASSESSMENT_STATUS";
    private const string Draft = "DRAFT";
    private const string RiskSubmitted = "SUBMITTED_FOR_APPROVAL";
    private const string PermitFinalized = "FINALIZED_FOR_APPROVAL";
    private const string PermitSubmitted = "PERMIT_SUBMITTED_FOR_APPROVAL";
    private const string Approved = "APPROVED";
    private const string Rejected = "REJECTED";

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

    public async Task<ApprovalOperationResult> SubmitRiskAssessmentAsync(
        int riskAssessmentId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        ApprovalOperationResult result = new(ApprovalOperationOutcome.NotFound);
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
            if (!IsStatus(risk.RiskAssessmentStatusListItem, RiskStatusCategory, Draft))
            {
                result = new(ApprovalOperationOutcome.NotDraft,
                    "Only a Draft risk assessment can be submitted.");
                return;
            }
            if (risk.PermitApplications.Count == 0)
            {
                result = new(ApprovalOperationOutcome.NoPermitApplications,
                    "The risk assessment has no child permit applications.");
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
                result = new(ApprovalOperationOutcome.MissingWorkflow,
                    $"Active approval workflow is missing for permit type id(s): {string.Join(", ", missingTypeIds)}.");
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
                result = new(ApprovalOperationOutcome.PermitApplicationsNotFinalized,
                    "Every related permit application must be in FINALIZED_FOR_APPROVAL status.");
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
            result = new(ApprovalOperationOutcome.Success);
        });

        if (result.Outcome == ApprovalOperationOutcome.Success)
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
                 (x.AlternateApproverRoleId.HasValue && roleIds.Contains(x.AlternateApproverRoleId.Value))));

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

    public async Task<ApprovalOperationResult> DecideAsync(
        long permitApprovalId,
        ApprovalDecisionRequestDto request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        ApprovalOperationResult result = new(ApprovalOperationOutcome.NotFound);
        var strategy = context.Database.CreateExecutionStrategy();
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
                result = new(ApprovalOperationOutcome.NotPending,
                    "This approval is no longer pending.");
                return;
            }

            var eligible = await context.UserRoles.AnyAsync(x =>
                x.UserId == userId && x.IsActive &&
                (x.RoleId == approval.PrimaryApproverRoleId ||
                 x.RoleId == approval.AlternateApproverRoleId), cancellationToken);
            if (!eligible)
            {
                result = new(ApprovalOperationOutcome.NotEligible,
                    "The user is not assigned to a primary or alternate approver role for this level.");
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
                var rejectedPermitId = await GetStatusIdAsync(PermitStatusCategory, Rejected, cancellationToken);
                var rejectedRiskId = await GetStatusIdAsync(RiskStatusCategory, Rejected, cancellationToken);
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
                    var approvedPermitId = await GetStatusIdAsync(PermitStatusCategory, Approved, cancellationToken);
                    approval.PermitApplication.PermitStatusListItemId = approvedPermitId;
                    approval.PermitApplication.UpdatedAtUtc = now;
                    approval.PermitApplication.UpdatedByUserId = userId;

                    if (approval.PermitApplication.RiskAssessment is not null)
                    {
                        var riskId = approval.PermitApplication.RiskAssessment.Id;
                        var allOtherApproved = await context.PermitApplications
                            .Where(x => x.RiskAssessmentId == riskId && x.Id != approval.PermitApplicationId)
                            .AllAsync(x => x.PermitStatusListItemId == approvedPermitId, cancellationToken);
                        if (allOtherApproved)
                        {
                            var approvedRiskId = await GetStatusIdAsync(RiskStatusCategory, Approved, cancellationToken);
                            approval.PermitApplication.RiskAssessment.RiskAssessmentStatusListItemId = approvedRiskId;
                            approval.PermitApplication.RiskAssessment.ModifiedBy = userId;
                            approval.PermitApplication.RiskAssessment.UpdatedAtUtc = now;
                        }
                    }
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            result = new(ApprovalOperationOutcome.Success);
        });

        if (result.Outcome == ApprovalOperationOutcome.Success)
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

    private async Task AddNotificationsAsync(
        PermitApproval approval,
        CancellationToken cancellationToken)
    {
        var roleIds = new[] { approval.PrimaryApproverRoleId, approval.AlternateApproverRoleId }
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var recipients = await context.UserRoles
            .Where(x => x.IsActive && x.User.IsActive && roleIds.Contains(x.RoleId))
            .Select(x => x.UserId).Distinct().ToListAsync(cancellationToken);
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

    private async Task<int> GetStatusIdAsync(
        string categoryCode,
        string systemName,
        CancellationToken cancellationToken) =>
        await context.ListItems.Where(x => x.IsActive && x.Code == systemName &&
                x.ListItemCategory.Code == categoryCode)
            .Select(x => x.Id).SingleAsync(cancellationToken);

    private static bool IsStatus(ListItem item, string categoryCode, string systemName) =>
        item.Code == systemName && item.ListItemCategory.Code == categoryCode;

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
}
