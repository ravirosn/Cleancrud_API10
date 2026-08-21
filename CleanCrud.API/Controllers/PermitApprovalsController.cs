using System.Security.Claims;
using CleanCrud.API.Middleware;
using CleanCrud.Application.Common;
using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCrud.API.Controllers;

[ApiController]
[Route("api/permit-approvals")]
[Authorize]
public sealed class PermitApprovalsController(IApprovalWorkflowService service) : ControllerBase
{
    [HttpGet("pending")]
    [ProducesResponseType<PermitApprovalPagedResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PermitApprovalPagedResponseDto>> Pending(
        [FromQuery] PermitApprovalQueryDto query,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();
        return Ok(await service.GetPendingAsync(userId, query, cancellationToken));
    }

    [HttpGet("approved")]
    [ProducesResponseType<ApprovedPermitPagedResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApprovedPermitPagedResponseDto>> Approved(
        [FromQuery] PermitApprovalHistoryQueryDto query,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();
        return Ok(await service.GetApprovedAsync(userId, query, cancellationToken));
    }

    [HttpGet("rejected")]
    [ProducesResponseType<RejectedPermitPagedResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RejectedPermitPagedResponseDto>> Rejected(
        [FromQuery] PermitApprovalHistoryQueryDto query,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();
        return Ok(await service.GetRejectedAsync(userId, query, cancellationToken));
    }

    [HttpPost("{id:long}/decision")]
    public async Task<ActionResult<ReturnMessageModel>> Decide(
        long id,
        ApprovalDecisionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();
        var result = await service.DecideAsync(id, request, userId, cancellationToken);
        return StatusCode(result.HttpStatusCode, result);
    }

    private bool TryGetUserId(out int userId)
    {
        var value = User.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }
}
