using Apcloud.Contracts.Permissions;
using Apcloud.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloud.Web.Areas.PermitApplication.Controllers;

[Area("PermitApplication")]
[Authorize]
[Route("PermitApplication/Dashboard")]
public sealed class DashboardController : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    [RequireMenu(ApplicationPermissions.PermitModule, "PermitDashboard", "Index")]
    [RequirePermission(ApplicationPermissions.PermitDashboard.View)]
    public IActionResult Index()
    {
        return View();
    }
}
