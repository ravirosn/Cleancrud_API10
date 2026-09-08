using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Apcloud.Contracts.Permissions;
using Apcloud.Web.Authorization;

namespace Apcloud.Web.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize]
[Route("Organization/ListItem")]
public sealed class ListItemController : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "ListItem", "Index")]
    [RequirePermission(ApplicationPermissions.ListItemCategories.View)]
    public IActionResult Index() => View();

    [HttpGet("ListItem")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "ListItem", "ListItem")]
    [RequirePermission(ApplicationPermissions.ListItems.View)]
    public IActionResult ListItem() => View();
}
