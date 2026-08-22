using System.Security.Claims;
using Apcloudpms.API.Middleware;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/approval-notifications")]
[Authorize]
public sealed class ApprovalNotificationsController(IApprovalWorkflowService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApprovalNotificationDto>>> Get(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();
        return Ok(await service.GetNotificationsAsync(userId, cancellationToken));
    }

    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();
        return await service.MarkNotificationReadAsync(id, userId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private bool TryGetUserId(out int userId)
    {
        var value = User.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }
}
