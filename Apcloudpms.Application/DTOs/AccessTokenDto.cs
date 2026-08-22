namespace Apcloudpms.Application.DTOs;

public sealed record AccessTokenDto(string Token, DateTime ExpiresAtUtc);
