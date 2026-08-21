using System.Data;
using CleanCrud.Application.Common;
using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using CleanCrud.Domain.Entities;
using CleanCrud.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CleanCrud.Infrastructure.Services;

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
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var approval = await context.PermitApprovals
                .Include(x => x.PermitApplication).ThenInclude(x => x.RiskAssessment)
                .SingleOrDefaultAsync(x => x.PermitApplicationId == permitApprovalId, cancellationToken);
            if (approval is null)
                return;
            if (approval.Status != ApprovalState.Pending)
            {
                result = Failure(
                    "This approval is no longer pending.",
                    StatusCodes.Status409Conflict);
                return;
            }

            var eligible = await context.UserRoles.AnyAsync(x =>
                x.UserId == userId && x.IsActive &&
                (x.RoleId == approval.PrimaryApproverRoleId ||
                 x.RoleId == approval.AlternateApproverRoleId), cancellationToken);
            if (!eligible)
            {
                result = Failure(
                    "The user is not assigned to a primary or alternate approver role for this level.",
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
                var rejectedPermitId = await GetStatusIdAsync(PermitStatusCategory, PermitRejected, cancellationToken);
                var rejectedRiskId = await GetStatusIdAsync(RiskStatusCategory, PermitRejected, cancellationToken);
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
                    var approvedPermitId = await GetStatusIdAsync(PermitStatusCategory, PermitApproved, cancellationToken);
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
                            var approvedRiskId = await GetStatusIdAsync(RiskStatusCategory, RiskAssessmentApproved, cancellationToken);
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

        if (result.IsSuccess)
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
