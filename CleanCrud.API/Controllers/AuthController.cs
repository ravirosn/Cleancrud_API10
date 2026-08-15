using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CleanCrud.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public AuthController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        LoginDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(dto, GetClientIp(), cancellationToken);
        SetNoStoreHeaders();
        return result is null
            ? Unauthorized(new { Message = "Invalid username or password." })
            : Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(
        RefreshTokenRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(
            dto.RefreshToken, GetClientIp(), cancellationToken);
        SetNoStoreHeaders();
        return result is null
            ? Unauthorized(new { Message = "The refresh token is invalid or expired." })
            : Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(
        RevokeTokenRequestDto dto, CancellationToken cancellationToken)
    {
        // Idempotent response avoids revealing whether a refresh token exists.
        await _authService.RevokeAsync(dto.RefreshToken, GetClientIp(), cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        await _userService.AddUserAsync(dto);
        return StatusCode(StatusCodes.Status201Created, new
        {
            Success = true,
            Message = "User registered successfully."
        });
    }

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private void SetNoStoreHeaders()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
    }
}
