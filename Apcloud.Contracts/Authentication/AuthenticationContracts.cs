using System.ComponentModel.DataAnnotations;

namespace Apcloud.Contracts.Authentication;

public sealed class LoginDto
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshTokenRequestDto
{
    [Required, StringLength(256, MinimumLength = 80)]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class RevokeTokenRequestDto
{
    [Required, StringLength(256, MinimumLength = 80)]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class ForgotPasswordRequestDto
{
    [Required, StringLength(320, MinimumLength = 3)]
    public string UserNameOrEmail { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequestDto
{
    [Required, StringLength(512, MinimumLength = 32)]
    public string Token { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^A-Za-z0-9]).{12,128}$",
        ErrorMessage = "Password must include uppercase, lowercase, number, and special characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    string TokenType = "Bearer",
    CurrentUserDetailsDto? User = null);

public sealed record CurrentUserDetailsDto(
    int Id,
    string UserName,
    string? DisplayName,
    string? Email,
    string? ContactNumber,
    Guid? EntraTenantId,
    Guid? EntraObjectId,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<string> Roles,
    int? DepartmentId,
    string? DepartmentName,
    int? BranchId,
    string? BranchName,
    string? ProfilePictureUrl);
