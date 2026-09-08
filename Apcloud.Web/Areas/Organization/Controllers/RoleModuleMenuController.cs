using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Apcloud.Contracts.Permissions;
using Apcloud.Web.Authorization;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize]
[Route("Organization/RoleModuleMenu")]
public sealed class RoleModuleMenuController : Controller
{
    [HttpGet("Index")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "RoleModuleMenu", "Index")]
    [RequirePermission(ApplicationPermissions.RoleModuleMenus.View)]
    public IActionResult Index() => View();
}
