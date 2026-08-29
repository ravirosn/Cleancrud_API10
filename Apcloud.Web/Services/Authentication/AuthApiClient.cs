using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Apcloud.Contracts.Authentication;

namespace Apcloud.Web.Services.Authentication;

public sealed class AuthApiClient(HttpClient httpClient, ILogger<AuthApiClient> logger) : IAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<AuthResponseDto> LoginAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default) =>
        PostForTokensAsync("api/Auth/login", new LoginDto { UserName = userName, Password = password }, cancellationToken);

    public Task<AuthResponseDto> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default) =>
        PostForTokensAsync("api/Auth/refresh", new RefreshTokenRequestDto { RefreshToken = refreshToken }, cancellationToken);

    public async Task<CurrentUserDetailsDto> GetCurrentUserAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/Auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return await ReadSuccessAsync<CurrentUserDetailsDto>(response, cancellationToken);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/Auth/revoke",
            new RevokeTokenRequestDto { RefreshToken = refreshToken },
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("The API rejected a refresh-token revocation with status {StatusCode}.", response.StatusCode);
        }
    }

    private async Task<AuthResponseDto> PostForTokensAsync<TRequest>(
        string path,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(path, payload, JsonOptions, cancellationToken);
        var tokens = await ReadSuccessAsync<AuthResponseDto>(response, cancellationToken);

        if (string.IsNullOrWhiteSpace(tokens.AccessToken) ||
            string.IsNullOrWhiteSpace(tokens.RefreshToken) ||
            tokens.AccessTokenExpiresAtUtc <= DateTimeOffset.UtcNow ||
            tokens.RefreshTokenExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new AuthApiException(HttpStatusCode.BadGateway, "The authentication service returned an invalid token response.");
        }

        return tokens;
    }

    private static async Task<T> ReadSuccessAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            try
            {
                var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                return value ?? throw new AuthApiException(
                    HttpStatusCode.BadGateway,
                    "The authentication service returned an empty response.");
            }
            catch (JsonException exception)
            {
                throw new AuthApiException(
                    HttpStatusCode.BadGateway,
                    "The authentication service returned an invalid response.",
                    exception);
            }
        }

        var message = response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized
            ? "The username or password is incorrect."
            : "The authentication service could not complete the request.";

        throw new AuthApiException(response.StatusCode, message);
    }
}
