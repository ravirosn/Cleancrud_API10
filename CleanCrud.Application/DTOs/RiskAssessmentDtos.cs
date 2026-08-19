using System.ComponentModel.DataAnnotations;

namespace CleanCrud.Application.DTOs;

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
