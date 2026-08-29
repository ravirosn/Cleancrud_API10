using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Apcloud.Contracts.Enums;
using Apcloud.Contracts.Authentication;
using Apcloud.Web.Infrastructure;

namespace Apcloud.Web.Services.Authentication;

/// <summary>
/// Adds the current API access token to server-side API calls and rotates it
/// with the refresh token shortly before expiration.
/// </summary>
public sealed class ApiBearerTokenHandler(
    IHttpContextAccessor httpContextAccessor,
    IAuthApiClient authApiClient,
    IServiceProvider serviceProvider,
    IOptions<MicrosoftEntraOptions> entraOptions,
    ILogger<ApiBearerTokenHandler> logger) : DelegatingHandler
{
    private static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RotationReplayWindow = TimeSpan.FromMinutes(2);
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);
    private static readonly Dictionary<string, RotatedTokenEntry> RecentRotations = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Authenticated API calls require an active HTTP request.");

        var accessToken = await GetValidAccessTokenAsync(context, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetValidAccessTokenAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var authentication = await context.AuthenticateAsync(AuthenticationSchemeNames.ApplicationCookie);
        if (!authentication.Succeeded || authentication.Principal is null || authentication.Properties is null)
        {
            throw new AuthApiException(System.Net.HttpStatusCode.Unauthorized, "The local session is no longer valid.");
        }

        if (authentication.Principal.FindFirst("authentication_method")?.Value ==
            AuthenticationMethod.MicrosoftEntraId.ToString())
        {
            var tokenAcquisition = serviceProvider.GetService<ITokenAcquisition>()
                ?? throw new InvalidOperationException("Microsoft Entra token acquisition is not registered.");

            try
            {
                return await tokenAcquisition.GetAccessTokenForUserAsync(
                    entraOptions.Value.ApiScopes,
                    authenticationScheme: AuthenticationSchemeNames.MicrosoftEntra,
                    user: authentication.Principal);
            }
            catch (MicrosoftIdentityWebChallengeUserException exception)
            {
                logger.LogInformation(exception, "The Microsoft Entra session requires renewed user interaction.");
                await context.SignOutAsync(AuthenticationSchemeNames.ApplicationCookie);
                throw new AuthApiException(
                    System.Net.HttpStatusCode.Unauthorized,
                    "The Microsoft Entra session must be renewed.",
                    exception);
            }
        }

        if (TryGetUsableAccessToken(authentication.Properties, out var accessToken))
        {
            return accessToken;
        }

        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            // Another concurrent request may have refreshed the shared ticket.
            authentication = await context.AuthenticateAsync(AuthenticationSchemeNames.ApplicationCookie);
            if (authentication.Succeeded && authentication.Properties is not null &&
                TryGetUsableAccessToken(authentication.Properties, out accessToken))
            {
                return accessToken;
            }

            var properties = authentication.Properties
                ?? throw new AuthApiException(System.Net.HttpStatusCode.Unauthorized, "The local session is no longer valid.");
            var refreshToken = properties.GetTokenValue(AuthenticationTokenNames.RefreshToken);
            var refreshExpiresAt = ParseTimestamp(properties.GetTokenValue(AuthenticationTokenNames.RefreshTokenExpiresAt));

            if (string.IsNullOrWhiteSpace(refreshToken) || refreshExpiresAt <= DateTimeOffset.UtcNow)
            {
                await context.SignOutAsync(AuthenticationSchemeNames.ApplicationCookie);
                throw new AuthApiException(System.Net.HttpStatusCode.Unauthorized, "The session has expired.");
            }

            try
            {
                var rotationKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
                RemoveExpiredRotations();

                AuthResponseDto tokens;
                if (RecentRotations.TryGetValue(rotationKey, out var recentRotation))
                {
                    tokens = recentRotation.Tokens;
                }
                else
                {
                    tokens = await authApiClient.RefreshAsync(refreshToken, cancellationToken);
                    RecentRotations[rotationKey] = new RotatedTokenEntry(
                        tokens,
                        DateTimeOffset.UtcNow.Add(RotationReplayWindow));
                }

                StoreTokens(properties, tokens);
                properties.ExpiresUtc = tokens.RefreshTokenExpiresAtUtc;
                await context.SignInAsync(
                    AuthenticationSchemeNames.ApplicationCookie,
                    authentication.Principal!,
                    properties);

                return tokens.AccessToken!;
            }
            catch (AuthApiException exception)
            {
                logger.LogWarning(exception, "The API token refresh failed with status {StatusCode}.", exception.StatusCode);
                if (exception.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized)
                {
                    await context.SignOutAsync(AuthenticationSchemeNames.ApplicationCookie);
                }

                throw;
            }
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    internal static void StoreTokens(AuthenticationProperties properties, AuthResponseDto tokens) =>
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = AuthenticationTokenNames.AccessToken, Value = tokens.AccessToken! },
            new AuthenticationToken { Name = AuthenticationTokenNames.RefreshToken, Value = tokens.RefreshToken! },
            new AuthenticationToken
            {
                Name = AuthenticationTokenNames.AccessTokenExpiresAt,
                Value = tokens.AccessTokenExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture)
            },
            new AuthenticationToken
            {
                Name = AuthenticationTokenNames.RefreshTokenExpiresAt,
                Value = tokens.RefreshTokenExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture)
            }
        ]);

    private static bool TryGetUsableAccessToken(AuthenticationProperties properties, out string accessToken)
    {
        accessToken = properties.GetTokenValue(AuthenticationTokenNames.AccessToken) ?? string.Empty;
        var expiresAt = ParseTimestamp(properties.GetTokenValue(AuthenticationTokenNames.AccessTokenExpiresAt));
        return accessToken.Length > 0 && expiresAt > DateTimeOffset.UtcNow.Add(RefreshWindow);
    }

    private static DateTimeOffset ParseTimestamp(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
            ? result
            : DateTimeOffset.MinValue;

    private static void RemoveExpiredRotations()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var key in RecentRotations.Where(pair => pair.Value.ExpiresAt <= now).Select(pair => pair.Key).ToArray())
        {
            RecentRotations.Remove(key);
        }
    }

    private sealed record RotatedTokenEntry(AuthResponseDto Tokens, DateTimeOffset ExpiresAt);
}
