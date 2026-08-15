using CleanCrud.Application.DTOs;

namespace CleanCrud.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto dto, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthResponseDto?> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);
}
