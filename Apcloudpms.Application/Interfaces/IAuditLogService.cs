using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IAuditLogService
{
    Task<AuditLogPagedResponseDto> GetPagedAsync(
        AuditLogQueryDto query,
        CancellationToken cancellationToken = default);

    Task<AuditLogPagedResponseDto> GetExportAsync(
        AuditLogQueryDto query,
        CancellationToken cancellationToken = default);

    Task<AuditLogFilterOptionsDto> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default);
}
