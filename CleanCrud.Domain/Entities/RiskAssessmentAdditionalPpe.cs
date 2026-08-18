namespace CleanCrud.Domain.Entities;

public class RiskAssessmentAdditionalPpe
{
    public int RiskAssessmentId { get; set; }
    public int AdditionalProtectiveMeasuresListItemId { get; set; }
    public bool? IsSelected { get; set; }

    public RiskAssessment RiskAssessment { get; set; } = null!;
    public ListItem AdditionalProtectiveMeasureListItem { get; set; } = null!;
}
