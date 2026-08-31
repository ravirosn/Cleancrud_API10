using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IWorkflowSetupService
{
    Task<WorkflowSetupPagedResponseDto> GetAsync(WorkflowSetupQueryDto query, CancellationToken cancellationToken);
    Task<WorkflowSetupDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<WorkflowSetupDetailDto> CreateAsync(WorkflowSetupRequestDto request, CancellationToken cancellationToken);
    Task<WorkflowSetupDetailDto?> UpdateAsync(int id, WorkflowSetupRequestDto request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowModuleOptionDto>> GetModulesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowRoleOptionDto>> GetRolesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowSubjectCategoryOptionDto>> GetSubjectCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowSubjectOptionDto>> GetSubjectsAsync(string categoryCode, CancellationToken cancellationToken);
}
