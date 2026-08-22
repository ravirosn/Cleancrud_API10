using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed class RefreshTokenRequestDto
{
    [Required, StringLength(256, MinimumLength = 80)]
    public string RefreshToken { get; set; } = string.Empty;
}
