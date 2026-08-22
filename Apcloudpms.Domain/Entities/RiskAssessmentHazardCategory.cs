namespace Apcloudpms.Domain.Entities;

public class RiskAssessmentHazardCategory
{
    public int RiskAssessmentId { get; set; }
    public int HazardCategoriesListItemId { get; set; }
    public bool? IsSelected { get; set; }

    public RiskAssessment RiskAssessment { get; set; } = null!;
    public ListItem HazardCategoryListItem { get; set; } = null!;
}
