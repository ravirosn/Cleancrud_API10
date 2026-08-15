using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCrud.API.Controllers;

[ApiController]
[Route("api/modules")]
[Authorize(Roles = "Admin")]
public sealed class ModulesController : ControllerBase
{
    private readonly IModuleAccessService _service;

    public ModulesController(IModuleAccessService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApplicationModuleDto>>> GetModules(
        bool includeInactive = false, CancellationToken cancellationToken = default) =>
        Ok(await _service.GetModulesAsync(includeInactive, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApplicationModuleDto>> CreateModule(
        ApplicationModuleRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateModuleAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetModules), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApplicationModuleDto>> UpdateModule(
        int id, ApplicationModuleRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateModuleAsync(id, dto, cancellationToken);
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

    [HttpPut("user-assignment")]
    public async Task<IActionResult> SetUserModule(
        UserModuleAssignmentDto dto, CancellationToken cancellationToken)
    {
        var updated = await _service.SetUserModuleAsync(dto, cancellationToken);
        return updated ? NoContent() : NotFound();
    }
}
