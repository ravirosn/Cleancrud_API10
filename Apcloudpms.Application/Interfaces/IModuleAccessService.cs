using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IModuleAccessService
{
    Task<IReadOnlyList<ApplicationModuleDto>> GetModulesAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<ApplicationModuleDto> CreateModuleAsync(ApplicationModuleRequestDto dto, CancellationToken cancellationToken);
    Task<ApplicationModuleDto?> UpdateModuleAsync(int id, ApplicationModuleRequestDto dto, CancellationToken cancellationToken);
    Task<IReadOnlyList<ModuleMenuDto>> GetMenusAsync(int moduleId, bool includeInactive, CancellationToken cancellationToken);
    Task<ModuleMenuDto?> CreateMenuAsync(int moduleId, ModuleMenuRequestDto dto, CancellationToken cancellationToken);
    Task<ModuleMenuDto?> UpdateMenuAsync(int moduleId, int menuId, ModuleMenuRequestDto dto, CancellationToken cancellationToken);
    Task<bool> SetUserModuleAsync(UserModuleAssignmentDto dto, CancellationToken cancellationToken);
    Task<IReadOnlyList<ApplicationModuleDto>> GetAssignedModulesAsync(int userId, CancellationToken cancellationToken);
    Task<ModuleNavigationDto?> SelectModuleAsync(int userId, int moduleId, CancellationToken cancellationToken);
}
