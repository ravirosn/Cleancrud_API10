using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize]
[Route("Organization/User")]
public sealed class UserController : Controller
{
    [HttpGet("Index")]
    public IActionResult Index() => View();
}
