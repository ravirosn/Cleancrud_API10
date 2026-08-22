using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed class RiskAssessmentRequestDto
{
    [Required, StringLength(50)]
    public string PreRiskAssessmentNumber { get; set; } = string.Empty;

    public DateOnly IssueDate { get; set; }

    [Required, StringLength(100)]
    public string PermitIssuerName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string PermitReceiverName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string AreaResponsibleName { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string LocationOfWork { get; set; } = string.Empty;

    public string? DescriptionOfWork { get; set; }
    public string? SpecialInstructions { get; set; }

    [StringLength(500)]
    public string? OtherEquipmentsPPE { get; set; }

    [StringLength(500)]
    public string? OtherProtectionMeasures { get; set; }

    public DateTime? PlannedStartDateTime { get; set; }
    public DateTime? PlannedEndDateTime { get; set; }

    public List<RiskAssessmentSelectionDto> AdditionalPpe { get; set; } = [];
    public List<RiskAssessmentSelectionDto> HazardCategories { get; set; } = [];
    public List<RiskAssessmentSelectionDto> PersonalProtectiveEquipment { get; set; } = [];
    public List<RiskAssessmentSelectionDto> SpecialPermits { get; set; } = [];
}

public sealed class RiskAssessmentSelectionDto
{
    [Range(1, int.MaxValue)]
    public int ListItemId { get; set; }

    public bool IsSelected { get; set; } = true;
}

public sealed record RiskAssessmentWriteResponseDto(
    int RiskAssessmentId,
    int RiskAssessmentStatusListItemId,
    string Status,
    DateTime UpdatedAtUtc);

public enum RiskAssessmentWriteOutcome
{
    Success,
    NotFound,
    NotDraft
}

public sealed record RiskAssessmentWriteResult(
    RiskAssessmentWriteOutcome Outcome,
    RiskAssessmentWriteResponseDto? Value = null);

public sealed class RiskAssessmentQueryDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    [StringLength(200)]
    public string? Search { get; set; }
}

public sealed record RiskAssessmentGridItemDto(
    int Id,
    string PreRiskAssessmentNumber,
    DateOnly IssueDate,
    string PermitIssuerName,
    string PermitReceiverName,
    string AreaResponsibleName,
    DateTime? PlannedStartDateTime,
    DateTime? PlannedEndDateTime,
    int RiskAssessmentStatusListItemId,
    string RiskAssessmentStatus);

public sealed record RiskAssessmentDetailsDto(
    int Id,
    string PreRiskAssessmentNumber,
    DateOnly IssueDate,
    string PermitIssuerName,
    string PermitReceiverName,
    string AreaResponsibleName,
    string LocationOfWork,
    string? DescriptionOfWork,
    string? SpecialInstructions,
    string? OtherEquipmentsPPE,
    string? OtherProtectionMeasures,
    DateTime? PlannedStartDateTime,
    DateTime? PlannedEndDateTime,
    int RiskAssessmentStatusListItemId,
    string RiskAssessmentStatus,
    int? CreatedBy,
    int? ModifiedBy,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<RiskAssessmentSelectionDto> AdditionalPpe,
    IReadOnlyList<RiskAssessmentSelectionDto> HazardCategories,
    IReadOnlyList<RiskAssessmentSelectionDto> PersonalProtectiveEquipment,
    IReadOnlyList<RiskAssessmentSelectionDto> SpecialPermits);

public sealed record RiskAssessmentPermitApplicationDto(
    long Id,
    string PermitNumber,
    DateOnly IssueDate,
    string PermitIssuerName,
    string PermitReceiverName,
    int PermitTypeListItemId,
    string PermitTypeName,
    int PermitStatusListItemId,
    string PermitStatusName,
    int? RiskAssessmentId);

public sealed record RiskAssessmentPagedResponseDto(
    IReadOnlyList<RiskAssessmentGridItemDto> Data,
    long TotalRecords,
    long TotalPages,
    int PageNumber,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);
