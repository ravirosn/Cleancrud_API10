using Apcloudpms.API.Middleware;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Apcloudpms.API.Authorization;
using Apcloud.Contracts.Permissions;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/permission-assignments")]
[Authorize]
[RequireMenu(ApplicationPermissions.OrganizationModule, "Setup", "Permission")]
public sealed class PermissionAssignmentsController(IPermissionAssignmentService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(ApplicationPermissions.PermissionAssignments.View)]
    public async Task<ActionResult<PermissionAssignmentPagedResponseDto>> Get([FromQuery] PermissionAssignmentQueryDto query,CancellationToken cancellationToken=default)=>Ok(await service.GetAsync(query,cancellationToken));
    [HttpGet("roles")]
    [RequirePermission(ApplicationPermissions.PermissionAssignments.View)]
    public async Task<ActionResult<IReadOnlyList<PermissionRoleOptionDto>>> Roles(CancellationToken cancellationToken=default)=>Ok(await service.GetRolesAsync(cancellationToken));
    [HttpGet("policies")]
    [RequirePermission(ApplicationPermissions.PermissionAssignments.View)]
    public async Task<ActionResult<IReadOnlyList<PermissionPolicyOptionDto>>> Policies(int roleId,CancellationToken cancellationToken=default)=>Ok(await service.GetPoliciesAsync(roleId,cancellationToken));
    [HttpPost]
    [RequirePermission(ApplicationPermissions.PermissionAssignments.Create)]
    public async Task<ActionResult<PermissionAssignmentDto>> Create(PermissionAssignmentRequestDto request,CancellationToken cancellationToken)=>StatusCode(StatusCodes.Status201Created,await service.CreateAsync(request,cancellationToken));
    [HttpPut("{roleId:int}/{permissionPolicyId:int}")]
    [RequirePermission(ApplicationPermissions.PermissionAssignments.Edit)]
    public async Task<ActionResult<PermissionAssignmentDto>> Update(int roleId,int permissionPolicyId,PermissionAssignmentRequestDto request,CancellationToken cancellationToken)
    {var result=await service.UpdateAsync(roleId,permissionPolicyId,request,cancellationToken);return result is null?NotFound():Ok(result);}
    [HttpPut("bulk/{roleId:int}")]
    [RequirePermission(ApplicationPermissions.PermissionAssignments.Manage)]
    public async Task<ActionResult<BulkPermissionAssignmentResultDto>> SetBulk(
        int roleId, BulkPermissionAssignmentRequestDto request, CancellationToken cancellationToken) =>
        Ok(await service.SetBulkAsync(roleId,request,cancellationToken));
    [HttpDelete("{roleId:int}/{permissionPolicyId:int}")]
    [RequirePermission(ApplicationPermissions.PermissionAssignments.Delete)]
    public async Task<IActionResult> Delete(int roleId,int permissionPolicyId,CancellationToken cancellationToken)=>await service.DeleteAsync(roleId,permissionPolicyId,cancellationToken)?NoContent():NotFound();
}

[ApiController]
[Route("api/permissions")]
[Authorize]
public sealed class CurrentPermissionsController(IPermissionAssignmentService service) : ControllerBase
{
    [HttpGet("menu-access")]
    public async Task<ActionResult<object>> HasMenuAccess(
        string moduleCode,string menuController,string menuAction,CancellationToken cancellationToken=default)
    {
        if(!TryGetUserId(out var userId))return Unauthorized();
        return Ok(new { hasAccess=await service.HasMenuAccessAsync(userId,moduleCode,menuController,menuAction,cancellationToken) });
    }

    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<CurrentPermissionDto>>> GetMine(
        string moduleCode,string menuController,string menuAction,CancellationToken cancellationToken=default)
    {
        if(!TryGetUserId(out var userId))return Unauthorized();
        var codes=await service.GetGrantedCodesAsync(userId,moduleCode,menuController,menuAction,cancellationToken);
        return Ok(codes.Select(code=>new CurrentPermissionDto(code)));
    }

    [HttpGet("check")]
    public async Task<ActionResult<object>> HasPermission(
        string permissionCode,CancellationToken cancellationToken=default)
    {
        if(!TryGetUserId(out var userId))return Unauthorized();
        return Ok(new { hasPermission=await service.HasPermissionAsync(userId,permissionCode,cancellationToken) });
    }

    private bool TryGetUserId(out int userId)
    {
        var value=User.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)??User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value,out userId)&&userId>0;
    }
}
