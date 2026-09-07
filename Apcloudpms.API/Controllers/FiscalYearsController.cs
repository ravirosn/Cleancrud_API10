using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/fiscal-years")]
[Authorize]
public sealed class FiscalYearsController(IOrganizationService organizationService) : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<ActiveFiscalYearDto>> GetCurrent(
        CancellationToken cancellationToken = default)
    {
        var fiscalYear = await organizationService.GetActiveFiscalYearAsync(cancellationToken);
        return fiscalYear is null ? NotFound() : Ok(fiscalYear);
    }
}
