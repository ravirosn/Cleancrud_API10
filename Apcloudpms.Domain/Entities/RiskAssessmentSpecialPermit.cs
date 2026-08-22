namespace Apcloudpms.Domain.Entities;

public class RiskAssessmentSpecialPermit
{
    public int RiskAssessmentId { get; set; }
    public int SpecialPermitListItemId { get; set; }
    public bool? IsSelected { get; set; }

    public RiskAssessment RiskAssessment { get; set; } = null!;
    public ListItem SpecialPermitListItem { get; set; } = null!;
}
