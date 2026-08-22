using System.Security.Claims;
using Apcloudpms.API.Middleware;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/approval-workflows")]
[Authorize(Roles = "Admin")]
public sealed class ApprovalWorkflowsController(IApprovalWorkflowService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApprovalWorkflowDto>>> Get(
        int? permitTypeListItemId = null,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetAsync(permitTypeListItemId, cancellationToken));

    [HttpPut("{permitTypeListItemId:int}")]
    public async Task<ActionResult<ApprovalWorkflowDto>> Save(
        int permitTypeListItemId,
        ApprovalWorkflowRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();
        var workflow = await service.SaveAsync(
            permitTypeListItemId, request, userId, cancellationToken);
        return workflow is null
            ? NotFound(new { message = "An active PERMIT_TYPE list item was not found." })
            : Ok(workflow);
    }

    private bool TryGetUserId(out int userId)
    {
        var value = User.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }
}
