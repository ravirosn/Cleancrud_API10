using Apcloudpms.Application.Common;
using Apcloudpms.Application.DTOs;

using Apcloud.Contracts.Common;

namespace Apcloudpms.Application.Interfaces;

public interface IApprovalWorkflowService
{
    Task<IReadOnlyList<ApprovalWorkflowDto>> GetAsync(
        int? permitTypeListItemId = null,
        CancellationToken cancellationToken = default);

    Task<ApprovalWorkflowDto?> SaveAsync(
        int permitTypeListItemId,
        ApprovalWorkflowRequestDto request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<ReturnMessageModel> SubmitRiskAssessmentAsync(
        int riskAssessmentId,
        int userId,
        CancellationToken cancellationToken = default);

    Task<PermitApprovalPagedResponseDto> GetPendingAsync(
        int userId,
        PermitApprovalQueryDto query,
        CancellationToken cancellationToken = default);

    Task<AdminPendingApprovalPagedResponseDto> GetAdminPendingAssignmentsAsync(
        AdminPendingApprovalQueryDto query,
        CancellationToken cancellationToken = default);

    Task<ApprovedPermitPagedResponseDto> GetApprovedAsync(
        int userId,
        PermitApprovalHistoryQueryDto query,
        CancellationToken cancellationToken = default);

    Task<RejectedPermitPagedResponseDto> GetRejectedAsync(
        int userId,
        PermitApprovalHistoryQueryDto query,
        CancellationToken cancellationToken = default);

    Task<ReturnMessageModel> DecideAsync(
        long permitApprovalId,
        ApprovalDecisionRequestDto request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<(int StatusCode, string? Error, IReadOnlyList<AlternateApproverAssignmentDto> Data)>
        AssignAlternateUsersAsync(
            AlternateApproverAssignmentRequestDto request,
            int assignedByUserId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovalNotificationDto>> GetNotificationsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<bool> MarkNotificationReadAsync(
        long notificationId,
        int userId,
        CancellationToken cancellationToken = default);
}

public interface IApprovalNotificationQueue
{
    void Signal();
    ValueTask WaitAsync(CancellationToken cancellationToken);
}
