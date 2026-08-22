using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

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

public sealed class AlternateApproverAssignmentRequestDto
{
    [Range(1, int.MaxValue)]
    public int? RiskAssessmentId { get; set; }

    [Range(1, long.MaxValue)]
    public long? PermitApplicationId { get; set; }

    [Range(1, 5)]
    public byte LevelNumber { get; set; }

    [Required, MinLength(1), MaxLength(100)]
    public List<int> UserIds { get; set; } = [];
}

public sealed record AlternateApproverAssignmentDto(
    long PermitApprovalId,
    long PermitApplicationId,
    string PermitNumber,
    int? RiskAssessmentId,
    byte LevelNumber,
    IReadOnlyList<AssignedApproverUserDto> AssignedUsers);

public sealed record AssignedApproverUserDto(
    int UserId,
    string UserName,
    string? DisplayName,
    string? Email);

public sealed record PermitApprovalDto(
    long Id,
    long PermitApplicationId,
    string PermitNumber,
    string PermitType,
    byte LevelNumber,
    string Status,
    string PrimaryRole,
    string? AlternateRole,
    IReadOnlyList<AssignedApproverUserDto> AssignedUsers,
    DateTime CreatedAtUtc);

public sealed class PermitApprovalQueryDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}

public sealed record PermitApprovalPagedResponseDto(
    IReadOnlyList<PermitApprovalDto> Data,
    long TotalRecords,
    long TotalPages,
    int PageNumber,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed class AdminPendingApprovalQueryDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    [StringLength(200)]
    public string? Search { get; set; }
}

public sealed record AdminAssignedApproverUserDto(
    int UserId,
    string UserName,
    string? DisplayName,
    string? Email,
    DateTime AssignedAtUtc,
    int AssignedByUserId,
    string AssignedByUserName);

public sealed record AdminPendingApprovalDto(
    long PermitApprovalId,
    int? RiskAssessmentId,
    string? PreRiskAssessmentNumber,
    string? RiskAssessmentStatus,
    long PermitApplicationId,
    string PermitNumber,
    string PermitType,
    string PermitApplicationStatus,
    byte PendingLevel,
    string ApprovalStatus,
    int PrimaryApproverRoleId,
    string PrimaryApproverRoleName,
    int? AlternateApproverRoleId,
    string? AlternateApproverRoleName,
    IReadOnlyList<AdminAssignedApproverUserDto> AssignedUsers,
    DateTime AssignedAtUtc);

public sealed record AdminPendingApprovalPagedResponseDto(
    IReadOnlyList<AdminPendingApprovalDto> Data,
    long TotalRecords,
    long TotalPages,
    int PageNumber,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record ApprovedPermitDto(
    string? PreRiskAssessmentNumber,
    string PermitNumber,
    DateOnly IssuedDate,
    string PermitIssuerName,
    string PermitReceiverName,
    string PermitType,
    string PermitStatus,
    DateTime? ApprovedDate,
    string? ApprovalRemarks);

public sealed record RejectedPermitDto(
    string? PreRiskAssessmentNumber,
    string PermitNumber,
    DateOnly IssuedDate,
    string PermitIssuerName,
    string PermitReceiverName,
    string PermitType,
    string PermitStatus,
    DateTime? RejectedDate,
    string? RejectedReason);

public sealed class PermitApprovalHistoryQueryDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    [StringLength(200)]
    public string? Search { get; set; }
}

public sealed record ApprovedPermitPagedResponseDto(
    IReadOnlyList<ApprovedPermitDto> Data,
    long TotalRecords,
    long TotalPages,
    int PageNumber,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record RejectedPermitPagedResponseDto(
    IReadOnlyList<RejectedPermitDto> Data,
    long TotalRecords,
    long TotalPages,
    int PageNumber,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record ApprovalNotificationDto(
    long Id,
    long PermitApprovalId,
    string Title,
    string Message,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc,
    DateTime? ReadAtUtc);
