using System.Security.Claims;
using CleanCrud.API.Middleware;
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
    public async Task<ActionResult<IReadOnlyList<PermitApprovalDto>>> Pending(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();
        return Ok(await service.GetPendingAsync(userId, cancellationToken));
    }

    [HttpPost("{id:long}/decision")]
    public async Task<IActionResult> Decide(
        long id,
        ApprovalDecisionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();
        var result = await service.DecideAsync(id, request, userId, cancellationToken);
        return result.Outcome switch
        {
            ApprovalOperationOutcome.Success => Ok(new { message = "Approval decision recorded." }),
            ApprovalOperationOutcome.NotFound => NotFound(new { message = "Approval was not found." }),
            ApprovalOperationOutcome.NotEligible => StatusCode(StatusCodes.Status403Forbidden,
                new { message = result.Message }),
            ApprovalOperationOutcome.NotPending => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    private bool TryGetUserId(out int userId)
    {
        var value = User.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }
}
