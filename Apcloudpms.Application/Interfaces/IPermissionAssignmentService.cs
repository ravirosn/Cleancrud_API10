using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IPermissionAssignmentService
{
    Task<PermissionAssignmentPagedResponseDto> GetAsync(PermissionAssignmentQueryDto query, CancellationToken cancellationToken);
    Task<IReadOnlyList<PermissionRoleOptionDto>> GetRolesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PermissionPolicyOptionDto>> GetPoliciesAsync(int roleId, CancellationToken cancellationToken);
    Task<PermissionAssignmentDto> CreateAsync(PermissionAssignmentRequestDto request, CancellationToken cancellationToken);
    Task<PermissionAssignmentDto?> UpdateAsync(int roleId, int permissionPolicyId, PermissionAssignmentRequestDto request, CancellationToken cancellationToken);
    Task<BulkPermissionAssignmentResultDto> SetBulkAsync(int roleId, BulkPermissionAssignmentRequestDto request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int roleId, int permissionPolicyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetGrantedCodesAsync(int userId, string moduleCode, string menuController, string menuAction, CancellationToken cancellationToken);
    Task<bool> HasMenuAccessAsync(int userId, string moduleCode, string menuController, string menuAction, CancellationToken cancellationToken);
    Task<bool> HasPermissionAsync(int userId, string permissionCode, CancellationToken cancellationToken);
}
