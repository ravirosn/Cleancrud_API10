using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IOrganizationService
{
    Task<OrganizationDetailsDto?> GetCurrentOrganizationAsync(CancellationToken cancellationToken);
    Task<OrganizationDetailsDto?> GetOrganizationByIdAsync(int id, CancellationToken cancellationToken);
    Task<OrganizationDetailsDto?> UpdateOrganizationAsync(int id, OrganizationUpdateRequestDto dto, CancellationToken cancellationToken);
    Task<OrganizationPagedResponseDto<OfficeBranchDto>> GetBranchesAsync(OrganizationQueryDto query, CancellationToken cancellationToken);
    Task<IReadOnlyList<DropdownItemDto>> GetBranchDropdownAsync(CancellationToken cancellationToken);
    Task<OfficeBranchDto?> GetBranchByIdAsync(int id, CancellationToken cancellationToken);
    Task<OfficeBranchDto> CreateBranchAsync(OfficeBranchRequestDto dto, CancellationToken cancellationToken);
    Task<OfficeBranchDto?> UpdateBranchAsync(int id, OfficeBranchRequestDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteBranchAsync(int id, CancellationToken cancellationToken);
    Task<OrganizationPagedResponseDto<DepartmentDto>> GetDepartmentsAsync(DepartmentQueryDto query, CancellationToken cancellationToken);
    Task<IReadOnlyList<DropdownItemDto>> GetDepartmentDropdownAsync(int? officeBranchId, CancellationToken cancellationToken);
    Task<DepartmentDto?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken);
    Task<DepartmentDto> CreateDepartmentAsync(DepartmentRequestDto dto, CancellationToken cancellationToken);
    Task<DepartmentDto?> UpdateDepartmentAsync(int id, DepartmentRequestDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteDepartmentAsync(int id, CancellationToken cancellationToken);
    Task<OrganizationPagedResponseDto<FiscalYearDto>> GetFiscalYearsAsync(FiscalYearQueryDto query, CancellationToken cancellationToken);
    Task<ActiveFiscalYearDto?> GetActiveFiscalYearAsync(CancellationToken cancellationToken);
    Task<FiscalYearDto?> GetFiscalYearByIdAsync(int id, CancellationToken cancellationToken);
    Task<FiscalYearDto> CreateFiscalYearAsync(FiscalYearRequestDto dto, CancellationToken cancellationToken);
    Task<FiscalYearDto?> UpdateFiscalYearAsync(int id, FiscalYearRequestDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteFiscalYearAsync(int id, CancellationToken cancellationToken);
}
