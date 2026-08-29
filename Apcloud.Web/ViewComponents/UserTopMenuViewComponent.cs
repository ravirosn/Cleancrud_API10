using System.Globalization;
using System.Security.Claims;
using Apcloud.Contracts.Authentication;
using Apcloud.Web.Services;
using Apcloud.Web.Services.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Apcloud.Web.ViewComponents;

public sealed class UserTopMenuViewComponent : ViewComponent
{
    public IViewComponentResult Invoke() => View();
}

public sealed class UserProfileMenuViewComponent(
    ApcloudApiClient apiClient,
    ILogger<UserProfileMenuViewComponent> logger) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var user = await apiClient.GetCurrentUserAsync(HttpContext.RequestAborted);
            return View(UserTopMenuViewModel.FromApi(user));
        }
        catch (Exception exception) when (
            !HttpContext.RequestAborted.IsCancellationRequested &&
            exception is HttpRequestException or AuthApiException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Could not load the current user's profile from /api/Auth/me.");
            return View(UserTopMenuViewModel.FromClaims(UserClaimsPrincipal));
        }
    }
}

public sealed record UserTopMenuViewModel(
    string DisplayName,
    string Email,
    string Initials,
    string PrimaryRole,
    string? UserName,
    string? ContactNumber,
    string? DepartmentName,
    string? BranchName,
    string? ProfilePictureUrl,
    IReadOnlyList<string> Roles,
    int? UserId,
    bool? IsActive,
    DateTimeOffset? CreatedAtUtc,
    string AccountType)
{
    public static UserTopMenuViewModel FromApi(CurrentUserDetailsDto user)
    {
        var displayName = FirstNonEmpty(user.DisplayName, user.UserName, user.Email, "User");
        var roles = user.Roles?.Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        return new UserTopMenuViewModel(
            displayName,
            FirstNonEmpty(user.Email, user.UserName, "Email unavailable"),
            GetInitials(displayName),
            roles.FirstOrDefault() ?? "User",
            user.UserName,
            user.ContactNumber,
            user.DepartmentName,
            user.BranchName,
            ToBffUrl(user.ProfilePictureUrl),
            roles,
            user.Id,
            user.IsActive,
            user.CreatedAtUtc,
            user.EntraObjectId.HasValue ? "Microsoft Entra ID" : "Local account");
    }

    public static UserTopMenuViewModel FromClaims(ClaimsPrincipal principal)
    {
        var displayName = FirstNonEmpty(principal.Identity?.Name, principal.FindFirstValue("preferred_username"), "User");
        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new UserTopMenuViewModel(
            displayName,
            FirstNonEmpty(principal.FindFirstValue(ClaimTypes.Email), principal.FindFirstValue("preferred_username"), "Email unavailable"),
            GetInitials(displayName),
            roles.FirstOrDefault() ?? "User",
            principal.FindFirstValue("preferred_username"),
            principal.FindFirstValue("contact_number"),
            principal.FindFirstValue("department"),
            principal.FindFirstValue("branch"),
            null,
            roles,
            null,
            null,
            null,
            string.Equals(
                principal.FindFirstValue("authentication_method"),
                "MicrosoftEntraId",
                StringComparison.OrdinalIgnoreCase)
                ? "Microsoft Entra ID"
                : "Local account");
    }

    private static string GetInitials(string displayName)
    {
        var initials = string.Concat(displayName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpper(part[0], CultureInfo.CurrentCulture)));
        return string.IsNullOrWhiteSpace(initials) ? "U" : initials;
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.First(value => !string.IsNullOrWhiteSpace(value))!;

    private static string? ToBffUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            ? "/bff/" + value[5..]
            : value;
    }
}
