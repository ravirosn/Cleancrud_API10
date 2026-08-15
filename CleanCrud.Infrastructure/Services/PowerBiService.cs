using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CleanCrud.Infrastructure.Services;

public sealed class PowerBiService : IPowerBiService
{
    private static readonly string[] Scopes = ["https://analysis.windows.net/powerbi/api/.default"];
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PowerBiOptions _options;
    private IConfidentialClientApplication? _confidentialClient;

    public PowerBiService(IHttpClientFactory httpClientFactory, IOptions<PowerBiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<PowerBiEmbedConfigDto> GetEmbedConfigAsync(
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();
        _confidentialClient ??= ConfidentialClientApplicationBuilder
            .Create(_options.ClientId)
            .WithClientSecret(_options.ClientSecret)
            .WithAuthority($"https://login.microsoftonline.com/{_options.TenantId}")
            .Build();

        var authentication = await _confidentialClient.AcquireTokenForClient(Scopes)
            .ExecuteAsync(cancellationToken);
        var client = _httpClientFactory.CreateClient("PowerBi");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authentication.AccessToken);

        var reportUrl = $"groups/{_options.WorkspaceId}/reports/{_options.ReportId}";
        var report = await GetAsync<PowerBiReportResponse>(client, reportUrl, cancellationToken);

        using var tokenResponse = await client.PostAsJsonAsync(
            $"{reportUrl}/GenerateToken", new { accessLevel = "View" }, cancellationToken);
        await EnsureSuccessAsync(tokenResponse, cancellationToken);
        var embedToken = await tokenResponse.Content.ReadFromJsonAsync<PowerBiTokenResponse>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Power BI returned an empty embed-token response.");

        return new PowerBiEmbedConfigDto(report.Id, report.Name, report.EmbedUrl,
            embedToken.Token, embedToken.Expiration);
    }

    private static async Task<T> GetAsync<T>(
        HttpClient client, string url, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Power BI returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Power BI request failed with status {(int)response.StatusCode}: " +
            detail[..Math.Min(detail.Length, 1000)]);
    }

    private void ValidateConfiguration()
    {
        var missing = new Dictionary<string, string>
        {
            [nameof(_options.TenantId)] = _options.TenantId,
            [nameof(_options.ClientId)] = _options.ClientId,
            [nameof(_options.ClientSecret)] = _options.ClientSecret,
            [nameof(_options.WorkspaceId)] = _options.WorkspaceId,
            [nameof(_options.ReportId)] = _options.ReportId
        }.Where(x => string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Key).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException(
                $"Power BI configuration is missing: {string.Join(", ", missing)}.");
    }

    private sealed class PowerBiReportResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string EmbedUrl { get; set; } = string.Empty;
    }

    private sealed class PowerBiTokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset Expiration { get; set; }
    }
}
