using Apcloud.Contracts.Permissions;
using Apcloud.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloud.Web.Areas.PermitApplication.Controllers;

[Area("PermitApplication")]
[Authorize]
[Route("RiskAssessment")]
public sealed class RiskAssessmentController : Controller
{
    [HttpGet("Index")]
    [RequireMenu(ApplicationPermissions.PermitModule, "RiskAssessment", "Index")]
    [RequirePermission(ApplicationPermissions.RiskAssessments.View)]
    public IActionResult Index() => View();

    [HttpGet("Create")]
    [RequireMenu(ApplicationPermissions.PermitModule, "RiskAssessment", "Create")]
    [RequirePermission(ApplicationPermissions.RiskAssessments.Create)]
    public IActionResult Create() => RedirectToAction(nameof(Index), new { create = true });

    [HttpGet("Edit/{id}")]
    [RequireMenu(ApplicationPermissions.PermitModule, "RiskAssessment", "Edit")]
    [RequirePermission(ApplicationPermissions.RiskAssessments.Edit)]
    public IActionResult Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 100 || id.Contains('/'))
        {
            return BadRequest();
        }

        return RedirectToAction(nameof(Index), new { edit = id });
    }

}
