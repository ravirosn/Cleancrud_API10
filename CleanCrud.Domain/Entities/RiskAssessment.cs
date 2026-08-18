namespace CleanCrud.Domain.Entities;

public class RiskAssessment
{
    public int Id { get; set; }
    public string PreRiskAssessmentNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public string PermitIssuerName { get; set; } = string.Empty;
    public string? PermitIssuerContact { get; set; }
    public string PermitReceiverName { get; set; } = string.Empty;
    public string? PermitReceiverContact { get; set; }
    public string AreaResponsibleName { get; set; } = string.Empty;
    public string? AreaResponsibleContact { get; set; }
    public string LocationOfWork { get; set; } = string.Empty;
    public string? DescriptionOfWork { get; set; }
    public string? SpecialInstructions { get; set; }
    public DateTime? PlannedStartDateTime { get; set; }
    public DateTime? PlannedEndDateTime { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<RiskAssessmentHazardCategory> HazardCategories { get; set; } =
        new List<RiskAssessmentHazardCategory>();
    public ICollection<RiskAssessmentSpecialPermit> SpecialPermits { get; set; } =
        new List<RiskAssessmentSpecialPermit>();
    public ICollection<RiskAssessmentPpe> PersonalProtectiveEquipment { get; set; } =
        new List<RiskAssessmentPpe>();
    public ICollection<RiskAssessmentAdditionalPpe> AdditionalPersonalProtectiveEquipment { get; set; } =
        new List<RiskAssessmentAdditionalPpe>();
}
