using System.Globalization;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Apcloud.Contracts.Enums;
using Apcloud.Contracts.Models;
using Apcloud.Contracts.Authentication;
using Apcloud.Web.Infrastructure;
using Apcloud.Web.Services;
using Apcloud.Web.Services.Authentication;

namespace Apcloud.Web.Areas.Authentication.Controllers;

[Area("Authentication")]
public class AccountController(
    IAuthApiClient authApiClient,
    ApcloudApiClient apiClient,
    IOptions<MicrosoftEntraOptions> entraOptions,
    ILogger<AccountController> logger) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Portal", new { area = "Portal" });
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EntraLogin(string? returnUrl = null)
    {
        if (!entraOptions.Value.IsConfigured)
        {
            ModelState.AddModelError(string.Empty, "Microsoft SSO has not been configured yet.");
            return View(nameof(Login), new LoginViewModel { ReturnUrl = returnUrl });
        }

        var redirectUri = Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action("Index", "Portal", new { area = "Portal" })!;

        return Challenge(
            new AuthenticationProperties { RedirectUri = redirectUri },
            AuthenticationSchemeNames.MicrosoftEntra);
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var tokens = await authApiClient.LoginAsync(model.Username.Trim(), model.Password, cancellationToken);
            var user = tokens.User ??
                await authApiClient.GetCurrentUserAsync(tokens.AccessToken!, cancellationToken);

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "This account is inactive. Contact your administrator.");
                ModelState.Remove(nameof(model.Password));
                model.Password = string.Empty;
                return View(model);
            }

            await SignInAsync(user, tokens, model.RememberMe);
            apiClient.CacheCurrentUser(user);
            return RedirectToLocal(model.ReturnUrl);
        }
        catch (AuthApiException exception)
        {
            logger.LogWarning("Login failed with API status {StatusCode}.", exception.StatusCode);
            var message = exception.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized
                ? "The username or password is incorrect."
                : "Sign-in is temporarily unavailable. Please try again.";
            ModelState.AddModelError(string.Empty, message);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "The authentication API could not be reached.");
            ModelState.AddModelError(string.Empty, "Sign-in is temporarily unavailable. Please try again.");
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "The authentication API request timed out.");
            ModelState.AddModelError(string.Empty, "Sign-in is temporarily unavailable. Please try again.");
        }

        // Never send an attempted password back to the rendered page.
        ModelState.Remove(nameof(model.Password));
        model.Password = string.Empty;
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();

        if (User.FindFirstValue("authentication_method") == AuthenticationMethod.MicrosoftEntraId.ToString())
        {
            return SignOut(
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action(nameof(Login), "Account", new { area = "Authentication" })
                },
                AuthenticationSchemeNames.MicrosoftEntra,
                AuthenticationSchemeNames.ApplicationCookie);
        }

        var refreshToken = await HttpContext.GetTokenAsync(
            AuthenticationSchemeNames.ApplicationCookie,
            AuthenticationTokenNames.RefreshToken);

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                using var revokeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await authApiClient.RevokeAsync(refreshToken, revokeTimeout.Token);
            }
            catch (Exception exception) when (exception is HttpRequestException or AuthApiException or TaskCanceledException)
            {
                logger.LogWarning(exception, "The remote token could not be revoked during sign-out.");
            }
        }

        await HttpContext.SignOutAsync(AuthenticationSchemeNames.ApplicationCookie);
        return RedirectToAction(nameof(Login), new { area = "Authentication" });
    }

    private async Task SignInAsync(CurrentUserDetailsDto user, AuthResponseDto tokens, bool persistent)
    {
        var displayName = FirstNonEmpty(user.DisplayName, user.UserName, user.Email, "User");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, displayName),
            new("authentication_method", AuthenticationMethod.UsernamePassword.ToString())
        };

        AddClaimIfPresent(claims, ClaimTypes.Email, user.Email);
        AddClaimIfPresent(claims, "preferred_username", user.UserName);
        AddClaimIfPresent(claims, "contact_number", user.ContactNumber);
        AddClaimIfPresent(claims, "department", user.DepartmentName);
        AddClaimIfPresent(claims, "branch", user.BranchName);

        if (user.DepartmentId is not null)
        {
            claims.Add(new Claim("department_id", user.DepartmentId.Value.ToString(CultureInfo.InvariantCulture)));
        }

        if (user.BranchId is not null)
        {
            claims.Add(new Claim("branch_id", user.BranchId.Value.ToString(CultureInfo.InvariantCulture)));
        }

        foreach (var role in user.Roles?.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.OrdinalIgnoreCase)
                     ?? [])
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, AuthenticationSchemeNames.ApplicationCookie);
        var properties = new AuthenticationProperties
        {
            AllowRefresh = true,
            IsPersistent = persistent,
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = tokens.RefreshTokenExpiresAtUtc
        };
        ApiBearerTokenHandler.StoreTokens(properties, tokens);

        await HttpContext.SignInAsync(
            AuthenticationSchemeNames.ApplicationCookie,
            new ClaimsPrincipal(identity),
            properties);
    }

    private IActionResult RedirectToLocal(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Portal", new { area = "Portal" });

    private static void AddClaimIfPresent(ICollection<Claim> claims, string type, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(type, value));
        }
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.First(value => !string.IsNullOrWhiteSpace(value))!;
}
