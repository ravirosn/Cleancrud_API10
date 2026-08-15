using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using CleanCrud.Domain.Entities;
using CleanCrud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CleanCrud.Infrastructure.Services;

public sealed class OrganizationService : IOrganizationService
{
    private readonly AppDbContext _context;

    public OrganizationService(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<OfficeBranchDto>> GetBranchesAsync(
        bool includeInactive, CancellationToken cancellationToken) =>
        await _context.OfficeBranches.AsNoTracking()
            .Where(x => includeInactive || x.IsActive)
            .OrderByDescending(x => x.IsHeadOffice).ThenBy(x => x.Name)
            .Select(x => new OfficeBranchDto(x.Id, x.Code, x.Name, x.Address,
                x.IsHeadOffice, x.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<OfficeBranchDto> CreateBranchAsync(
        OfficeBranchRequestDto dto, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(dto.Code);
        if (dto.IsHeadOffice && !dto.IsActive)
            throw new ArgumentException("The head office must be active.");
        if (await _context.OfficeBranches.AnyAsync(x => x.Code == code, cancellationToken))
            throw new ArgumentException("An office branch with this code already exists.");

        var hasActiveHeadOffice = await _context.OfficeBranches
            .AnyAsync(x => x.IsHeadOffice && x.IsActive, cancellationToken);
        if (dto.IsActive && !dto.IsHeadOffice && !hasActiveHeadOffice)
            throw new ArgumentException("The first active office branch must be the head office.");

        var branch = new OfficeBranch
        {
            Code = code,
            Name = dto.Name.Trim(),
            Address = NormalizeOptional(dto.Address),
            IsHeadOffice = dto.IsHeadOffice,
            IsActive = dto.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.OfficeBranches.Add(branch);
        if (dto.IsHeadOffice)
            await SaveWithHeadOfficeTransferAsync(null, cancellationToken);
        else
            await _context.SaveChangesAsync(cancellationToken);
        return Map(branch);
    }

    public async Task<OfficeBranchDto?> UpdateBranchAsync(
        int id, OfficeBranchRequestDto dto, CancellationToken cancellationToken)
    {
        var branch = await _context.OfficeBranches.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (branch is null) return null;

        if (dto.IsHeadOffice && !dto.IsActive)
            throw new ArgumentException("The head office must be active.");
        if (!dto.IsActive && await _context.Departments.AnyAsync(
                x => x.OfficeBranchId == id && x.IsActive, cancellationToken))
            throw new ArgumentException("Disable the branch's active departments before disabling the branch.");

        var code = NormalizeCode(dto.Code);
        if (await _context.OfficeBranches.AnyAsync(x => x.Id != id && x.Code == code, cancellationToken))
            throw new ArgumentException("An office branch with this code already exists.");
        if (branch.IsHeadOffice && branch.IsActive && (!dto.IsHeadOffice || !dto.IsActive))
            throw new ArgumentException("Assign another active branch as head office before disabling this one.");

        branch.Code = code;
        branch.Name = dto.Name.Trim();
        branch.Address = NormalizeOptional(dto.Address);
        branch.IsHeadOffice = dto.IsHeadOffice;
        branch.IsActive = dto.IsActive;
        if (dto.IsHeadOffice)
            await SaveWithHeadOfficeTransferAsync(id, cancellationToken);
        else
            await _context.SaveChangesAsync(cancellationToken);
        return Map(branch);
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(
        int? branchId, bool includeInactive, CancellationToken cancellationToken) =>
        await _context.Departments.AsNoTracking()
            .Where(x => (!branchId.HasValue || x.OfficeBranchId == branchId) &&
                        (includeInactive || x.IsActive))
            .OrderBy(x => x.OfficeBranch.Name).ThenBy(x => x.Name)
            .Select(x => new DepartmentDto(x.Id, x.OfficeBranchId, x.OfficeBranch.Name,
                x.Code, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<DepartmentDto> CreateDepartmentAsync(
        DepartmentRequestDto dto, CancellationToken cancellationToken)
    {
        var branch = await _context.OfficeBranches.SingleOrDefaultAsync(
            x => x.Id == dto.OfficeBranchId, cancellationToken);
        if (branch is null) throw new ArgumentException("Office branch was not found.");
        if (!branch.IsActive && dto.IsActive)
            throw new ArgumentException("An active department cannot belong to an inactive branch.");

        var code = NormalizeCode(dto.Code);
        if (await _context.Departments.AnyAsync(
                x => x.OfficeBranchId == dto.OfficeBranchId && x.Code == code, cancellationToken))
            throw new ArgumentException("This department code already exists in the selected branch.");

        var department = new Department
        {
            OfficeBranchId = branch.Id,
            Code = code,
            Name = dto.Name.Trim(),
            IsActive = dto.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(department, branch.Name);
    }

    public async Task<DepartmentDto?> UpdateDepartmentAsync(
        int id, DepartmentRequestDto dto, CancellationToken cancellationToken)
    {
        var department = await _context.Departments.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (department is null) return null;
        var branch = await _context.OfficeBranches.SingleOrDefaultAsync(
            x => x.Id == dto.OfficeBranchId, cancellationToken);
        if (branch is null) throw new ArgumentException("Office branch was not found.");
        if (!branch.IsActive && dto.IsActive)
            throw new ArgumentException("An active department cannot belong to an inactive branch.");

        var code = NormalizeCode(dto.Code);
        if (await _context.Departments.AnyAsync(x => x.Id != id &&
                x.OfficeBranchId == dto.OfficeBranchId && x.Code == code, cancellationToken))
            throw new ArgumentException("This department code already exists in the selected branch.");

        department.OfficeBranchId = branch.Id;
        department.Code = code;
        department.Name = dto.Name.Trim();
        department.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return Map(department, branch.Name);
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private async Task SaveWithHeadOfficeTransferAsync(
        int? newHeadOfficeId, CancellationToken cancellationToken)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);
            await _context.OfficeBranches
                .Where(x => x.IsHeadOffice && (!newHeadOfficeId.HasValue || x.Id != newHeadOfficeId))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsHeadOffice, false), cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static OfficeBranchDto Map(OfficeBranch x) =>
        new(x.Id, x.Code, x.Name, x.Address, x.IsHeadOffice, x.IsActive);
    private static DepartmentDto Map(Department x, string branchName) =>
        new(x.Id, x.OfficeBranchId, branchName, x.Code, x.Name, x.IsActive);
}
