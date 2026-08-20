using System.Security.Claims;
using CleanCrud.API.Middleware;
using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCrud.API.Controllers;

[ApiController]
[Route("api/permit/applications")]
[Authorize]
public sealed class PermitApplicationsController(IPermitApplicationService service) : ControllerBase
{
    [HttpGet("{id:long}")]
    [ProducesResponseType<PermitApplicationDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PermitApplicationDetailsDto>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var permitApplication = await service.GetByIdAsync(id, cancellationToken);
        return permitApplication is null
            ? NotFound(new { message = "Permit application was not found." })
            : Ok(permitApplication);
    }

    [HttpGet]
    [ProducesResponseType<PermitApplicationPagedResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PermitApplicationPagedResponseDto>> GetCreatedByCurrentUser(
        [FromQuery] PermitApplicationQueryDto query,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();

        var permitApplications = await service.GetByCreatedUserAsync(
            userId, query, cancellationToken);
        return Ok(permitApplications);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType<PermitApplicationUpdateResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PermitApplicationUpdateResponseDto>> Update(
        long id,
        PermitApplicationUpdateRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();

        var result = await service.UpdateAsync(id, request, userId, cancellationToken);
        return result.Outcome switch
        {
            PermitApplicationUpdateOutcome.Success => Ok(result.Value),
            PermitApplicationUpdateOutcome.NotFound =>
                NotFound(new { message = "Permit application was not found." }),
            PermitApplicationUpdateOutcome.NotEditable =>
                Conflict(new { message = result.Message }),
            PermitApplicationUpdateOutcome.UnsupportedPermitType =>
                Conflict(new { message = result.Message }),
            PermitApplicationUpdateOutcome.InvalidSelections =>
                BadRequest(new { message = result.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPut("{id:long}/finalize")]
    [ProducesResponseType<PermitApplicationUpdateResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PermitApplicationUpdateResponseDto>> UpdateAndFinalize(
        long id,
        PermitApplicationUpdateRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();

        var result = await service.UpdateAndFinalizeAsync(
            id, request, userId, cancellationToken);
        return result.Outcome switch
        {
            PermitApplicationUpdateOutcome.Success => Ok(result.Value),
            PermitApplicationUpdateOutcome.NotFound =>
                NotFound(new { message = "Permit application was not found." }),
            PermitApplicationUpdateOutcome.NotEditable =>
                Conflict(new { message = result.Message }),
            PermitApplicationUpdateOutcome.UnsupportedPermitType =>
                Conflict(new { message = result.Message }),
            PermitApplicationUpdateOutcome.InvalidSelections =>
                BadRequest(new { message = result.Message }),
            PermitApplicationUpdateOutcome.StatusNotConfigured =>
                Problem(result.Message, statusCode: StatusCodes.Status500InternalServerError),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPatch("{id:long}/completion")]
    [ProducesResponseType<PermitApplicationActionResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PermitApplicationActionResponseDto>> Complete(
        long id,
        PermitApplicationRemarksRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();

        var result = await service.CompleteAsync(
            id, request.Remarks, userId, cancellationToken);
        return result is null
            ? NotFound(new { message = "Permit application was not found." })
            : Ok(result);
    }

    [HttpPatch("{id:long}/cancellation")]
    [ProducesResponseType<PermitApplicationActionResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PermitApplicationActionResponseDto>> Cancel(
        long id,
        PermitApplicationRemarksRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();

        var result = await service.CancelAsync(
            id, request.Remarks, userId, cancellationToken);
        return result is null
            ? NotFound(new { message = "Permit application was not found." })
            : Ok(result);
    }

    private bool TryGetUserId(out int userId)
    {
        var value = User.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }
}
