using Apcloudpms.Infrastructure.Data;
using Apcloudpms.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Apcloudpms.API.Authorization;

public sealed record ModuleRequirement(string ModuleCode) : IAuthorizationRequirement;
public sealed record MenuRequirement(string ModuleCode, string Controller, string Action) : IAuthorizationRequirement;
public sealed record PermissionRequirement(string PermissionCode) : IAuthorizationRequirement;
public sealed record ApiScopeRequirement(string Scope) : IAuthorizationRequirement;

/// <summary>
/// SuperAdmin is the application break-glass role and is not constrained by
/// module, menu, action, scope, or legacy role-specific authorization policies.
/// Authentication is still required before this handler can grant the bypass.
/// </summary>
public sealed class SuperAdminAuthorizationHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            AuthorizationUser.IsSuperAdmin(context.User))
        {
            foreach (var requirement in context.PendingRequirements.ToArray())
                context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public sealed class ApiScopeAuthorizationHandler : AuthorizationHandler<ApiScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ApiScopeRequirement requirement)
    {
        // Legacy local tokens have no Entra tenant/object claims during the transition.
        if (context.User.FindFirst("tid") is null || context.User.FindFirst("oid") is null)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var scopes = context.User.FindFirstValue("scp")?.Split(
            ' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (scopes.Contains(requirement.Scope, StringComparer.OrdinalIgnoreCase))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

public sealed class ModuleAuthorizationHandler : AuthorizationHandler<ModuleRequirement>
{
    private readonly AppDbContext _context;

    public ModuleAuthorizationHandler(AppDbContext context) => _context = context;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ModuleRequirement requirement)
    {
        if (AuthorizationUser.IsSuperAdmin(context.User))
        {
            context.Succeed(requirement);
            return;
        }

        var localUserId = context.User.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)
            ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(localUserId, out var userId))
            return;

        var hasAccess = await _context.ApplicationModules.AsNoTracking().AnyAsync(x =>
            x.IsActive && x.Code == requirement.ModuleCode &&
            x.RoleModules.Any(rm => rm.IsActive && rm.Role.IsActive &&
                rm.Role.UserRoles.Any(ur => ur.UserId == userId && ur.IsActive)));
        if (hasAccess) context.Succeed(requirement);
    }
}

public sealed class MenuAuthorizationHandler(AppDbContext context) : AuthorizationHandler<MenuRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authorizationContext, MenuRequirement requirement)
    {
        if (AuthorizationUser.IsSuperAdmin(authorizationContext.User))
        {
            authorizationContext.Succeed(requirement);
            return;
        }

        if (!AuthorizationUser.TryGetLocalUserId(authorizationContext.User, out var userId)) return;
        var hasAccess = await context.RoleModuleMenus.AsNoTracking().AnyAsync(rmm =>
            rmm.IsActive && rmm.RoleModule.IsActive && rmm.RoleModule.Role.IsActive &&
            rmm.RoleModule.ApplicationModule.IsActive && rmm.RoleModule.ApplicationModule.Code == requirement.ModuleCode &&
            rmm.ModuleMenu.IsActive && rmm.ModuleMenu.ControllerName == requirement.Controller &&
            rmm.ModuleMenu.ActionName == requirement.Action &&
            rmm.RoleModule.Role.UserRoles.Any(ur => ur.UserId == userId && ur.IsActive));
        if (hasAccess) authorizationContext.Succeed(requirement);
    }
}

public sealed class PermissionAuthorizationHandler(AppDbContext context) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authorizationContext, PermissionRequirement requirement)
    {
        if (AuthorizationUser.IsSuperAdmin(authorizationContext.User))
        {
            authorizationContext.Succeed(requirement);
            return;
        }

        if (!AuthorizationUser.TryGetLocalUserId(authorizationContext.User, out var userId)) return;
        var hasPermission = await context.RolePermissions.AsNoTracking().AnyAsync(rp =>
            rp.IsActive && rp.PermissionPolicy.IsActive && rp.PermissionPolicy.Code == requirement.PermissionCode &&
            rp.Role.IsActive && rp.Role.UserRoles.Any(ur => ur.UserId == userId && ur.IsActive) &&
            rp.Role.RoleModules.Any(rm => rm.IsActive && rm.ApplicationModule.IsActive &&
                rm.ApplicationModule.Code == rp.PermissionPolicy.ModuleCode &&
                rm.RoleModuleMenus.Any(rmm => rmm.IsActive && rmm.ModuleMenu.IsActive &&
                    rmm.ModuleMenu.ControllerName == rp.PermissionPolicy.MenuController &&
                    rmm.ModuleMenu.ActionName == rp.PermissionPolicy.MenuAction)));
        if (hasPermission) authorizationContext.Succeed(requirement);
    }
}

internal static class AuthorizationUser
{
    public static bool IsSuperAdmin(ClaimsPrincipal principal) =>
        principal.IsInRole("SuperAdmin");

    public static bool TryGetLocalUserId(ClaimsPrincipal principal, out int userId) =>
        int.TryParse(principal.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId) && userId > 0;
}

public sealed class ModulePolicyProvider : DefaultAuthorizationPolicyProvider
{
    private readonly string _apiScope;

    public ModulePolicyProvider(
        IOptions<AuthorizationOptions> options, IConfiguration configuration) : base(options) =>
        _apiScope = configuration["AzureAd:Scopes"] ?? "access_as_user";

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(RequireMenuAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var parts = policyName[RequireMenuAttribute.PolicyPrefix.Length..].Split(':', 3);
            if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace))
                return Task.FromResult<AuthorizationPolicy?>(null);
            return Task.FromResult<AuthorizationPolicy?>(BuildPolicy(
                new MenuRequirement(parts[0].Trim(), parts[1].Trim(), parts[2].Trim())));
        }

        if (policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var code = policyName[RequirePermissionAttribute.PolicyPrefix.Length..].Trim();
            return Task.FromResult<AuthorizationPolicy?>(string.IsNullOrWhiteSpace(code)
                ? null
                : BuildPolicy(new PermissionRequirement(code)));
        }

        if (!policyName.StartsWith(RequireModuleAttribute.PolicyPrefix,
                StringComparison.OrdinalIgnoreCase))
            return base.GetPolicyAsync(policyName);

        var moduleCode = policyName[RequireModuleAttribute.PolicyPrefix.Length..]
            .Trim().ToUpperInvariant();
        return Task.FromResult<AuthorizationPolicy?>(BuildPolicy(new ModuleRequirement(moduleCode)));
    }

    private AuthorizationPolicy BuildPolicy(IAuthorizationRequirement requirement) =>
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new ApiScopeRequirement(_apiScope))
            .AddRequirements(requirement)
            .Build();
}
