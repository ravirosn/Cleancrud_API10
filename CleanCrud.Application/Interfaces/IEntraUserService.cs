using CleanCrud.Application.DTOs;

namespace CleanCrud.Application.Interfaces;

public interface IEntraUserService
{
    Task<AuthenticatedUserDto?> EnsureUserAsync(
        EntraUserProfileDto profile, CancellationToken cancellationToken);
}
