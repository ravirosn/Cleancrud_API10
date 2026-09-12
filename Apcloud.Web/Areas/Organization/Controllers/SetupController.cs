using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Apcloud.Contracts.Permissions;
using Apcloud.Web.Authorization;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize]
[Route("Organization/Setup")]
public sealed class SetupController : Controller
{
    [HttpGet("Role")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "Setup", "Role")]
    [RequirePermission(ApplicationPermissions.Roles.View)]
    public IActionResult Role() => View();

    [HttpGet("Module")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "Setup", "Module")]
    [RequirePermission(ApplicationPermissions.Modules.View)]
    public IActionResult Module() => View();

    [HttpGet("Permission")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "Setup", "Permission")]
    [RequirePermission(ApplicationPermissions.PermissionAssignments.View)]
    public IActionResult Permission() => View();

    [HttpGet("PermitApplicationGuidelines")]
    [RequirePermission(ApplicationPermissions.PermitApplicationGuidelines.View)]
    public IActionResult PermitApplicationGuidelines() => View();
}
