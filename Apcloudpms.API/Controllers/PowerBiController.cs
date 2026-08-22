using Apcloudpms.API.Authorization;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/power-bi")]
[RequireModule("POWERBI")]
public sealed class PowerBiController : ControllerBase
{
    private readonly IPowerBiService _service;

    public PowerBiController(IPowerBiService service) => _service = service;

    [HttpGet("embed-config")]
    public async Task<ActionResult<PowerBiEmbedConfigDto>> GetEmbedConfig(
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return Ok(await _service.GetEmbedConfigAsync(cancellationToken));
    }
}
