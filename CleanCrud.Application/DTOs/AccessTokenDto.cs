namespace CleanCrud.Application.DTOs;

public sealed record AccessTokenDto(string Token, DateTime ExpiresAtUtc);
