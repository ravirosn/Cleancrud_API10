using System.ComponentModel.DataAnnotations;

namespace CleanCrud.Application.DTOs;

public sealed class RevokeTokenRequestDto
{
    [Required, StringLength(256, MinimumLength = 80)]
    public string RefreshToken { get; set; } = string.Empty;
}
