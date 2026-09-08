using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Apcloud.Web.Authorization;
using Apcloud.Contracts.Permissions;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize]
[Route("Organization/Organization")]
public sealed class OrganizationController : Controller
{
    [HttpGet("Index")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "Organization", "Index")]
    [RequirePermission(ApplicationPermissions.Organization.View)]
    public IActionResult Index() => View();

    [HttpGet("OfficeBranches")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "Organization", "OfficeBranches")]
    [RequirePermission(ApplicationPermissions.OfficeBranches.View)]
    public IActionResult OfficeBranches() => View();

    [HttpGet("Departments")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "Organization", "Departments")]
    [RequirePermission(ApplicationPermissions.Departments.View)]
    public IActionResult Departments() => View();

    [HttpGet("FiscalYears")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "Organization", "FiscalYears")]
    [RequirePermission(ApplicationPermissions.FiscalYears.View)]
    public IActionResult FiscalYears() => View();
}
