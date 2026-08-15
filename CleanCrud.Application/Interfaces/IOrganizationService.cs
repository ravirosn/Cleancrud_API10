using CleanCrud.Application.DTOs;

namespace CleanCrud.Application.Interfaces;

public interface IOrganizationService
{
    Task<IReadOnlyList<OfficeBranchDto>> GetBranchesAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<OfficeBranchDto> CreateBranchAsync(OfficeBranchRequestDto dto, CancellationToken cancellationToken);
    Task<OfficeBranchDto?> UpdateBranchAsync(int id, OfficeBranchRequestDto dto, CancellationToken cancellationToken);
    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(int? branchId, bool includeInactive, CancellationToken cancellationToken);
    Task<DepartmentDto> CreateDepartmentAsync(DepartmentRequestDto dto, CancellationToken cancellationToken);
    Task<DepartmentDto?> UpdateDepartmentAsync(int id, DepartmentRequestDto dto, CancellationToken cancellationToken);
}
