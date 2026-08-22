using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/organization")]
[Authorize(Roles = "Admin")]
public sealed class OrganizationController : ControllerBase
{
    private readonly IOrganizationService _service;
    public OrganizationController(IOrganizationService service) => _service = service;

    [HttpGet("branches")]
    public async Task<ActionResult<IReadOnlyList<OfficeBranchDto>>> GetBranches(
        bool includeInactive = false, CancellationToken cancellationToken = default) =>
        Ok(await _service.GetBranchesAsync(includeInactive, cancellationToken));

    [HttpPost("branches")]
    public async Task<ActionResult<OfficeBranchDto>> CreateBranch(
        OfficeBranchRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateBranchAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("branches/{id:int}")]
    public async Task<ActionResult<OfficeBranchDto>> UpdateBranch(
        int id, OfficeBranchRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateBranchAsync(id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("departments")]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> GetDepartments(
        int? branchId = null, bool includeInactive = false,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetDepartmentsAsync(branchId, includeInactive, cancellationToken));

    [HttpPost("departments")]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment(
        DepartmentRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateDepartmentAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("departments/{id:int}")]
    public async Task<ActionResult<DepartmentDto>> UpdateDepartment(
        int id, DepartmentRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateDepartmentAsync(id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
