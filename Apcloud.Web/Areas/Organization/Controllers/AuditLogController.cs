using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Organization/AuditLog")]
public sealed class AuditLogController : Controller
{
    [HttpGet("Index")]
    public IActionResult Index() => View();
}
