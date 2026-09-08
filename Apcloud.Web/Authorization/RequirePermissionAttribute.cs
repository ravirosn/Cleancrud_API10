using Apcloud.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Apcloud.Web.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permissionCode) : base(typeof(RequirePermissionFilter)) =>
        Arguments = [permissionCode];
}

public sealed class RequirePermissionFilter(
    string permissionCode,
    ApcloudApiClient apiClient,
    ILogger<RequirePermissionFilter> logger) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        try
        {
            if (!await apiClient.HasPermissionAsync(
                    permissionCode, context.HttpContext.RequestAborted))
                context.Result = new ForbidResult();
        }
        catch (Exception exception) when (!context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning(exception,
                "Could not verify permission {PermissionCode}.", permissionCode);
            context.Result = new ForbidResult();
        }
    }
}
