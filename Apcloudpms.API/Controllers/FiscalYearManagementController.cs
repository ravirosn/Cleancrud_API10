using Apcloudpms.API.Authorization;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloud.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/organization/fiscal-years")]
[Authorize]
[RequireMenu(ApplicationPermissions.OrganizationModule, "Organization", "FiscalYears")]
public sealed class FiscalYearManagementController(IOrganizationService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(ApplicationPermissions.FiscalYears.View)]
    public async Task<ActionResult<OrganizationPagedResponseDto<FiscalYearDto>>> Get(
        [FromQuery] FiscalYearQueryDto query, CancellationToken cancellationToken = default) =>
        Ok(await service.GetFiscalYearsAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    [RequirePermission(ApplicationPermissions.FiscalYears.View)]
    public async Task<ActionResult<FiscalYearDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var result = await service.GetFiscalYearByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission(ApplicationPermissions.FiscalYears.Create)]
    public async Task<ActionResult<FiscalYearDto>> Create(FiscalYearRequestDto dto, CancellationToken cancellationToken)
    {
        dto.IsClosed = false;
        var result = await service.CreateFiscalYearAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(ApplicationPermissions.FiscalYears.Edit)]
    public async Task<ActionResult<FiscalYearDto>> Update(int id, FiscalYearRequestDto dto, CancellationToken cancellationToken)
    {
        if (dto.IsClosed) return BadRequest("Use the close endpoint to close a fiscal year.");
        var result = await service.UpdateFiscalYearAsync(id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:int}/close")]
    [RequirePermission(ApplicationPermissions.FiscalYears.Close)]
    public async Task<ActionResult<FiscalYearDto>> Close(int id, FiscalYearRequestDto dto, CancellationToken cancellationToken)
    {
        dto.IsActive = false;
        dto.IsClosed = true;
        var result = await service.UpdateFiscalYearAsync(id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(ApplicationPermissions.FiscalYears.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await service.DeleteFiscalYearAsync(id, cancellationToken) ? NoContent() : NotFound();
}
