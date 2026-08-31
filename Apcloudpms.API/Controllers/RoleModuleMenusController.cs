using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/role-module-menus")]
[Authorize]
public sealed class RoleModuleMenusController(IRoleModuleMenuManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RoleModuleMenuPagedResponseDto>> Get(
        [FromQuery] RoleModuleMenuQueryDto query,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetAsync(query, cancellationToken));

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RoleModuleMenuRoleOptionDto>>> GetRoles(
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetRoleOptionsAsync(cancellationToken));

    [HttpGet("modules")]
    public async Task<ActionResult<IReadOnlyList<RoleModuleMenuModuleOptionDto>>> GetModules(
        [FromQuery] int roleId, CancellationToken cancellationToken = default) =>
        Ok(await service.GetModuleOptionsAsync(roleId, cancellationToken));

    [HttpGet("menus")]
    public async Task<ActionResult<IReadOnlyList<RoleModuleMenuMenuOptionDto>>> GetMenus(
        [FromQuery] int roleId, [FromQuery] int moduleId,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetMenuOptionsAsync(roleId, moduleId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<RoleModuleMenuManagementDto>> Create(
        RoleModuleMenuManagementRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            await service.CreateAsync(request, cancellationToken));

    [HttpPut("{roleId:int}/{moduleId:int}/{menuId:int}")]
    public async Task<ActionResult<RoleModuleMenuManagementDto>> Update(
        int roleId, int moduleId, int menuId,
        RoleModuleMenuManagementRequestDto request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(
            roleId, moduleId, menuId, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{roleId:int}/{moduleId:int}/{menuId:int}")]
    public async Task<IActionResult> Delete(
        int roleId, int moduleId, int menuId, CancellationToken cancellationToken) =>
        await service.DeleteAsync(roleId, moduleId, menuId, cancellationToken)
            ? NoContent()
            : NotFound();
}
