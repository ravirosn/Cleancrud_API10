using System.Text;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Data;
using Apcloudpms.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using Apcloud.Contracts.Profiles;

namespace Apcloudpms.Infrastructure.Services;

public sealed class UserProfileService(
    AppDbContext context,
    IPasswordService passwordService,
    IProfileImageStorage imageStorage,
    IOptions<ProfileImageStorageOptions> storageOptions) : IUserProfileService
{
    private readonly ProfileImageStorageOptions _storageOptions = storageOptions.Value;

    public async Task<UserProfileUpdateDto?> UpdateAsync(
        int userId,
        UpdateUserProfileDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.DisplayName = request.DisplayName.Trim();
        user.ContactNumber = NullIfWhiteSpace(request.ContactNumber);
        await context.SaveChangesAsync(cancellationToken);
        return new UserProfileUpdateDto(
            user.DisplayName,
            user.ContactNumber,
            GetPictureUrl(user.ProfilePicturePath, user.ProfilePictureUpdatedAtUtc));
    }

    public async Task<ChangePasswordOutcome> ChangePasswordAsync(
        int userId,
        ChangePasswordDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return ChangePasswordOutcome.UserNotFound;
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return ChangePasswordOutcome.ExternallyManagedAccount;
        }

        if (Encoding.UTF8.GetByteCount(request.CurrentPassword) > 72 ||
            !passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return ChangePasswordOutcome.CurrentPasswordIncorrect;
        }

        if (!IsValidNewPassword(request.NewPassword) ||
            passwordService.VerifyPassword(request.NewPassword, user.PasswordHash))
        {
            return ChangePasswordOutcome.InvalidPassword;
        }

        user.PasswordHash = passwordService.HashPassword(request.NewPassword);
        await context.SaveChangesAsync(cancellationToken);
        return ChangePasswordOutcome.Success;
    }

    public async Task<ProfilePictureUploadDto?> UploadPictureAsync(
        int userId,
        IFormFile picture,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        await using var processed = await ProcessImageAsync(picture, cancellationToken);
        var newPath = await imageStorage.SaveAsync(processed, ".jpg", cancellationToken);
        var oldPath = user.ProfilePicturePath;
        try
        {
            user.ProfilePicturePath = newPath;
            user.ProfilePictureUpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await imageStorage.DeleteAsync(newPath, cancellationToken);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(oldPath))
        {
            await imageStorage.DeleteAsync(oldPath, cancellationToken);
        }

        return new ProfilePictureUploadDto(
            GetPictureUrl(user.ProfilePicturePath, user.ProfilePictureUpdatedAtUtc)!);
    }

    public async Task<StoredProfileImage?> GetPictureAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var path = await context.Users.AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => item.ProfilePicturePath)
            .SingleOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(path)
            ? null
            : await imageStorage.OpenReadAsync(path, cancellationToken);
    }

    private async Task<MemoryStream> ProcessImageAsync(
        IFormFile picture,
        CancellationToken cancellationToken)
    {
        if (picture.Length <= 0 || picture.Length > _storageOptions.MaximumUploadBytes)
        {
            throw new InvalidDataException($"Select an image no larger than {_storageOptions.MaximumUploadBytes / 1024 / 1024} MB.");
        }

        if (picture.ContentType is not ("image/jpeg" or "image/png" or "image/webp"))
        {
            throw new InvalidDataException("Only JPEG, PNG, and WebP profile pictures are allowed.");
        }

        try
        {
            await using var source = picture.OpenReadStream();
            using var image = await Image.LoadAsync<Rgba32>(source, cancellationToken);
            if ((long)image.Width * image.Height > 40_000_000)
            {
                throw new InvalidDataException("The image dimensions are too large.");
            }

            image.Mutate(operation =>
            {
                operation.AutoOrient();
                operation.BackgroundColor(Color.White);
            });
            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.XmpProfile = null;

            ResizeToMaximum(image, _storageOptions.MaximumDimension);
            var output = new MemoryStream();
            var encoder = new JpegEncoder { Quality = 92 };
            await image.SaveAsJpegAsync(output, encoder, cancellationToken);

            while (output.Length > _storageOptions.MaximumSavedBytes && image.Width > 320 && image.Height > 320)
            {
                image.Mutate(operation => operation.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(
                        Math.Max(320, (int)(image.Width * .85)),
                        Math.Max(320, (int)(image.Height * .85)))
                }));
                output.SetLength(0);
                await image.SaveAsJpegAsync(output, encoder, cancellationToken);
            }

            if (output.Length > _storageOptions.MaximumSavedBytes)
            {
                output.Dispose();
                throw new InvalidDataException("The processed profile picture could not be reduced below 2 MB.");
            }

            output.Position = 0;
            return output;
        }
        catch (UnknownImageFormatException exception)
        {
            throw new InvalidDataException("The selected file is not a valid supported image.", exception);
        }
        catch (InvalidImageContentException exception)
        {
            throw new InvalidDataException("The selected image is damaged or invalid.", exception);
        }
    }

    private static void ResizeToMaximum(Image image, int maximumDimension)
    {
        if (image.Width <= maximumDimension && image.Height <= maximumDimension)
        {
            return;
        }

        image.Mutate(operation => operation.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maximumDimension, maximumDimension)
        }));
    }

    private static bool IsValidNewPassword(string password) =>
        Encoding.UTF8.GetByteCount(password) <= 72 &&
        password.Length >= 8 &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsLower) &&
        password.Any(char.IsDigit);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? GetPictureUrl(string? path, DateTime? updatedAtUtc) =>
        path is null ? null : $"/api/user-profile/photo?v={updatedAtUtc?.Ticks ?? 0}";
}
