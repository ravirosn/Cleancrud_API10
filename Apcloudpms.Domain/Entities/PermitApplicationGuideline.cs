namespace Apcloudpms.Domain.Entities;

public sealed class PermitApplicationGuideline
{
    public int Id { get; set; }
    public int PermitTypeListItemId { get; set; }
    public string Guidelines { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ListItem PermitTypeListItem { get; set; } = null!;
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
}
