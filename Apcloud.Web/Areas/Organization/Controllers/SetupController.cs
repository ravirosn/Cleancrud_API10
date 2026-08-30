using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize]
[Route("Organization/Setup")]
public sealed class SetupController : Controller
{
    [HttpGet("Role")]
    public IActionResult Role() => View();

    [HttpGet("Module")]
    public IActionResult Module() => View();
}
