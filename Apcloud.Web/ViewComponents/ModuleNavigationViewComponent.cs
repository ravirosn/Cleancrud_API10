using Microsoft.AspNetCore.Mvc;
using Apcloud.Web.Areas.Portal.Models;
using Apcloud.Web.Services;
using Apcloud.Web.Services.Authentication;

namespace Apcloud.Web.ViewComponents;

public sealed class ModuleNavigationViewComponent(
    ApcloudApiClient apiClient,
    ILogger<ModuleNavigationViewComponent> logger) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string moduleId)
    {
        if (HttpContext.Items[ModuleMenusViewModelItemKey] is ModuleMenusViewModel existing &&
            existing.ModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase))
        {
            PrepareLinks(existing.Menus);
            return View(new ModuleSidebarViewModel
            {
                Module = new AssignedModuleViewModel
                {
                    Id = existing.ModuleId,
                    Name = existing.ModuleName,
                    IsActive = true
                },
                Menus = existing.Menus
            });
        }

        try
        {
            var modules = await apiClient.GetMyModulesAsync(HttpContext.RequestAborted);
            var module = modules.FirstOrDefault(item =>
                item.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
            if (module is null)
            {
                return View(new ModuleSidebarViewModel
                {
                    ErrorMessage = "This module is not assigned to your account."
                });
            }

            var menus = await apiClient.GetModuleMenusAsync(module.Id, HttpContext.RequestAborted);
            PrepareLinks(menus);
            return View(new ModuleSidebarViewModel { Module = module, Menus = menus });
        }
        catch (Exception exception) when (
            !HttpContext.RequestAborted.IsCancellationRequested &&
            exception is HttpRequestException or AuthApiException or TaskCanceledException)
        {
            logger.LogError(exception, "Could not render navigation for module {ModuleId}.", moduleId);
            return View(new ModuleSidebarViewModel
            {
                ErrorMessage = "Module navigation is temporarily unavailable."
            });
        }
    }

    internal const string ModuleMenusViewModelItemKey = "Apcloud.ModuleNavigation";

    private void PrepareLinks(IEnumerable<NavigationMenuViewModel> menus)
    {
        foreach (var menu in menus)
        {
            if (!string.IsNullOrWhiteSpace(menu.Url))
            {
                var menuPath = menu.Url.Split('?', '#')[0].TrimEnd('/');
                menu.IsCurrent = HttpContext.Request.Path.Value?.TrimEnd('/')
                    .Equals(menuPath, StringComparison.OrdinalIgnoreCase) == true;
            }

            PrepareLinks(menu.Children);
            menu.IsCurrent |= menu.Children.Any(child => child.IsCurrent);
        }
    }
}
