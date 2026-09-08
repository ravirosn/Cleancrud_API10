using System.Security.Claims;
using Apcloud.Contracts.Permissions;
using Apcloudpms.API.Authorization;
using Apcloudpms.API.Middleware;
using Apcloudpms.Application.Common;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Apcloud.Contracts.Common;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/risk-assessments")]
[Authorize]
[RequireMenu(ApplicationPermissions.PermitModule, "PermitApplications", "Index")]
public sealed class RiskAssessmentsController(
    IRiskAssessmentService service,
    IListItemService listItemService,
    IApprovalWorkflowService approvalWorkflowService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(ApplicationPermissions.RiskAssessments.View)]
    public async Task<ActionResult<RiskAssessmentPagedResponseDto>> GetPaged(
        [FromQuery] RiskAssessmentQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(ApplicationPermissions.RiskAssessments.View)]
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
    [RequirePermission(ApplicationPermissions.RiskAssessments.View)]
    public async Task<ActionResult<IReadOnlyList<RiskAssessmentPermitApplicationDto>>>
        GetPermitApplications(
            int riskAssessmentId,
            CancellationToken cancellationToken)
    {
        var result = await service.GetPermitApplicationsAsync(
            riskAssessmentId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("options/categories")]
    [RequirePermission(ApplicationPermissions.RiskAssessments.View)]
    public async Task<ActionResult<IReadOnlyList<ListItemCategoryOptionDto>>> GetCategoryOptions(
        CancellationToken cancellationToken = default) =>
        Ok(await listItemService.GetCategoryOptionsAsync(cancellationToken));

    [HttpGet("options/category/{categoryName}")]
    [RequirePermission(ApplicationPermissions.RiskAssessments.View)]
    public async Task<ActionResult<IReadOnlyList<ListItemDto>>> GetOptionsByCategory(
        string categoryName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return BadRequest("Category name is required.");

        return Ok(await listItemService.GetByCategoryAsync(categoryName, cancellationToken));
    }

    [HttpGet("options/users")]
    [RequirePermission(ApplicationPermissions.RiskAssessments.View)]
    public async Task<ActionResult<IReadOnlyList<RiskAssessmentUserOptionDto>>> GetUserOptions(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();

        return Ok(await service.GetUserOptionsAsync(userId, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(ApplicationPermissions.RiskAssessments.Create)]
    public async Task<ActionResult<RiskAssessmentWriteResponseDto>> Create(
        RiskAssessmentRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();

        var result = await service.CreateAsync(request, userId, cancellationToken);
        return result.Outcome switch
        {
            RiskAssessmentWriteOutcome.Success when result.Value is not null =>
                StatusCode(StatusCodes.Status201Created, result.Value),
            RiskAssessmentWriteOutcome.InvalidUsers =>
                BadRequest(new { message = "Select active permit issuer and receiver users." }),
            RiskAssessmentWriteOutcome.StatusNotConfigured =>
                Conflict(new { message = "The required Draft risk-assessment or permit status is not configured." }),
            RiskAssessmentWriteOutcome.NumberingNotConfigured =>
                Conflict(new { message = "An active fiscal year with valid RA and PA numbering settings is required." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPut("{id:int}")]
    [RequirePermission(ApplicationPermissions.RiskAssessments.Edit)]
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
            RiskAssessmentWriteOutcome.NotEditable => Conflict(new
            {
                message = "Only risk assessments in Draft or Rejected status can be updated."
            }),
            RiskAssessmentWriteOutcome.InvalidUsers =>
                BadRequest(new { message = "Select active permit issuer and receiver users." }),
            RiskAssessmentWriteOutcome.StatusNotConfigured =>
                Conflict(new { message = "The required Draft risk-assessment or permit status is not configured." }),
            RiskAssessmentWriteOutcome.NumberingNotConfigured =>
                Conflict(new { message = "An active fiscal year with valid RA and PA numbering settings is required." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPut("{id:int}/creation-progress")]
    [RequirePermission(ApplicationPermissions.RiskAssessments.Create)]
    public async Task<ActionResult<RiskAssessmentWriteResponseDto>> ContinueCreate(
        int id,
        RiskAssessmentRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();

        var result = await service.ContinueCreateAsync(id, request, userId, cancellationToken);
        return WriteResult(result);
    }

    [HttpPost("{id:int}/submit")]
    [RequirePermission(ApplicationPermissions.RiskAssessments.Submit)]
    public async Task<ActionResult<ReturnMessageModel>> Submit(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();

        var result = await approvalWorkflowService.SubmitRiskAssessmentAsync(
            id, userId, cancellationToken);
        return StatusCode(result.HttpStatusCode, result);
    }

    private bool TryGetUserId(out int userId)
    {
        var value = User.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }

    private ActionResult<RiskAssessmentWriteResponseDto> WriteResult(
        RiskAssessmentWriteResult result) => result.Outcome switch
    {
        RiskAssessmentWriteOutcome.Success => Ok(result.Value),
        RiskAssessmentWriteOutcome.NotFound =>
            NotFound(new { message = "Risk assessment was not found." }),
        RiskAssessmentWriteOutcome.NotEditable => Conflict(new
        {
            message = "Only risk assessments in Draft or Rejected status can be updated."
        }),
        RiskAssessmentWriteOutcome.InvalidUsers =>
            BadRequest(new { message = "Select active permit issuer and receiver users." }),
        RiskAssessmentWriteOutcome.StatusNotConfigured => Conflict(new
        {
            message = "The required Draft risk-assessment or permit status is not configured."
        }),
        RiskAssessmentWriteOutcome.NumberingNotConfigured => Conflict(new
        {
            message = "An active fiscal year with valid RA and PA numbering settings is required."
        }),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };
}
