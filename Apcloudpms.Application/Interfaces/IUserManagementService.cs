using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IUserManagementService
{
    Task<UserManagementPagedResponseDto> GetUsersAsync(UserManagementQueryDto query, CancellationToken cancellationToken);
    Task<UserManagementDto> CreateUserAsync(UserCreateRequestDto request, CancellationToken cancellationToken);
    Task<UserManagementDto?> UpdateUserAsync(int id, UserUpdateRequestDto request, CancellationToken cancellationToken);
    Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken);
    Task<UserRoleConfigurationDto?> GetUserRolesAsync(int id, CancellationToken cancellationToken);
    Task<UserRoleConfigurationDto?> SetUserRolesAsync(int id, UserRolesUpdateRequestDto request, CancellationToken cancellationToken);
}
