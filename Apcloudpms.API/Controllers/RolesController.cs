using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.API.Authorization;
using Apcloud.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
[RequireMenu(ApplicationPermissions.OrganizationModule, "Setup", "Role")]
public sealed class RolesController : ControllerBase
{
    private readonly IAccessControlService _service;
    public RolesController(IAccessControlService service) => _service = service;

    [HttpGet]
    [RequirePermission(ApplicationPermissions.Roles.View)]
    public async Task<ActionResult<RolePagedResponseDto>> GetRoles(
        [FromQuery] RoleQueryDto query, CancellationToken cancellationToken = default) =>
        Ok(await _service.GetRolesAsync(query, cancellationToken));

    [HttpGet("module-options")]
    [RequirePermission(ApplicationPermissions.Roles.View)]
    public async Task<ActionResult<IReadOnlyList<RoleModuleOptionDto>>> GetModuleOptions(
        [FromQuery] int? roleId, CancellationToken cancellationToken = default) =>
        Ok(await _service.GetRoleModuleOptionsAsync(roleId, cancellationToken));

    [HttpPost]
    [RequirePermission(ApplicationPermissions.Roles.Create)]
    public async Task<ActionResult<RoleDto>> CreateRole(
        RoleRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateRoleAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(ApplicationPermissions.Roles.Edit)]
    public async Task<ActionResult<RoleDto>> UpdateRole(
        int id, RoleRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateRoleAsync(id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(ApplicationPermissions.Roles.Delete)]
    public async Task<IActionResult> DeleteRole(
        int id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteRoleAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPut("user-assignment")]
    [RequirePermission(ApplicationPermissions.Roles.AssignUsers)]
    public async Task<IActionResult> SetUserRole(
        UserRoleAssignmentDto dto, CancellationToken cancellationToken)
    {
        var updated = await _service.SetUserRoleAsync(dto, cancellationToken);
        return updated ? NoContent() : NotFound();
    }
}
