using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed class PermitApplicationQueryDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    [StringLength(200)]
    public string? Search { get; set; }
}

public sealed record UserPermitApplicationDto(
    long Id,
    string PermitNumber,
    DateOnly IssueDate,
    string PermitIssuerName,
    string PermitReceiverName,
    int PermitTypeListItemId,
    string PermitTypeName,
    int PermitStatusListItemId,
    string PermitStatusName,
    DateTime? SubmittedAtUtc,
    int? CreatedByUserId,
    string CreatedByUserName,
    string? PreRiskAssessmentNumber,
    int? RiskAssessmentId);

public sealed record PermitApplicationPagedResponseDto(
    IReadOnlyList<UserPermitApplicationDto> Data,
    long TotalRecords,
    long TotalPages,
    int PageNumber,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed class PermitApplicationRemarksRequestDto
{
    [StringLength(500)]
    public string? Remarks { get; set; }
}

public sealed record PermitApplicationActionResponseDto(
    long PermitApplicationId,
    string? Remarks,
    int ActionedByUserId,
    DateTime ActionedAtUtc);

public sealed class PermitApplicationUpdateRequestDto
{
    public DateOnly IssueDate { get; set; }

    [Required, StringLength(200)]
    public string PermitIssuerName { get; set; } = string.Empty;

    [StringLength(30)]
    public string? PermitIssuerContactNumber { get; set; }

    [Required, StringLength(200)]
    public string PermitReceiverName { get; set; } = string.Empty;

    [StringLength(30)]
    public string? PermitReceiverContactNumber { get; set; }

    [StringLength(50)]
    public string? PreRiskAssessmentNumber { get; set; }

    [Required, StringLength(500)]
    public string WorkLocation { get; set; } = string.Empty;

    [Required]
    public string WorkDescription { get; set; } = string.Empty;

    public string? SpecialInstructions { get; set; }

    [StringLength(200)]
    public string? WorkHeightBelowSurface { get; set; }

    [StringLength(500)]
    public string? CompletionOfWorks { get; set; }

    public List<PermitApplicationUpdateSelectionDto> InspectionPriorToCommencement { get; set; } = [];
    public List<PermitApplicationUpdateSelectionDto> WorksOnWall { get; set; } = [];
    public List<PermitApplicationUpdateSelectionDto> WorkingInConfinedSpace { get; set; } = [];
}

public sealed class PermitApplicationUpdateSelectionDto
{
    [Range(1, int.MaxValue)]
    public int ListItemId { get; set; }

    public bool IsSelected { get; set; }
}

public sealed record PermitApplicationUpdateResponseDto(
    long PermitApplicationId,
    int PermitStatusListItemId,
    string PermitStatusSystemName,
    DateTime UpdatedAtUtc);

public enum PermitApplicationUpdateOutcome
{
    Success,
    NotFound,
    NotEditable,
    UnsupportedPermitType,
    InvalidSelections,
    StatusNotConfigured
}

public sealed record PermitApplicationUpdateResult(
    PermitApplicationUpdateOutcome Outcome,
    PermitApplicationUpdateResponseDto? Value = null,
    string? Message = null);

public sealed record PermitApplicationDetailsDto(
    long Id,
    int? RiskAssessmentId,
    string PermitNumber,
    DateOnly IssueDate,
    string PermitIssuerName,
    string? PermitIssuerContactNumber,
    string PermitReceiverName,
    string? PermitReceiverContactNumber,
    string? PreRiskAssessmentNumber,
    string WorkLocation,
    string WorkDescription,
    string? SpecialInstructions,
    string? WorkHeightBelowSurface,
    int PermitTypeListItemId,
    string PermitTypeSystemName,
    string PermitTypeName,
    int PermitStatusListItemId,
    string PermitStatusSystemName,
    string PermitStatusName,
    DateTime? SubmittedAtUtc,
    int? CreatedByUserId,
    int? UpdatedByUserId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string? CompletionOfWorks,
    int? CompletionApprovedBy,
    DateTime? CompletionDate,
    string? CompletionRemarks,
    int? CancelledBy,
    DateTime? CancelledDate,
    string? CancelledRemarks,
    IReadOnlyList<PermitApplicationListItemSelectionDto> InspectionPriorToCommencement,
    IReadOnlyList<PermitApplicationListItemSelectionDto> WorksOnWall,
    IReadOnlyList<PermitApplicationListItemSelectionDto> WorkingInConfinedSpace);

public sealed record PermitApplicationListItemSelectionDto(
    int ListItemId,
    string SystemName,
    string Name,
    string? Description,
    int DisplayOrder,
    bool IsSelected);
