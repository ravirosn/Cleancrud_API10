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
public sealed class RiskAssessmentsController(IRiskAssessmentService service) : ControllerBase
{
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

    private bool TryGetUserId(out int userId)
    {
        var value = User.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }
}
