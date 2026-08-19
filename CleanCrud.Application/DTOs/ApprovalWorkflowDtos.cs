using System.ComponentModel.DataAnnotations;

namespace CleanCrud.Application.DTOs;

public sealed class ApprovalWorkflowRequestDto
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Required, MinLength(1), MaxLength(5)]
    public List<ApprovalWorkflowLevelRequestDto> Levels { get; set; } = [];
}

public sealed class ApprovalWorkflowLevelRequestDto
{
    [Range(1, 5)]
    public byte LevelNumber { get; set; }

    [Range(1, int.MaxValue)]
    public int PrimaryApproverRoleId { get; set; }

    [Range(1, int.MaxValue)]
    public int? AlternateApproverRoleId { get; set; }
}

public sealed record ApprovalWorkflowDto(
    int Id,
    int PermitTypeListItemId,
    string PermitTypeSystemName,
    string PermitTypeName,
    string Name,
    bool IsActive,
    IReadOnlyList<ApprovalWorkflowLevelDto> Levels);

public sealed record ApprovalWorkflowLevelDto(
    int Id,
    byte LevelNumber,
    int PrimaryApproverRoleId,
    string PrimaryApproverRoleName,
    int? AlternateApproverRoleId,
    string? AlternateApproverRoleName);

public sealed class ApprovalDecisionRequestDto
{
    [Required, RegularExpression("^(APPROVED|REJECTED)$")]
    public string Decision { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Comments { get; set; }
}

public sealed record PermitApprovalDto(
    long Id,
    long PermitApplicationId,
    string PermitNumber,
    string PermitType,
    byte LevelNumber,
    string Status,
    string PrimaryRole,
    string? AlternateRole,
    DateTime CreatedAtUtc);

public sealed record ApprovalNotificationDto(
    long Id,
    long PermitApprovalId,
    string Title,
    string Message,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc,
    DateTime? ReadAtUtc);

public enum ApprovalOperationOutcome
{
    Success,
    NotFound,
    NotDraft,
    NoPermitApplications,
    MissingWorkflow,
    NotPending,
    NotEligible
}

public sealed record ApprovalOperationResult(
    ApprovalOperationOutcome Outcome,
    string? Message = null);
