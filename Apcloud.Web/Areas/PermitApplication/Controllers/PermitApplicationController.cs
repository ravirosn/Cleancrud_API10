using Apcloud.Contracts.Permissions;
using Apcloud.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloud.Web.Areas.PermitApplication.Controllers;

[Area("PermitApplication")]
[Authorize]
[Route("PermitApplication/PermitApplication")]
public sealed class PermitApplicationController : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    [RequireMenu(ApplicationPermissions.PermitModule, "PermitApplications", "Index")]
    [RequirePermission(ApplicationPermissions.PermitApplications.View)]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("PermitApproval")]
    [RequireMenu(ApplicationPermissions.PermitModule, "PermitApprovals", "Index")]
    [RequirePermission(ApplicationPermissions.PermitApprovals.View)]
    public IActionResult PermitApproval()
    {
        return View();
    }

    [HttpGet("Create")]
    [RequireMenu(ApplicationPermissions.PermitModule, "PermitApplications", "Index")]
    [RequirePermission(ApplicationPermissions.PermitApplications.Create)]
    public IActionResult Create()
    {
        return View();
    }

    [HttpGet("Edit/{id}")]
    [RequireMenu(ApplicationPermissions.PermitModule, "PermitApplications", "Index")]
    [RequirePermission(ApplicationPermissions.PermitApplications.Edit)]
    public IActionResult Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 100 || id.Contains('/'))
        {
            return BadRequest();
        }

        ViewData["PermitApplicationId"] = id;
        return View();
    }
}
