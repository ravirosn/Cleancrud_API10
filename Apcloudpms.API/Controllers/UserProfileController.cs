using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Apcloud.Contracts.Common;
using Apcloud.Contracts.Profiles;
using Apcloudpms.API.Middleware;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Authorize]
[Route("api/user-profile")]
public sealed class UserProfileController(IUserProfileService service) : ControllerBase
{
    [HttpPut]
    [ProducesResponseType<ReturnMessageModel<UserProfileUpdateDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReturnMessageModel<UserProfileUpdateDto>>> Update(
        UpdateUserProfileDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return StatusCode(
                StatusCodes.Status401Unauthorized,
                ReturnMessageModel<UserProfileUpdateDto>.Failure(
                    "The access token does not identify a local user.",
                    StatusCodes.Status401Unauthorized));
        }

        var result = await service.UpdateAsync(userId, request, cancellationToken);
        return result is null
            ? NotFound(ReturnMessageModel<UserProfileUpdateDto>.Failure(
                "The current user no longer exists.",
                StatusCodes.Status404NotFound))
            : Ok(ReturnMessageModel<UserProfileUpdateDto>.Success(
                result,
                "Your profile details were updated successfully."));
    }

    [HttpPut("password")]
    [ProducesResponseType<ReturnMessageModel>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReturnMessageModel>> ChangePassword(
        ChangePasswordDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return StatusCode(
                StatusCodes.Status401Unauthorized,
                ReturnMessageModel.Failure(
                    "The access token does not identify a local user.",
                    StatusCodes.Status401Unauthorized));
        }

        var outcome = await service.ChangePasswordAsync(userId, request, cancellationToken);
        return outcome switch
        {
            ChangePasswordOutcome.Success => Ok(ReturnMessageModel.Success(
                "Your password was changed successfully.")),
            ChangePasswordOutcome.UserNotFound => NotFound(ReturnMessageModel.Failure(
                "The current user no longer exists.",
                StatusCodes.Status404NotFound)),
            ChangePasswordOutcome.CurrentPasswordIncorrect => BadRequest(ReturnMessageModel.Failure(
                "The current password is incorrect.",
                StatusCodes.Status400BadRequest)),
            ChangePasswordOutcome.ExternallyManagedAccount => Conflict(ReturnMessageModel.Failure(
                "This account's password is managed by Microsoft.",
                StatusCodes.Status409Conflict)),
            ChangePasswordOutcome.InvalidPassword => BadRequest(ReturnMessageModel.Failure(
                "The new password must be different and contain uppercase, lowercase, and numeric characters.",
                StatusCodes.Status400BadRequest)),
            _ => StatusCode(
                StatusCodes.Status500InternalServerError,
                ReturnMessageModel.Failure(
                    "The password could not be changed.",
                    StatusCodes.Status500InternalServerError))
        };
    }

    [HttpPost("photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType<ReturnMessageModel<ProfilePictureUploadDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReturnMessageModel<ProfilePictureUploadDto>>> UploadPhoto(
        IFormFile? picture,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return StatusCode(
                StatusCodes.Status401Unauthorized,
                ReturnMessageModel<ProfilePictureUploadDto>.Failure(
                    "The access token does not identify a local user.",
                    StatusCodes.Status401Unauthorized));
        }

        if (picture is null || picture.Length == 0)
        {
            return BadRequest(ReturnMessageModel<ProfilePictureUploadDto>.Failure(
                "Select a profile picture to upload.",
                StatusCodes.Status400BadRequest));
        }

        try
        {
            var result = await service.UploadPictureAsync(userId, picture, cancellationToken);
            return result is null
                ? NotFound(ReturnMessageModel<ProfilePictureUploadDto>.Failure(
                    "The current user no longer exists.",
                    StatusCodes.Status404NotFound))
                : Ok(ReturnMessageModel<ProfilePictureUploadDto>.Success(
                    result,
                    "Your profile picture was updated successfully."));
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(ReturnMessageModel<ProfilePictureUploadDto>.Failure(
                exception.Message,
                StatusCodes.Status400BadRequest));
        }
    }

    [HttpGet("photo")]
    public async Task<IActionResult> GetPhoto(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var image = await service.GetPictureAsync(userId, cancellationToken);
        if (image is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "private, max-age=86400";
        return File(image.Content, image.ContentType);
    }

    private bool TryGetUserId(out int userId)
    {
        var value = User.FindFirstValue(EntraUserMiddleware.LocalUserIdClaim)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }
}
