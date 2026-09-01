using System.ComponentModel.DataAnnotations;

namespace Apcloud.Web.Areas.PermitApplication.Models;

public sealed class RiskAssessmentFormDto
{
    public int? Id { get; set; }
    [Required, StringLength(50)] public string PreRiskAssessmentNumber { get; set; } = string.Empty;
    [Required] public DateOnly IssueDate { get; set; }
    [Required, StringLength(100)] public string PermitIssuerName { get; set; } = string.Empty;
    [Required, StringLength(100)] public string PermitReceiverName { get; set; } = string.Empty;
    [Required, StringLength(100)] public string AreaResponsibleName { get; set; } = string.Empty;
    [Required, StringLength(255)] public string LocationOfWork { get; set; } = string.Empty;
    public string? DescriptionOfWork { get; set; }
    public string? SpecialInstructions { get; set; }
    [StringLength(500)] public string? OtherEquipmentsPpe { get; set; }
    [StringLength(500)] public string? OtherProtectionMeasures { get; set; }
    public DateTime? PlannedStartDateTime { get; set; }
    public DateTime? PlannedEndDateTime { get; set; }
    public List<RiskAssessmentSelectionFormDto> AdditionalPpe { get; set; } = [];
    public List<RiskAssessmentSelectionFormDto> HazardCategories { get; set; } = [];
    public List<RiskAssessmentSelectionFormDto> PersonalProtectiveEquipment { get; set; } = [];
    public List<RiskAssessmentSelectionFormDto> SpecialPermits { get; set; } = [];
}

public sealed class RiskAssessmentSelectionFormDto
{
    [Range(1, int.MaxValue)] public int ListItemId { get; set; }
    public bool IsSelected { get; set; } = true;
}
