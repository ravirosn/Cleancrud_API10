using Apcloudpms.Application.Interfaces;
using System.Security.Claims;

namespace Apcloudpms.API.Services;

public sealed class HttpAuditContext(IHttpContextAccessor httpContextAccessor) : IAuditContext
{
    private HttpContext? HttpContext => httpContextAccessor.HttpContext;

    public int? UserId
    {
        get
        {
            var value = HttpContext?.User.FindFirstValue("local_user_id")
                ?? HttpContext?.User.FindFirstValue("sub")
                ?? HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }

    public string? UserName => HttpContext?.User.Identity?.Name
        ?? HttpContext?.User.FindFirstValue("preferred_username");

    public string? TraceId => HttpContext?.TraceIdentifier;

    public string? IpAddress => HttpContext?.Connection.RemoteIpAddress?.ToString();
}
