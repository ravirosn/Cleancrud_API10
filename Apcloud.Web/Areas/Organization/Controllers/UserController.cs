using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Apcloud.Contracts.Permissions;
using Apcloud.Web.Authorization;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize]
[Route("Organization/User")]
public sealed class UserController : Controller
{
    [HttpGet("Index")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "User", "Index")]
    [RequirePermission(ApplicationPermissions.Users.View)]
    public IActionResult Index() => View();
}
