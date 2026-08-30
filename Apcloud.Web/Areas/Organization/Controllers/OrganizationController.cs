using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize]
[Route("Organization/Organization")]
public sealed class OrganizationController : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index() => View();
}
