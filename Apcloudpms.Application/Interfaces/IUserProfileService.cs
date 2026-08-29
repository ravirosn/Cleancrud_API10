using Apcloudpms.Application.DTOs;
using Microsoft.AspNetCore.Http;

using Apcloud.Contracts.Profiles;

namespace Apcloudpms.Application.Interfaces;

public interface IUserProfileService
{
    Task<UserProfileUpdateDto?> UpdateAsync(
        int userId,
        UpdateUserProfileDto request,
        CancellationToken cancellationToken = default);

    Task<ChangePasswordOutcome> ChangePasswordAsync(
        int userId,
        ChangePasswordDto request,
        CancellationToken cancellationToken = default);

    Task<ProfilePictureUploadDto?> UploadPictureAsync(
        int userId,
        IFormFile picture,
        CancellationToken cancellationToken = default);

    Task<StoredProfileImage?> GetPictureAsync(
        int userId,
        CancellationToken cancellationToken = default);
}

public sealed record StoredProfileImage(Stream Content, string ContentType);
