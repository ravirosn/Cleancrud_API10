using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCrud.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = "Admin")]
public sealed class RolesController : ControllerBase
{
    private readonly IAccessControlService _service;
    public RolesController(IAccessControlService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles(
        bool includeInactive = false, CancellationToken cancellationToken = default) =>
        Ok(await _service.GetRolesAsync(includeInactive, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<RoleDto>> CreateRole(
        RoleRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateRoleAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoleDto>> UpdateRole(
        int id, RoleRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateRoleAsync(id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("user-assignment")]
    public async Task<IActionResult> SetUserRole(
        UserRoleAssignmentDto dto, CancellationToken cancellationToken)
    {
        var updated = await _service.SetUserRoleAsync(dto, cancellationToken);
        return updated ? NoContent() : NotFound();
    }
}
