namespace Apcloudpms.Domain.Entities;

public sealed class FiscalYearSetting
{
    public int FiscalYearId { get; set; }
    public string RaPrefix { get; set; } = string.Empty;
    public string PaPrefix { get; set; } = string.Empty;
    public string NextRaNumber { get; set; } = string.Empty;
    public string NextPaNumber { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public int? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public FiscalYear FiscalYear { get; set; } = null!;
}
