using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Organization/Organization")]
public sealed class OrganizationController : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index() => View();

    [HttpGet("OfficeBranches")]
    public IActionResult OfficeBranches() => View();

    [HttpGet("Departments")]
    public IActionResult Departments() => View();
}
