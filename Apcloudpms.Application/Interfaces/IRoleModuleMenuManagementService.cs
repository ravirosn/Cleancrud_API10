using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IRoleModuleMenuManagementService
{
    Task<RoleModuleMenuPagedResponseDto> GetAsync(RoleModuleMenuQueryDto query, CancellationToken cancellationToken);
    Task<IReadOnlyList<RoleModuleMenuRoleOptionDto>> GetRoleOptionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<RoleModuleMenuModuleOptionDto>> GetModuleOptionsAsync(int roleId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RoleModuleMenuMenuOptionDto>> GetMenuOptionsAsync(int roleId, int moduleId, CancellationToken cancellationToken);
    Task<RoleModuleMenuManagementDto> CreateAsync(RoleModuleMenuManagementRequestDto request, CancellationToken cancellationToken);
    Task<RoleModuleMenuManagementDto?> UpdateAsync(int roleId, int moduleId, int menuId, RoleModuleMenuManagementRequestDto request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int roleId, int moduleId, int menuId, CancellationToken cancellationToken);
}
