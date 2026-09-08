using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.API.Authorization;
using Apcloud.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/modules")]
[Authorize]
[RequireMenu(ApplicationPermissions.OrganizationModule, "Setup", "Module")]
public sealed class ModulesController : ControllerBase
{
    private readonly IModuleAccessService _service;

    public ModulesController(IModuleAccessService service) => _service = service;

    [HttpGet]
    [RequirePermission(ApplicationPermissions.Modules.View)]
    public async Task<ActionResult<ApplicationModulePagedResponseDto>> GetModules(
        [FromQuery] ApplicationModuleQueryDto query,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetModulesAsync(query, cancellationToken));

    [HttpPost]
    [RequirePermission(ApplicationPermissions.Modules.Create)]
    public async Task<ActionResult<ApplicationModuleDto>> CreateModule(
        ApplicationModuleRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateModuleAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(ApplicationPermissions.Modules.Edit)]
    public async Task<ActionResult<ApplicationModuleDto>> UpdateModule(
        int id, ApplicationModuleRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateModuleAsync(id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(ApplicationPermissions.Modules.Delete)]
    public async Task<IActionResult> DeleteModule(
        int id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteModuleAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:int}/configuration")]
    [RequirePermission(ApplicationPermissions.Modules.View)]
    public async Task<ActionResult<ApplicationModuleConfigurationDto>> GetModuleConfiguration(
        int id, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetModuleConfigurationAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}/roles")]
    [RequirePermission(ApplicationPermissions.Modules.Configure)]
    public async Task<ActionResult<ApplicationModuleConfigurationDto>> SetModuleRoles(
        int id, RoleModuleAssignmentRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.SetModuleRolesAsync(id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}/configuration")]
    [RequirePermission(ApplicationPermissions.Modules.Configure)]
    public async Task<ActionResult<ApplicationModuleConfigurationDto>> UpdateModuleConfiguration(
        int id, ApplicationModuleConfigurationRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateModuleConfigurationAsync(id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{moduleId:int}/menus")]
    [RequirePermission(ApplicationPermissions.Modules.View)]
    public async Task<ActionResult<IReadOnlyList<ModuleMenuDto>>> GetMenus(
        int moduleId, bool includeInactive = false,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetMenusAsync(moduleId, includeInactive, cancellationToken));

    [HttpPost("{moduleId:int}/menus")]
    [RequirePermission(ApplicationPermissions.Modules.Create)]
    public async Task<ActionResult<ModuleMenuDto>> CreateMenu(
        int moduleId, ModuleMenuRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateMenuAsync(moduleId, dto, cancellationToken);
        return result is null
            ? NotFound()
            : StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{moduleId:int}/menus/{menuId:int}")]
    [RequirePermission(ApplicationPermissions.Modules.Edit)]
    public async Task<ActionResult<ModuleMenuDto>> UpdateMenu(
        int moduleId, int menuId, ModuleMenuRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateMenuAsync(moduleId, menuId, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{moduleId:int}/roles/{roleId:int}/menus")]
    [RequirePermission(ApplicationPermissions.Modules.View)]
    public async Task<ActionResult<RoleModuleMenuConfigurationDto>> GetRoleModuleMenus(
        int moduleId, int roleId, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetRoleModuleMenusAsync(
            moduleId, roleId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{moduleId:int}/roles/{roleId:int}/menus")]
    [RequirePermission(ApplicationPermissions.Modules.Configure)]
    public async Task<ActionResult<RoleModuleMenuConfigurationDto>> SetRoleModuleMenus(
        int moduleId, int roleId, RoleModuleMenuAssignmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.SetRoleModuleMenusAsync(
            moduleId, roleId, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

}
