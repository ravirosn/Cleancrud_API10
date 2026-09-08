using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Apcloud.Contracts.Permissions;
using Apcloud.Web.Authorization;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize]
[Route("Organization/AuditLog")]
public sealed class AuditLogController : Controller
{
    [HttpGet("Index")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "AuditLog", "Index")]
    [RequirePermission(ApplicationPermissions.AuditLogs.View)]
    public IActionResult Index() => View();
}
