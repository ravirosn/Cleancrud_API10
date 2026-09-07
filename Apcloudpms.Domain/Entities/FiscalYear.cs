namespace Apcloudpms.Domain.Entities;

public sealed class FiscalYear
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsClosed { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public int? UpdatedByUserId { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public int? ClosedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public FiscalYearSetting Setting { get; set; } = null!;
}
