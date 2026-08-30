using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IAccessControlService
{
    Task<RolePagedResponseDto> GetRolesAsync(RoleQueryDto query, CancellationToken cancellationToken);
    Task<RoleDto> CreateRoleAsync(RoleRequestDto dto, CancellationToken cancellationToken);
    Task<RoleDto?> UpdateRoleAsync(int id, RoleRequestDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteRoleAsync(int id, CancellationToken cancellationToken);
    Task<bool> SetUserRoleAsync(UserRoleAssignmentDto dto, CancellationToken cancellationToken);
}
