using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.API.Authorization;
using Apcloud.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[RequireMenu(ApplicationPermissions.OrganizationModule, "User", "Index")]
public sealed class UsersController(
    IUserManagementService service,
    IOrganizationService organizationService) : ControllerBase
{
    [HttpGet("offices")]
    [RequirePermission(ApplicationPermissions.Users.View)]
    public async Task<ActionResult<IReadOnlyList<DropdownItemDto>>> GetOffices(
        CancellationToken cancellationToken = default) =>
        Ok(await organizationService.GetBranchDropdownAsync(cancellationToken));

    [HttpGet("departments")]
    [RequirePermission(ApplicationPermissions.Users.View)]
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
    [RequirePermission(ApplicationPermissions.Users.View)]
    public async Task<ActionResult<UserManagementPagedResponseDto>> GetUsers(
        [FromQuery] UserManagementQueryDto query,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetUsersAsync(query, cancellationToken));

    [HttpPost]
    [RequirePermission(ApplicationPermissions.Users.Create)]
    public async Task<ActionResult<UserManagementDto>> CreateUser(
        UserCreateRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            await service.CreateUserAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    [RequirePermission(ApplicationPermissions.Users.Edit)]
    public async Task<ActionResult<UserManagementDto>> UpdateUser(
        int id, UserUpdateRequestDto request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateUserAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(ApplicationPermissions.Users.Delete)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken) =>
        await service.DeleteUserAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("{id:int}/roles")]
    [RequirePermission(ApplicationPermissions.Users.View)]
    public async Task<ActionResult<UserRoleConfigurationDto>> GetRoles(
        int id, CancellationToken cancellationToken = default)
    {
        var result = await service.GetUserRolesAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}/roles")]
    [RequirePermission(ApplicationPermissions.Users.AssignRoles)]
    public async Task<ActionResult<UserRoleConfigurationDto>> SetRoles(
        int id, UserRolesUpdateRequestDto request, CancellationToken cancellationToken)
    {
        var result = await service.SetUserRolesAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
