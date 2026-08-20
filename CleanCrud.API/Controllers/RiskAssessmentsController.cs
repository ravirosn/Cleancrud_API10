using System.Security.Claims;
using CleanCrud.API.Middleware;
using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCrud.API.Controllers;

[ApiController]
[Route("api/risk-assessments")]
[Authorize]
public sealed class RiskAssessmentsController(
    IRiskAssessmentService service,
    IApprovalWorkflowService approvalWorkflowService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RiskAssessmentPagedResponseDto>> GetPaged(
        [FromQuery] RiskAssessmentQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RiskAssessmentDetailsDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result is null
            ? NotFound(new { message = "Risk assessment was not found." })
            : Ok(result);
    }

    [HttpGet("{riskAssessmentId:int}/permit-applications")]
    public async Task<ActionResult<IReadOnlyList<RiskAssessmentPermitApplicationDto>>>
        GetPermitApplications(
            int riskAssessmentId,
            CancellationToken cancellationToken)
    {
        var result = await service.GetPermitApplicationsAsync(
            riskAssessmentId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<RiskAssessmentWriteResponseDto>> Create(
        RiskAssessmentRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();

        var result = await service.CreateAsync(request, userId, cancellationToken);
        if (result.Outcome != RiskAssessmentWriteOutcome.Success || result.Value is null)
            return StatusCode(StatusCodes.Status500InternalServerError);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RiskAssessmentWriteResponseDto>> Update(
        int id,
        RiskAssessmentRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();

        var result = await service.UpdateAsync(id, request, userId, cancellationToken);
        return result.Outcome switch
        {
            RiskAssessmentWriteOutcome.Success => Ok(result.Value),
            RiskAssessmentWriteOutcome.NotFound => NotFound(new { message = "Risk assessment was not found." }),
            RiskAssessmentWriteOutcome.NotDraft => Conflict(new
            {
                message = "Only risk assessments in Draft status can be updated."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();

        var result = await approvalWorkflowService.SubmitRiskAssessmentAsync(
            id, userId, cancellationToken);
        return result.Outcome switch
        {
            ApprovalOperationOutcome.Success => Ok(new
            {
                message = "Risk assessment and child permits were submitted for approval."
            }),
            ApprovalOperationOutcome.NotFound => NotFound(new
            {
                message = "Risk assessment was not found."
            }),
            ApprovalOperationOutcome.NotDraft => Conflict(new { message = result.Message }),
            ApprovalOperationOutcome.NoPermitApplications => Conflict(new { message = result.Message }),
            ApprovalOperationOutcome.PermitApplicationsNotFinalized => Conflict(new
            {
                message = result.Message
            }),
            ApprovalOperationOutcome.MissingWorkflow => Conflict(new { message = result.Message }),
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
