using System.Data;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Domain.Entities;
using Apcloudpms.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Apcloudpms.Infrastructure.Services;

public sealed class AccessControlService : IAccessControlService
{
    private readonly AppDbContext _context;
    public AccessControlService(AppDbContext context) => _context = context;

    public async Task<RolePagedResponseDto> GetRolesAsync(
        RoleQueryDto query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var connection = (SqlConnection)_context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;
        if (shouldCloseConnection)
            await _context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.SpRolesGet";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@PageNumber", SqlDbType.Int)
                { Value = query.PageNumber });
            command.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int)
                { Value = query.PageSize });
            command.Parameters.Add(new SqlParameter("@SearchTerm", SqlDbType.NVarChar, 100)
                { Value = NormalizeOptional(query.SearchTerm) ?? (object)DBNull.Value });
            command.Parameters.Add(new SqlParameter("@IncludeInactive", SqlDbType.Bit)
                { Value = query.IncludeInactive });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException(
                    "dbo.SpRolesGet did not return the total record count.");

            var totalRecords = reader.GetInt64(reader.GetOrdinal("TotalRecords"));
            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException(
                    "dbo.SpRolesGet did not return the paged roles.");

            var roles = new List<RoleGridItemDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                roles.Add(new RoleGridItemDto(
                    reader.GetInt32(reader.GetOrdinal("Id")),
                    reader.GetString(reader.GetOrdinal("Name")),
                    isActive,
                    isActive ? "Active" : "Inactive",
                    reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc"))));
            }

            var totalPages = totalRecords == 0
                ? 0
                : (totalRecords + query.PageSize - 1L) / query.PageSize;
            return new RolePagedResponseDto(
                roles, totalRecords, totalPages, query.PageNumber, query.PageSize,
                totalRecords > 0 && query.PageNumber > 1,
                query.PageNumber < totalPages);
        }
        finally
        {
            if (shouldCloseConnection)
                await _context.Database.CloseConnectionAsync();
        }
    }

    public async Task<RoleDto> CreateRoleAsync(RoleRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var name = dto.Name.Trim();
        var normalizedName = name.ToUpperInvariant();
        if (await _context.Roles.AnyAsync(x => x.NormalizedName == normalizedName, cancellationToken))
            throw new ArgumentException("A role with this name already exists.");

        var moduleIds = await ValidateModuleIdsAsync(dto.ModuleIds, cancellationToken);
        var now = DateTime.UtcNow;
        var role = new Role
        {
            Name = name,
            NormalizedName = normalizedName,
            IsActive = dto.IsActive,
            CreatedAtUtc = now
        };
        foreach (var moduleId in moduleIds)
            role.RoleModules.Add(new RoleModule
            {
                ApplicationModuleId = moduleId,
                IsActive = true,
                AssignedAtUtc = now
            });
        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
        return new RoleDto(role.Id, role.Name, role.IsActive, moduleIds.Order().ToArray());
    }

    public async Task<RoleDto?> UpdateRoleAsync(
        int id, RoleRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var role = await _context.Roles.Include(x => x.RoleModules)
            .ThenInclude(x => x.ApplicationModule)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null) return null;
        var name = dto.Name.Trim();
        var normalizedName = name.ToUpperInvariant();
        if (await _context.Roles.AnyAsync(
                x => x.Id != id && x.NormalizedName == normalizedName, cancellationToken))
            throw new ArgumentException("A role with this name already exists.");

        role.Name = name;
        role.NormalizedName = normalizedName;
        role.IsActive = dto.IsActive;
        HashSet<int>? synchronizedModuleIds = null;
        if (dto.ModuleIds is not null)
        {
            synchronizedModuleIds = await ValidateModuleIdsAsync(dto.ModuleIds, cancellationToken);
            SynchronizeRoleModules(role, synchronizedModuleIds);
        }
        await _context.SaveChangesAsync(cancellationToken);
        var activeModuleIds = synchronizedModuleIds is not null
            ? synchronizedModuleIds.Order().ToArray()
            : role.RoleModules.Where(x => x.IsActive && x.ApplicationModule.IsActive)
                .Select(x => x.ApplicationModuleId).Order().ToArray();
        return new RoleDto(role.Id, role.Name, role.IsActive, activeModuleIds);
    }

    public async Task<IReadOnlyList<RoleModuleOptionDto>> GetRoleModuleOptionsAsync(
        int? roleId, CancellationToken cancellationToken)
    {
        if (roleId is <= 0) throw new ArgumentException("Role id must be positive.");
        if (roleId.HasValue && !await _context.Roles.AnyAsync(x => x.Id == roleId.Value, cancellationToken))
            throw new KeyNotFoundException("The role was not found.");

        return await _context.ApplicationModules.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new RoleModuleOptionDto(
                x.Id, x.Code, x.Name, x.DisplayOrder,
                roleId.HasValue && x.RoleModules.Any(rm =>
                    rm.RoleId == roleId.Value && rm.IsActive)))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteRoleAsync(int id, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.SingleOrDefaultAsync(
            x => x.Id == id, cancellationToken);
        if (role is null) return false;
        if (role.NormalizedName == "ADMIN")
            throw new ArgumentException("The Admin role cannot be deleted.");

        role.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
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

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<HashSet<int>> ValidateModuleIdsAsync(
        IReadOnlyList<int>? requestedModuleIds, CancellationToken cancellationToken)
    {
        var moduleIds = (requestedModuleIds ?? []).ToHashSet();
        if (moduleIds.Any(id => id <= 0))
            throw new ArgumentException("Module ids must be positive.");
        if (moduleIds.Count != (requestedModuleIds?.Count ?? 0))
            throw new ArgumentException("A module cannot be assigned more than once.");
        if (moduleIds.Count == 0) return moduleIds;

        var activeIds = await _context.ApplicationModules.AsNoTracking()
            .Where(x => moduleIds.Contains(x.Id) && x.IsActive)
            .Select(x => x.Id).ToListAsync(cancellationToken);
        if (activeIds.Count != moduleIds.Count)
            throw new ArgumentException("One or more selected modules do not exist or are inactive.");
        return moduleIds;
    }

    private static void SynchronizeRoleModules(Role role, HashSet<int> selectedModuleIds)
    {
        var now = DateTime.UtcNow;
        foreach (var assignment in role.RoleModules)
        {
            if (!assignment.ApplicationModule.IsActive) continue;
            var shouldBeActive = selectedModuleIds.Contains(assignment.ApplicationModuleId);
            if (shouldBeActive && !assignment.IsActive) assignment.AssignedAtUtc = now;
            assignment.IsActive = shouldBeActive;
        }

        var existingIds = role.RoleModules.Select(x => x.ApplicationModuleId).ToHashSet();
        foreach (var moduleId in selectedModuleIds.Except(existingIds))
            role.RoleModules.Add(new RoleModule
            {
                ApplicationModuleId = moduleId,
                IsActive = true,
                AssignedAtUtc = now
            });
    }
}
