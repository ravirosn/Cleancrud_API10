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
    public IActionResult Create()
    {
        return View();
    }

    [HttpGet("Edit/{id}")]
    public IActionResult Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 100 || id.Contains('/'))
        {
            return BadRequest();
        }

        ViewData["RiskAssessmentId"] = id;
        return View();
    }

}
