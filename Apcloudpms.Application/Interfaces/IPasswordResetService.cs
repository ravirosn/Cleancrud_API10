namespace Apcloudpms.Application.Interfaces;

public interface IPasswordResetService
{
    Task RequestAsync(string userNameOrEmail, string? ipAddress, CancellationToken cancellationToken = default);
    Task<bool> ResetAsync(string token, string newPassword, string? ipAddress, CancellationToken cancellationToken = default);
}
