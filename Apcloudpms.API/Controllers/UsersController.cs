using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(
    IUserManagementService service,
    IOrganizationService organizationService) : ControllerBase
{
    [HttpGet("offices")]
    public async Task<ActionResult<IReadOnlyList<DropdownItemDto>>> GetOffices(
        CancellationToken cancellationToken = default) =>
        Ok(await organizationService.GetBranchDropdownAsync(cancellationToken));

    [HttpGet("departments")]
    public async Task<ActionResult<IReadOnlyList<DropdownItemDto>>> GetDepartments(
        [FromQuery] int officeBranchId,
        CancellationToken cancellationToken = default)
    {
        if (officeBranchId <= 0)
            return BadRequest(new { Message = "A valid office branch is required." });

        return Ok(await organizationService.GetDepartmentDropdownAsync(
            officeBranchId, cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<UserManagementPagedResponseDto>> GetUsers(
        [FromQuery] UserManagementQueryDto query,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetUsersAsync(query, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<UserManagementDto>> CreateUser(
        UserCreateRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            await service.CreateUserAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserManagementDto>> UpdateUser(
        int id, UserUpdateRequestDto request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateUserAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken) =>
        await service.DeleteUserAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("{id:int}/roles")]
    public async Task<ActionResult<UserRoleConfigurationDto>> GetRoles(
        int id, CancellationToken cancellationToken = default)
    {
        var result = await service.GetUserRolesAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}/roles")]
    public async Task<ActionResult<UserRoleConfigurationDto>> SetRoles(
        int id, UserRolesUpdateRequestDto request, CancellationToken cancellationToken)
    {
        var result = await service.SetUserRolesAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
