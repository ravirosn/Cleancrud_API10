using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Apcloud.Web.Infrastructure;
using Apcloud.Web.Services;
using Apcloud.Web.Services.Authentication;

namespace Apcloud.Web.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
[Route("bff")]
public sealed class BffController(
    ApcloudApiClient apiClient,
    IOptions<BffOptions> options,
    ILogger<BffController> logger) : ControllerBase
{
    private static readonly HashSet<string> RequestHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept",
        "Accept-Language",
        "If-Match",
        "If-None-Match",
        "If-Modified-Since",
        "Range"
    };

    private static readonly HashSet<string> ResponseHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cache-Control",
        "Content-Disposition",
        "Content-Language",
        "Content-Range",
        "ETag",
        "Last-Modified",
        "Retry-After"
    };

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("{**path}")]
    public async Task Proxy(string? path, CancellationToken cancellationToken)
    {
        if (!IsAllowed(path, Request.Method))
        {
            logger.LogWarning(
                "Blocked BFF request for method {Method} and path {Path}.",
                Request.Method,
                path);
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var target = $"api/{path}{Request.QueryString}";
        using var downstreamRequest = new HttpRequestMessage(new HttpMethod(Request.Method), target);
        CopyRequestHeaders(downstreamRequest);

        if (Request.ContentLength > 0 || Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            downstreamRequest.Content = Request.HasFormContentType
                ? await CreateFormContentAsync(cancellationToken)
                : CreateStreamContent();
        }

        HttpResponseMessage downstreamResponse;
        try
        {
            downstreamResponse = await apiClient.SendAsync(
                downstreamRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (AuthApiException exception)
        {
            Response.StatusCode = (int)exception.StatusCode;
            return;
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "The downstream API request failed for path {Path}.", path);
            Response.StatusCode = StatusCodes.Status502BadGateway;
            await Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status502BadGateway,
                    Title = "The API is temporarily unavailable."
                },
                cancellationToken);
            return;
        }

        using (downstreamResponse)
        {
            Response.StatusCode = (int)downstreamResponse.StatusCode;
            Response.ContentType = downstreamResponse.Content.Headers.ContentType?.ToString();
            CopyResponseHeaders(downstreamResponse);

            if (downstreamResponse.IsSuccessStatusCode &&
                !HttpMethods.IsGet(Request.Method) &&
                path?.Trim('/').StartsWith("user-profile", StringComparison.OrdinalIgnoreCase) == true)
            {
                apiClient.InvalidateCurrentUserProfile();
            }

            await using var responseStream = await downstreamResponse.Content.ReadAsStreamAsync(cancellationToken);
            await responseStream.CopyToAsync(Response.Body, cancellationToken);
        }
    }

    private HttpContent CreateStreamContent()
    {
        var content = new StreamContent(Request.Body);
        if (MediaTypeHeaderValue.TryParse(Request.ContentType, out var contentType))
        {
            content.Headers.ContentType = contentType;
        }

        return content;
    }

    private async Task<HttpContent> CreateFormContentAsync(CancellationToken cancellationToken)
    {
        // Model binding/antiforgery can inspect a form before this action runs. Rebuilding
        // the multipart body avoids forwarding a consumed request stream with the old
        // boundary, which the API rejects before its action is invoked.
        var form = await Request.ReadFormAsync(cancellationToken);
        var content = new MultipartFormDataContent();

        foreach (var field in form)
        {
            foreach (var value in field.Value)
            {
                content.Add(new StringContent(value ?? string.Empty), field.Key);
            }
        }

        foreach (var file in form.Files)
        {
            var fileContent = new StreamContent(file.OpenReadStream());
            if (MediaTypeHeaderValue.TryParse(file.ContentType, out var contentType))
            {
                fileContent.Headers.ContentType = contentType;
            }

            content.Add(fileContent, file.Name, file.FileName);
        }

        return content;
    }

    private bool IsAllowed(string? path, string method)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            path.Contains('\\', StringComparison.Ordinal) ||
            path.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(segment => segment is "." or "..") ||
            !options.Value.AllowedMethods.Contains(method, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedPath = path.Trim('/');
        return options.Value.AllowedPathPrefixes.Any(prefix =>
        {
            var normalizedPrefix = prefix.Trim('/');
            return normalizedPath.Equals(normalizedPrefix, StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.StartsWith(normalizedPrefix + '/', StringComparison.OrdinalIgnoreCase);
        });
    }

    private void CopyRequestHeaders(HttpRequestMessage downstreamRequest)
    {
        foreach (var header in Request.Headers.Where(header => RequestHeaders.Contains(header.Key)))
        {
            downstreamRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    private void CopyResponseHeaders(HttpResponseMessage downstreamResponse)
    {
        foreach (var header in downstreamResponse.Headers
                     .Concat(downstreamResponse.Content.Headers)
                     .Where(header => ResponseHeaders.Contains(header.Key)))
        {
            Response.Headers[header.Key] = header.Value.ToArray();
        }
    }
}
