using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CleanCrud.API.Middleware;

namespace CleanCrud.API.Controllers;

[ApiController]
[Route("api/module-access")]
[Authorize]
public sealed class ModuleAccessController : ControllerBase
{
    private readonly IModuleAccessService _service;

    public ModuleAccessController(IModuleAccessService service) => _service = service;

    [HttpGet("my-modules")]
    public async Task<ActionResult<IReadOnlyList<ApplicationModuleDto>>> GetMyModules(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _service.GetAssignedModulesAsync(userId, cancellationToken));
    }

    [HttpPost("select")]
    public async Task<ActionResult<ModuleNavigationDto>> SelectModule(
        ModuleSelectionRequestDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.SelectModuleAsync(
            userId, dto.ApplicationModuleId, cancellationToken);
        return result is null ? Forbid() : Ok(result);
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId) && userId > 0;
}
