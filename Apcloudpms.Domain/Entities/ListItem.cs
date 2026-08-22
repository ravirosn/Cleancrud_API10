namespace Apcloudpms.Domain.Entities;

public class ListItem
{
    public int Id { get; set; }
    public int ListItemCategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ListItemCategory ListItemCategory { get; set; } = null!;
    public ICollection<PermitApplication> PermitTypeApplications { get; set; } = new List<PermitApplication>();
    public ICollection<PermitApplication> PermitStatusApplications { get; set; } = new List<PermitApplication>();
    public ICollection<RiskAssessmentHazardCategory> RiskAssessmentHazardCategories { get; set; } =
        new List<RiskAssessmentHazardCategory>();
    public ICollection<RiskAssessmentSpecialPermit> RiskAssessmentSpecialPermits { get; set; } =
        new List<RiskAssessmentSpecialPermit>();
    public ICollection<RiskAssessmentPpe> RiskAssessmentPpeItems { get; set; } =
        new List<RiskAssessmentPpe>();
    public ICollection<RiskAssessmentAdditionalPpe> RiskAssessmentAdditionalPpeItems { get; set; } =
        new List<RiskAssessmentAdditionalPpe>();
    public ICollection<RiskAssessment> RiskAssessmentStatuses { get; set; } =
        new List<RiskAssessment>();
    public ICollection<PermitApplicationInspectionPriorToComm> PermitApplicationInspectionsPriorToComm { get; set; } =
        new List<PermitApplicationInspectionPriorToComm>();
    public ICollection<PermitApplicationWallWorks> PermitApplicationWallWorks { get; set; } =
        new List<PermitApplicationWallWorks>();
    public ICollection<PermitApplicationConfinedSpace> PermitApplicationConfinedSpaces { get; set; } =
        new List<PermitApplicationConfinedSpace>();
}
