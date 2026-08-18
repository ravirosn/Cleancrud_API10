namespace CleanCrud.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? EntityKey { get; set; }
    public string? ChangedColumns { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public int? ChangedByUserId { get; set; }
    public string? ChangedBy { get; set; }
    public string? TraceId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}
