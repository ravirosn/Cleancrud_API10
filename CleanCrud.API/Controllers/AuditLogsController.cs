using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCrud.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public sealed class AuditLogsController(IAuditLogService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AuditLogPagedResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditLogPagedResponseDto>> GetPaged(
        [FromQuery] AuditLogQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPagedAsync(query, cancellationToken);
        return Ok(result);
    }
}
