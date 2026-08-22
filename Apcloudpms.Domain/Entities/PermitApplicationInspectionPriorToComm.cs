namespace Apcloudpms.Domain.Entities;

public class PermitApplicationInspectionPriorToComm
{
    public long PermitApplicationId { get; set; }
    public int InspectionPriorToCommListItemId { get; set; }
    public bool IsSelected { get; set; } = true;

    public PermitApplication PermitApplication { get; set; } = null!;
    public ListItem InspectionPriorToCommListItem { get; set; } = null!;
}
