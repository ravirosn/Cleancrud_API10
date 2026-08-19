using CleanCrud.Application.DTOs;

namespace CleanCrud.Application.Interfaces;

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

    Task<ApprovalOperationResult> SubmitRiskAssessmentAsync(
        int riskAssessmentId,
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermitApprovalDto>> GetPendingAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<ApprovalOperationResult> DecideAsync(
        long permitApprovalId,
        ApprovalDecisionRequestDto request,
        int userId,
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
