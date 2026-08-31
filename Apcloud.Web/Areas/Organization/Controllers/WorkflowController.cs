using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize]
[Route("Organization/Workflow")]
public sealed class WorkflowController : Controller
{
    [HttpGet("Index")]
    public IActionResult Index() => View();
}
