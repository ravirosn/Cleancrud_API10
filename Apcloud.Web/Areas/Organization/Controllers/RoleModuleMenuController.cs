using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize]
[Route("Organization/RoleModuleMenu")]
public sealed class RoleModuleMenuController : Controller
{
    [HttpGet("Index")]
    public IActionResult Index() => View();
}
