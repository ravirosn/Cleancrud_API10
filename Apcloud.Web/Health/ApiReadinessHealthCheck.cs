using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Apcloud.Web.Health;

public sealed class ApiReadinessHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("ApiHealth");
            using var response = await client.GetAsync(
                "health/ready", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("API is ready.")
                : HealthCheckResult.Unhealthy(
                    $"API readiness endpoint returned HTTP {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("API readiness check timed out.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("API readiness check failed.", exception);
        }
    }
}
