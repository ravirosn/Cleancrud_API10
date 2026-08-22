using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using System.Security.Claims;

namespace Apcloudpms.API.Middleware;

public sealed class EntraUserMiddleware
{
    public const string LocalUserIdClaim = "local_user_id";
    private readonly RequestDelegate _next;

    public EntraUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IEntraUserService userService)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            Guid.TryParse(context.User.FindFirstValue("tid"), out var tenantId) &&
            Guid.TryParse(context.User.FindFirstValue("oid"), out var objectId))
        {
            var userName = FirstClaim(context.User, "preferred_username", "upn", "email", "name")
                ?? objectId.ToString("N");
            var profile = new EntraUserProfileDto(
                tenantId,
                objectId,
                userName,
                context.User.FindFirstValue("name"),
                FirstClaim(context.User, "email", "preferred_username", "upn"));
            var localUser = await userService.EnsureUserAsync(profile, context.RequestAborted);
            if (localUser is null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            if (context.User.Identity is ClaimsIdentity identity)
            {
                identity.AddClaim(new Claim(LocalUserIdClaim, localUser.UserId.ToString()));
                foreach (var role in localUser.Roles)
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        await _next(context);
    }

    private static string? FirstClaim(ClaimsPrincipal user, params string[] claimTypes) =>
        claimTypes.Select(user.FindFirstValue).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
}
