using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/workflow-setup")]
[Authorize]
public sealed class WorkflowSetupController(IWorkflowSetupService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WorkflowSetupPagedResponseDto>> Get(
        [FromQuery] WorkflowSetupQueryDto query, CancellationToken cancellationToken = default) =>
        Ok(await service.GetAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkflowSetupDetailDto>> GetById(
        int id, CancellationToken cancellationToken = default)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowSetupDetailDto>> Create(
        WorkflowSetupRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            await service.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<WorkflowSetupDetailDto>> Update(
        int id, WorkflowSetupRequestDto request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("options/modules")]
    public async Task<IActionResult> GetModules(CancellationToken cancellationToken) =>
        Ok(await service.GetModulesAsync(cancellationToken));

    [HttpGet("options/roles")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken) =>
        Ok(await service.GetRolesAsync(cancellationToken));

    [HttpGet("options/subject-categories")]
    public async Task<IActionResult> GetSubjectCategories(CancellationToken cancellationToken) =>
        Ok(await service.GetSubjectCategoriesAsync(cancellationToken));

    [HttpGet("options/subjects")]
    public async Task<IActionResult> GetSubjects(
        [FromQuery] string categoryCode, CancellationToken cancellationToken) =>
        Ok(await service.GetSubjectsAsync(categoryCode, cancellationToken));
}
