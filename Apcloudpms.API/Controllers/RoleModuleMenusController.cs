using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.API.Authorization;
using Apcloud.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/role-module-menus")]
[Authorize]
[RequireMenu(ApplicationPermissions.OrganizationModule, "RoleModuleMenu", "Index")]
public sealed class RoleModuleMenusController(IRoleModuleMenuManagementService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(ApplicationPermissions.RoleModuleMenus.View)]
    public async Task<ActionResult<RoleModuleMenuPagedResponseDto>> Get(
        [FromQuery] RoleModuleMenuQueryDto query,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetAsync(query, cancellationToken));

    [HttpGet("roles")]
    [RequirePermission(ApplicationPermissions.RoleModuleMenus.View)]
    public async Task<ActionResult<IReadOnlyList<RoleModuleMenuRoleOptionDto>>> GetRoles(
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetRoleOptionsAsync(cancellationToken));

    [HttpGet("modules")]
    [RequirePermission(ApplicationPermissions.RoleModuleMenus.View)]
    public async Task<ActionResult<IReadOnlyList<RoleModuleMenuModuleOptionDto>>> GetModules(
        [FromQuery] int roleId, CancellationToken cancellationToken = default) =>
        Ok(await service.GetModuleOptionsAsync(roleId, cancellationToken));

    [HttpGet("menus")]
    [RequirePermission(ApplicationPermissions.RoleModuleMenus.View)]
    public async Task<ActionResult<IReadOnlyList<RoleModuleMenuMenuOptionDto>>> GetMenus(
        [FromQuery] int roleId, [FromQuery] int moduleId,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetMenuOptionsAsync(roleId, moduleId, cancellationToken));

    [HttpPost]
    [RequirePermission(ApplicationPermissions.RoleModuleMenus.Create)]
    public async Task<ActionResult<RoleModuleMenuManagementDto>> Create(
        RoleModuleMenuManagementRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            await service.CreateAsync(request, cancellationToken));

    [HttpPut("{roleId:int}/{moduleId:int}/{menuId:int}")]
    [RequirePermission(ApplicationPermissions.RoleModuleMenus.Edit)]
    public async Task<ActionResult<RoleModuleMenuManagementDto>> Update(
        int roleId, int moduleId, int menuId,
        RoleModuleMenuManagementRequestDto request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(
            roleId, moduleId, menuId, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{roleId:int}/{moduleId:int}/{menuId:int}")]
    [RequirePermission(ApplicationPermissions.RoleModuleMenus.Delete)]
    public async Task<IActionResult> Delete(
        int roleId, int moduleId, int menuId, CancellationToken cancellationToken) =>
        await service.DeleteAsync(roleId, moduleId, menuId, cancellationToken)
            ? NoContent()
            : NotFound();
}
