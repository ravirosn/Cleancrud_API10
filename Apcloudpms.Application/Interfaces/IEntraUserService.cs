using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IEntraUserService
{
    Task<AuthenticatedUserDto?> EnsureUserAsync(
        EntraUserProfileDto profile, CancellationToken cancellationToken);
}
