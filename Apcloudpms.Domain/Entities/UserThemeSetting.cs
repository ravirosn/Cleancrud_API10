namespace Apcloudpms.Domain.Entities;

public sealed class UserThemeSetting
{
    public int UserId { get; set; }

    public string Mode { get; set; } = "system";

    public string Color { get; set; } = "blue";

    public int Radius { get; set; } = 6;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
