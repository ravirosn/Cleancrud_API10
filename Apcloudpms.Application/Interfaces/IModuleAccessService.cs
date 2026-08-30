using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IModuleAccessService
{
    Task<ApplicationModulePagedResponseDto> GetModulesAsync(ApplicationModuleQueryDto query, CancellationToken cancellationToken);
    Task<ApplicationModuleDto> CreateModuleAsync(ApplicationModuleRequestDto dto, CancellationToken cancellationToken);
    Task<ApplicationModuleDto?> UpdateModuleAsync(int id, ApplicationModuleRequestDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteModuleAsync(int id, CancellationToken cancellationToken);
    Task<ApplicationModuleConfigurationDto?> GetModuleConfigurationAsync(int id, CancellationToken cancellationToken);
    Task<ApplicationModuleConfigurationDto?> SetModuleRolesAsync(int id, RoleModuleAssignmentRequestDto dto, CancellationToken cancellationToken);
    Task<ApplicationModuleConfigurationDto?> UpdateModuleConfigurationAsync(int id, ApplicationModuleConfigurationRequestDto dto, CancellationToken cancellationToken);
    Task<IReadOnlyList<ModuleMenuDto>> GetMenusAsync(int moduleId, bool includeInactive, CancellationToken cancellationToken);
    Task<ModuleMenuDto?> CreateMenuAsync(int moduleId, ModuleMenuRequestDto dto, CancellationToken cancellationToken);
    Task<ModuleMenuDto?> UpdateMenuAsync(int moduleId, int menuId, ModuleMenuRequestDto dto, CancellationToken cancellationToken);
    Task<RoleModuleMenuConfigurationDto?> GetRoleModuleMenusAsync(int moduleId, int roleId, CancellationToken cancellationToken);
    Task<RoleModuleMenuConfigurationDto?> SetRoleModuleMenusAsync(int moduleId, int roleId, RoleModuleMenuAssignmentRequestDto dto, CancellationToken cancellationToken);
    Task<IReadOnlyList<ApplicationModuleDto>> GetAssignedModulesAsync(int userId, CancellationToken cancellationToken);
    Task<ModuleNavigationDto?> SelectModuleAsync(int userId, int moduleId, CancellationToken cancellationToken);
}
