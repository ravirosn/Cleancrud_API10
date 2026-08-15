using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using CleanCrud.Domain.Entities;
using CleanCrud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CleanCrud.Infrastructure.Services;

public sealed class AccessControlService : IAccessControlService
{
    private readonly AppDbContext _context;
    public AccessControlService(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(
        bool includeInactive, CancellationToken cancellationToken) =>
        await _context.Roles.AsNoTracking()
            .Where(x => includeInactive || x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new RoleDto(x.Id, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<RoleDto> CreateRoleAsync(RoleRequestDto dto, CancellationToken cancellationToken)
    {
        var name = dto.Name.Trim();
        var normalizedName = name.ToUpperInvariant();
        if (await _context.Roles.AnyAsync(x => x.NormalizedName == normalizedName, cancellationToken))
            throw new ArgumentException("A role with this name already exists.");

        var role = new Role
        {
            Name = name,
            NormalizedName = normalizedName,
            IsActive = dto.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
        return new RoleDto(role.Id, role.Name, role.IsActive);
    }

    public async Task<RoleDto?> UpdateRoleAsync(
        int id, RoleRequestDto dto, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null) return null;
        var name = dto.Name.Trim();
        var normalizedName = name.ToUpperInvariant();
        if (await _context.Roles.AnyAsync(
                x => x.Id != id && x.NormalizedName == normalizedName, cancellationToken))
            throw new ArgumentException("A role with this name already exists.");

        role.Name = name;
        role.NormalizedName = normalizedName;
        role.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return new RoleDto(role.Id, role.Name, role.IsActive);
    }

    public async Task<bool> SetUserRoleAsync(
        UserRoleAssignmentDto dto, CancellationToken cancellationToken)
    {
        if (!await _context.Users.AnyAsync(x => x.Id == dto.UserId, cancellationToken)) return false;
        var role = await _context.Roles.SingleOrDefaultAsync(x => x.Id == dto.RoleId, cancellationToken);
        if (role is null) return false;
        if (dto.IsActive && !role.IsActive)
            throw new ArgumentException("An inactive role cannot be assigned as active.");

        var assignment = await _context.UserRoles.SingleOrDefaultAsync(
            x => x.UserId == dto.UserId && x.RoleId == dto.RoleId, cancellationToken);
        if (assignment is null)
        {
            assignment = new UserRole
            {
                UserId = dto.UserId,
                RoleId = dto.RoleId,
                IsActive = dto.IsActive,
                AssignedAtUtc = DateTime.UtcNow
            };
            _context.UserRoles.Add(assignment);
        }
        else
        {
            assignment.IsActive = dto.IsActive;
            if (dto.IsActive) assignment.AssignedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
