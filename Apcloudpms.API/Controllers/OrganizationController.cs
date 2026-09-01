using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/organization")]
[Authorize(Roles = nameof(ApplicationRole.Admin) + "," + nameof(ApplicationRole.SuperAdmin))]
public sealed class OrganizationController : ControllerBase
{
    private readonly IOrganizationService _service;
    public OrganizationController(IOrganizationService service) => _service = service;

    [HttpGet("current")]
    public async Task<ActionResult<OrganizationDetailsDto>> GetCurrentOrganization(
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetCurrentOrganizationAsync(cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrganizationDetailsDto>> GetOrganization(
        int id, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetOrganizationByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrganizationDetailsDto>> UpdateOrganization(
        int id, OrganizationUpdateRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateOrganizationAsync(id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("branches")]
    public async Task<ActionResult<OrganizationPagedResponseDto<OfficeBranchDto>>> GetBranches(
        [FromQuery] OrganizationQueryDto query, CancellationToken cancellationToken = default) =>
        Ok(await _service.GetBranchesAsync(query, cancellationToken));

    [HttpGet("branches/ddl")]
    public async Task<ActionResult<IReadOnlyList<DropdownItemDto>>> GetBranchDropdown(
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetBranchDropdownAsync(cancellationToken));

    [HttpGet("branches/{id:int}")]
    public async Task<ActionResult<OfficeBranchDto>> GetBranchById(
        int id, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetBranchByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

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

    [HttpDelete("branches/{id:int}")]
    public async Task<IActionResult> DeleteBranch(int id, CancellationToken cancellationToken) =>
        await _service.DeleteBranchAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("departments")]
    public async Task<ActionResult<OrganizationPagedResponseDto<DepartmentDto>>> GetDepartments(
        [FromQuery] DepartmentQueryDto query, CancellationToken cancellationToken = default) =>
        Ok(await _service.GetDepartmentsAsync(query, cancellationToken));

    [HttpGet("departments/ddl")]
    public async Task<ActionResult<IReadOnlyList<DropdownItemDto>>> GetDepartmentDropdown(
        int? officeBranchId = null, CancellationToken cancellationToken = default) =>
        Ok(await _service.GetDepartmentDropdownAsync(officeBranchId, cancellationToken));

    [HttpGet("departments/{id:int}")]
    public async Task<ActionResult<DepartmentDto>> GetDepartmentById(
        int id, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetDepartmentByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

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

    [HttpDelete("departments/{id:int}")]
    public async Task<IActionResult> DeleteDepartment(int id, CancellationToken cancellationToken) =>
        await _service.DeleteDepartmentAsync(id, cancellationToken) ? NoContent() : NotFound();
}
