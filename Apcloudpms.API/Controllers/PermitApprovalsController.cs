using System.Security.Claims;
using Apcloudpms.API.Middleware;
using Apcloudpms.Application.Common;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/permit-approvals")]
[Authorize]
public sealed class PermitApprovalsController(IApprovalWorkflowService service) : ControllerBase
{
    [HttpGet("admin/pending-assignments")]
    [Authorize(Roles = nameof(ApplicationRole.SuperAdmin) + "," + nameof(ApplicationRole.Admin))]
    [ProducesResponseType<AdminPendingApprovalPagedResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminPendingApprovalPagedResponseDto>>
        AdminPendingAssignments(
            [FromQuery] AdminPendingApprovalQueryDto query,
            CancellationToken cancellationToken) =>
        Ok(await service.GetAdminPendingAssignmentsAsync(query, cancellationToken));

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

    [HttpPut("alternate-users")]
    [Authorize(Roles = nameof(ApplicationRole.SuperAdmin) + "," + nameof(ApplicationRole.Admin))]
    [ProducesResponseType<IReadOnlyList<AlternateApproverAssignmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<AlternateApproverAssignmentDto>>>
        AssignAlternateUsers(
            AlternateApproverAssignmentRequestDto request,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();

        var result = await service.AssignAlternateUsersAsync(
            request, userId, cancellationToken);
        return result.Error is null
            ? Ok(result.Data)
            : StatusCode(result.StatusCode, new { message = result.Error });
    }

    private bool TryGetUserId(out int userId)
    {
        var value = User.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }
}
