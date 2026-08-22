namespace Apcloudpms.Domain.Entities;

public class PermitApplicationConfinedSpace
{
    public long PermitApplicationId { get; set; }
    public int WorkingInConfinedSpaceListItemId { get; set; }
    public bool IsSelected { get; set; } = true;

    public PermitApplication PermitApplication { get; set; } = null!;
    public ListItem WorkingInConfinedSpaceListItem { get; set; } = null!;
}
