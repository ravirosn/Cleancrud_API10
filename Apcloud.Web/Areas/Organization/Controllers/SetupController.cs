using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Organization/Setup")]
public sealed class SetupController : Controller
{
    [HttpGet("Role")]
    public IActionResult Role() => View();

    [HttpGet("Module")]
    public IActionResult Module() => View();

    [HttpGet("Permission")]
    public IActionResult Permission() => View();
}
