using CleanCrud.Application.DTOs;

namespace CleanCrud.Application.Interfaces;

public interface IAuditLogService
{
    Task<AuditLogPagedResponseDto> GetPagedAsync(
        AuditLogQueryDto query,
        CancellationToken cancellationToken = default);
}
