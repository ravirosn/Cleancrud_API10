using Apcloud.Contracts.Authentication;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Domain.Entities;

namespace Apcloudpms.Application.Interfaces;

public interface IUserService
{
    Task<User?> LoginAsync(LoginDto dto);

    Task<CurrentUserDetailsDto?> GetCurrentUserDetailsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task AddUserAsync(RegisterDto user);
}
