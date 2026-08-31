using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.API.Services;
using Apcloudpms.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = nameof(ApplicationRole.Admin) + "," + nameof(ApplicationRole.SuperAdmin))]
public sealed class AuditLogsController(IAuditLogService service) : ControllerBase
{
    [HttpGet("filter-options")]
    [ProducesResponseType<AuditLogFilterOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditLogFilterOptionsDto>> GetFilterOptions(
        CancellationToken cancellationToken) =>
        Ok(await service.GetFilterOptionsAsync(cancellationToken));

    [HttpGet]
    [ProducesResponseType<AuditLogPagedResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditLogPagedResponseDto>> GetPaged(
        [FromQuery] AuditLogQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("export")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> Export(
        [FromQuery] AuditLogQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await service.GetExportAsync(query, cancellationToken);
        if (result.TotalRecords > result.PageSize)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The export contains too many records.",
                Detail = $"Narrow the filters to {result.PageSize:N0} audit records or fewer before exporting."
            });
        }

        var workbook = AuditLogExcelExporter.Create(result.Data);
        return File(
            workbook,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"AuditLogs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx");
    }
}
