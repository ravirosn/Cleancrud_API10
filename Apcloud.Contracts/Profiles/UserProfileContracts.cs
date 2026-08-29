using System.ComponentModel.DataAnnotations;

namespace Apcloud.Contracts.Profiles;

public sealed class UpdateUserProfileDto
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? ContactNumber { get; set; }
}

public sealed class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed record UserProfileUpdateDto(
    string DisplayName,
    string? ContactNumber,
    string? ProfilePictureUrl);

public sealed record ProfilePictureUploadDto(string ProfilePictureUrl);
