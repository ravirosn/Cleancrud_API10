namespace Apcloudpms.Domain.Entities;

public class PermitApplicationWallWorks
{
    public long PermitApplicationId { get; set; }
    public int WorksonWallListItemId { get; set; }
    public bool IsSelected { get; set; } = true;

    public PermitApplication PermitApplication { get; set; } = null!;
    public ListItem WorksonWallListItem { get; set; } = null!;
}
