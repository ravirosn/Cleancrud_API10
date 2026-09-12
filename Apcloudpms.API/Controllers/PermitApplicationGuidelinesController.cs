using Apcloud.Contracts.Permissions;
using Apcloudpms.API.Authorization;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/permit-application-guidelines")]
[Authorize]
public sealed class PermitApplicationGuidelinesController(
    IPermitApplicationGuidelineService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(ApplicationPermissions.PermitApplicationGuidelines.View)]
    public async Task<ActionResult<PermitApplicationGuidelinePagedResponseDto>> GetPaged(
        [FromQuery] PermitApplicationGuidelineQueryDto query,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetPagedAsync(query, cancellationToken));

    [HttpGet("permit-types")]
    [RequirePermission(ApplicationPermissions.PermitApplicationGuidelines.View)]
    public async Task<ActionResult<IReadOnlyList<PermitTypeGuidelineOptionDto>>> GetPermitTypes(
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetPermitTypesAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [RequirePermission(ApplicationPermissions.PermitApplicationGuidelines.View)]
    public async Task<ActionResult<PermitApplicationGuidelineDto>> GetById(
        int id, CancellationToken cancellationToken = default)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission(ApplicationPermissions.PermitApplicationGuidelines.Create)]
    public async Task<ActionResult<PermitApplicationGuidelineDto>> Create(
        PermitApplicationGuidelineRequestDto request,
        CancellationToken cancellationToken = default) =>
        StatusCode(StatusCodes.Status201Created,
            await service.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    [RequirePermission(ApplicationPermissions.PermitApplicationGuidelines.Edit)]
    public async Task<ActionResult<PermitApplicationGuidelineDto>> Update(
        int id, PermitApplicationGuidelineRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(ApplicationPermissions.PermitApplicationGuidelines.Delete)]
    public async Task<IActionResult> Deactivate(
        int id, CancellationToken cancellationToken = default) =>
        await service.DeactivateAsync(id, cancellationToken) ? NoContent() : NotFound();
}
