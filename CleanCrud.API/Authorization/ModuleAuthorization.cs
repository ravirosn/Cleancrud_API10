using CleanCrud.Infrastructure.Data;
using CleanCrud.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CleanCrud.API.Authorization;

public sealed record ModuleRequirement(string ModuleCode) : IAuthorizationRequirement;
public sealed record ApiScopeRequirement(string Scope) : IAuthorizationRequirement;

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
        var localUserId = context.User.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)
            ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(localUserId, out var userId))
            return;

        var hasAccess = await _context.UserModules.AsNoTracking().AnyAsync(x =>
            x.UserId == userId && x.IsActive && x.ApplicationModule.IsActive &&
            x.ApplicationModule.Code == requirement.ModuleCode);
        if (hasAccess) context.Succeed(requirement);
    }
}

public sealed class ModulePolicyProvider : DefaultAuthorizationPolicyProvider
{
    private readonly string _apiScope;

    public ModulePolicyProvider(
        IOptions<AuthorizationOptions> options, IConfiguration configuration) : base(options) =>
        _apiScope = configuration["AzureAd:Scopes"] ?? "access_as_user";

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(RequireModuleAttribute.PolicyPrefix,
                StringComparison.OrdinalIgnoreCase))
            return base.GetPolicyAsync(policyName);

        var moduleCode = policyName[RequireModuleAttribute.PolicyPrefix.Length..]
            .Trim().ToUpperInvariant();
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new ApiScopeRequirement(_apiScope))
            .AddRequirements(new ModuleRequirement(moduleCode))
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
