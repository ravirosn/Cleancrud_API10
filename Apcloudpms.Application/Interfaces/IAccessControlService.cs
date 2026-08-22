using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IAccessControlService
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<RoleDto> CreateRoleAsync(RoleRequestDto dto, CancellationToken cancellationToken);
    Task<RoleDto?> UpdateRoleAsync(int id, RoleRequestDto dto, CancellationToken cancellationToken);
    Task<bool> SetUserRoleAsync(UserRoleAssignmentDto dto, CancellationToken cancellationToken);
}
