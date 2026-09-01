using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloud.Web.Areas.PermitApplication.Controllers;

[Area("PermitApplication")]
[Authorize]
[Route("PermitApplication/RiskAssessment")]
public sealed class RiskAssessmentController : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index() => View();

    [HttpGet("Create")]
    public IActionResult Create() => RedirectToAction(nameof(Index), new { create = true });

    [HttpGet("Edit/{id}")]
    public IActionResult Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 100 || id.Contains('/'))
        {
            return BadRequest();
        }

        return RedirectToAction(nameof(Index), new { edit = id });
    }

}
