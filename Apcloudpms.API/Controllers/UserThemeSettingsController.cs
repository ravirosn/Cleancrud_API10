using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Apcloudpms.API.Middleware;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Apcloud.Contracts.Themes;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Authorize]
[Route("api/user-theme-settings")]
public sealed class UserThemeSettingsController(IUserThemeSettingService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<UserThemeSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserThemeSettingsDto>> Get(CancellationToken cancellationToken)
    {
        return TryGetUserId(out var userId)
            ? Ok(await service.GetAsync(userId, cancellationToken))
            : Unauthorized(new { Message = "The access token does not identify a local user." });
    }

    [HttpPut]
    [ProducesResponseType<UserThemeSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserThemeSettingsDto>> Put(
        UpdateUserThemeSettingsDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { Message = "The access token does not identify a local user." });
        }

        try
        {
            return Ok(await service.UpsertAsync(userId, request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { exception.Message });
        }
    }

    private bool TryGetUserId(out int userId)
    {
        var value = User.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }
}
