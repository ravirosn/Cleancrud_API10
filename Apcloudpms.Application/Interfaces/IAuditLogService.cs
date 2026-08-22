using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IAuditLogService
{
    Task<AuditLogPagedResponseDto> GetPagedAsync(
        AuditLogQueryDto query,
        CancellationToken cancellationToken = default);
}
