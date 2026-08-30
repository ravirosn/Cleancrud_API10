using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Apcloud.Web.Areas.Portal.Models;
using Apcloud.Web.Infrastructure;
using Apcloud.Web.Services;
using Apcloud.Web.Services.Authentication;
using Apcloud.Web.ViewComponents;

namespace Apcloud.Web.Areas.Portal.Controllers;

[Area("Portal")]
[Authorize]
public class PortalController(
    ApcloudApiClient apiClient,
    ILogger<PortalController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var modules = await apiClient.GetMyModulesAsync(cancellationToken);
            return View(new ModuleDashboardViewModel { Modules = modules });
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested &&
            exception is HttpRequestException or AuthApiException or JsonException or TaskCanceledException)
        {
            logger.LogError(exception, "Could not load the modules assigned to the current user.");
            return View(new ModuleDashboardViewModel
            {
                ErrorMessage = "We couldn't load your modules right now. Please try again."
            });
        }
    }

    [HttpGet("/Portal/Modules/{moduleId}")]
    public async Task<IActionResult> Module(string moduleId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(moduleId) || moduleId.Length > 100 || moduleId.Contains('/'))
        {
            return BadRequest();
        }

        try
        {
            var modules = await apiClient.GetMyModulesAsync(cancellationToken);
            var module = modules.FirstOrDefault(item => item.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
            if (module is null)
            {
                return NotFound();
            }

            var menus = await apiClient.SelectModuleMenusAsync(module.Id, cancellationToken);
            HttpContext.Session.SetString(ModuleSessionContext.ActiveModuleIdKey, module.Id);
            var firstMenu = FindFirstClickableMenu(menus);
            if (firstMenu is not null)
            {
                return LocalRedirect(firstMenu.Url!);
            }

            var viewModel = new ModuleMenusViewModel
            {
                ModuleId = module.Id,
                ModuleName = module.Name,
                Menus = menus
            };
            HttpContext.Items[ModuleNavigationViewComponent.ModuleMenusViewModelItemKey] = viewModel;
            return View(viewModel);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested &&
            exception is HttpRequestException or AuthApiException or JsonException or TaskCanceledException)
        {
            logger.LogError(exception, "Could not load menus for module {ModuleId}.", moduleId);
            return View(new ModuleMenusViewModel
            {
                ModuleId = moduleId,
                ErrorMessage = "We couldn't load this module's navigation right now. Please try again."
            });
        }
    }

    private static NavigationMenuViewModel? FindFirstClickableMenu(
        IEnumerable<NavigationMenuViewModel> menus)
    {
        foreach (var menu in menus)
        {
            if (!string.IsNullOrWhiteSpace(menu.Url))
            {
                return menu;
            }

            var child = FindFirstClickableMenu(menu.Children);
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }
}
