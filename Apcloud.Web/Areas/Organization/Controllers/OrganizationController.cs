using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Apcloud.Web.Authorization;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize]
[Route("Organization/Organization")]
public sealed class OrganizationController : Controller
{
    [HttpGet("Index")]
    public IActionResult Index() => View();

    [HttpGet("OfficeBranches")]
    public IActionResult OfficeBranches() => View();

    [HttpGet("Departments")]
    public IActionResult Departments() => View();

    [HttpGet("FiscalYears")]
    [RequireMenu("ORGANIZATION", "Organization", "FiscalYears")]
    public IActionResult FiscalYears() => View();
}
