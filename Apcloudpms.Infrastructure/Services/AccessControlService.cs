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
}
