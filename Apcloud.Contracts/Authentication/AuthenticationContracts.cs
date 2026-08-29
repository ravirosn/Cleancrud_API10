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

public sealed record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    string TokenType = "Bearer");

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
