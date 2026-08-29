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
    public IActionResult Index()
    {
        return View();
    }
}
