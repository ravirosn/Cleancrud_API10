namespace Apcloudpms.Application.Interfaces;

public interface IAuditContext
{
    int? UserId { get; }
    string? UserName { get; }
    string? TraceId { get; }
    string? IpAddress { get; }
}
