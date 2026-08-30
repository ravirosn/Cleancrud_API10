using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/modules")]
[Authorize]
public sealed class ModulesController : ControllerBase
{
    private readonly IModuleAccessService _service;

    public ModulesController(IModuleAccessService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApplicationModulePagedResponseDto>> GetModules(
        [FromQuery] ApplicationModuleQueryDto query,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetModulesAsync(query, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApplicationModuleDto>> CreateModule(
        ApplicationModuleRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateModuleAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApplicationModuleDto>> UpdateModule(
        int id, ApplicationModuleRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateModuleAsync(id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteModule(
        int id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteModuleAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:int}/configuration")]
    public async Task<ActionResult<ApplicationModuleConfigurationDto>> GetModuleConfiguration(
        int id, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetModuleConfigurationAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}/roles")]
    public async Task<ActionResult<ApplicationModuleConfigurationDto>> SetModuleRoles(
        int id, RoleModuleAssignmentRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.SetModuleRolesAsync(id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}/configuration")]
    public async Task<ActionResult<ApplicationModuleConfigurationDto>> UpdateModuleConfiguration(
        int id, ApplicationModuleConfigurationRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateModuleConfigurationAsync(id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{moduleId:int}/menus")]
    public async Task<ActionResult<IReadOnlyList<ModuleMenuDto>>> GetMenus(
        int moduleId, bool includeInactive = false,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetMenusAsync(moduleId, includeInactive, cancellationToken));

    [HttpPost("{moduleId:int}/menus")]
    public async Task<ActionResult<ModuleMenuDto>> CreateMenu(
        int moduleId, ModuleMenuRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateMenuAsync(moduleId, dto, cancellationToken);
        return result is null
            ? NotFound()
            : StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{moduleId:int}/menus/{menuId:int}")]
    public async Task<ActionResult<ModuleMenuDto>> UpdateMenu(
        int moduleId, int menuId, ModuleMenuRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateMenuAsync(moduleId, menuId, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{moduleId:int}/roles/{roleId:int}/menus")]
    public async Task<ActionResult<RoleModuleMenuConfigurationDto>> GetRoleModuleMenus(
        int moduleId, int roleId, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetRoleModuleMenusAsync(
            moduleId, roleId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{moduleId:int}/roles/{roleId:int}/menus")]
    public async Task<ActionResult<RoleModuleMenuConfigurationDto>> SetRoleModuleMenus(
        int moduleId, int roleId, RoleModuleMenuAssignmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.SetRoleModuleMenusAsync(
            moduleId, roleId, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

}
