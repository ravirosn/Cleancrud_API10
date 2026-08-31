using System.Data;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Apcloudpms.Infrastructure.Services;

public sealed class RoleModuleMenuManagementService(
    AppDbContext context,
    IAuditContext auditContext) : IRoleModuleMenuManagementService
{
    public Task<RoleModuleMenuPagedResponseDto> GetAsync(
        RoleModuleMenuQueryDto query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, "dbo.SpRoleModuleMenusGet");
            Add(command, "@PageNumber", SqlDbType.Int, query.PageNumber);
            Add(command, "@PageSize", SqlDbType.Int, query.PageSize);
            Add(command, "@SearchTerm", SqlDbType.NVarChar, Normalize(query.SearchTerm), 200);
            Add(command, "@SortBy", SqlDbType.NVarChar, query.SortBy, 30);
            Add(command, "@SortDirection", SqlDbType.VarChar, query.SortDirection, 4);
            Add(command, "@RoleId", SqlDbType.Int, query.RoleId);
            Add(command, "@ApplicationModuleId", SqlDbType.Int, query.ApplicationModuleId);
            Add(command, "@IncludeInactive", SqlDbType.Bit, query.IncludeInactive);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("dbo.SpRoleModuleMenusGet did not return a record count.");
            var totalRecords = reader.GetInt64(reader.GetOrdinal("TotalRecords"));
            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException("dbo.SpRoleModuleMenusGet did not return records.");

            var records = new List<RoleModuleMenuGridDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var roleId = reader.GetInt32(reader.GetOrdinal("RoleId"));
                var moduleId = reader.GetInt32(reader.GetOrdinal("ApplicationModuleId"));
                var menuId = reader.GetInt32(reader.GetOrdinal("ModuleMenuId"));
                var isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                records.Add(new RoleModuleMenuGridDto(
                    $"{roleId}:{moduleId}:{menuId}", roleId,
                    reader.GetString(reader.GetOrdinal("RoleName")), moduleId,
                    reader.GetString(reader.GetOrdinal("ModuleName")), menuId,
                    NullableInt(reader, "ParentMenuId"),
                    reader.GetString(reader.GetOrdinal("MenuName")),
                    reader.GetString(reader.GetOrdinal("MenuHierarchy")),
                    reader.GetInt32(reader.GetOrdinal("DisplayOrder")), isActive,
                    isActive ? "Active" : "Inactive",
                    reader.GetDateTime(reader.GetOrdinal("AssignedAtUtc")),
                    NullableString(reader, "AssignedBy"),
                    NullableDateTime(reader, "ModifiedAtUtc"),
                    NullableString(reader, "ModifiedBy")));
            }

            var totalPages = totalRecords == 0 ? 0 : (totalRecords + query.PageSize - 1L) / query.PageSize;
            return new RoleModuleMenuPagedResponseDto(
                records, totalRecords, totalPages, query.PageNumber, query.PageSize,
                totalRecords > 0 && query.PageNumber > 1, query.PageNumber < totalPages);
        }, cancellationToken);
    }

    public Task<IReadOnlyList<RoleModuleMenuRoleOptionDto>> GetRoleOptionsAsync(
        CancellationToken cancellationToken) =>
        ReadOptionsAsync("dbo.SpRoleModuleMenuRolesDdl", null,
            reader => new RoleModuleMenuRoleOptionDto(
                reader.GetInt32(reader.GetOrdinal("Id")),
                reader.GetString(reader.GetOrdinal("Name"))), cancellationToken);

    public Task<IReadOnlyList<RoleModuleMenuModuleOptionDto>> GetModuleOptionsAsync(
        int roleId, CancellationToken cancellationToken)
    {
        if (roleId <= 0) throw new ArgumentException("A role is required.");
        return ReadOptionsAsync("dbo.SpRoleModuleMenuModulesDdl",
            command => Add(command, "@RoleId", SqlDbType.Int, roleId),
            reader => new RoleModuleMenuModuleOptionDto(
                reader.GetInt32(reader.GetOrdinal("Id")),
                reader.GetString(reader.GetOrdinal("Code")),
                reader.GetString(reader.GetOrdinal("Name"))), cancellationToken);
    }

    public Task<IReadOnlyList<RoleModuleMenuMenuOptionDto>> GetMenuOptionsAsync(
        int roleId, int moduleId, CancellationToken cancellationToken)
    {
        if (roleId <= 0 || moduleId <= 0)
            throw new ArgumentException("A role and module are required.");
        return ReadOptionsAsync("dbo.SpRoleModuleMenuMenusDdl", command =>
        {
            Add(command, "@RoleId", SqlDbType.Int, roleId);
            Add(command, "@ApplicationModuleId", SqlDbType.Int, moduleId);
        }, reader => new RoleModuleMenuMenuOptionDto(
            reader.GetInt32(reader.GetOrdinal("Id")),
            NullableInt(reader, "ParentMenuId"),
            reader.GetString(reader.GetOrdinal("Name")),
            reader.GetString(reader.GetOrdinal("Hierarchy")),
            reader.GetInt32(reader.GetOrdinal("Depth")),
            reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
            reader.GetBoolean(reader.GetOrdinal("IsAssigned")),
            reader.GetBoolean(reader.GetOrdinal("CanAssign"))), cancellationToken);
    }

    public Task<RoleModuleMenuManagementDto> CreateAsync(
        RoleModuleMenuManagementRequestDto request, CancellationToken cancellationToken)
    {
        Validate(request);
        return ExecuteWriteAsync("dbo.SpRoleModuleMenusAdd", request, cancellationToken);
    }

    public async Task<RoleModuleMenuManagementDto?> UpdateAsync(
        int roleId, int moduleId, int menuId,
        RoleModuleMenuManagementRequestDto request, CancellationToken cancellationToken)
    {
        Validate(request);
        try
        {
            return await ExecuteWriteAsync(
                "dbo.SpRoleModuleMenusEdit", request, cancellationToken,
                roleId, moduleId, menuId);
        }
        catch (KeyNotFoundException) { return null; }
    }

    public Task<bool> DeleteAsync(
        int roleId, int moduleId, int menuId, CancellationToken cancellationToken)
    {
        if (roleId <= 0 || moduleId <= 0 || menuId <= 0)
            throw new ArgumentException("A valid assignment key is required.");
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, "dbo.SpRoleModuleMenusDelete");
            Add(command, "@RoleId", SqlDbType.Int, roleId);
            Add(command, "@ApplicationModuleId", SqlDbType.Int, moduleId);
            Add(command, "@ModuleMenuId", SqlDbType.Int, menuId);
            AddAudit(command);
            try { return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1; }
            catch (SqlException exception) { throw Translate(exception); }
        }, cancellationToken);
    }

    private Task<IReadOnlyList<T>> ReadOptionsAsync<T>(
        string procedure, Action<SqlCommand>? parameters,
        Func<SqlDataReader, T> map, CancellationToken cancellationToken) =>
        WithConnectionAsync<IReadOnlyList<T>>(async connection =>
        {
            await using var command = CreateCommand(connection, procedure);
            parameters?.Invoke(command);
            var records = new List<T>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) records.Add(map(reader));
            return records;
        }, cancellationToken);

    private Task<RoleModuleMenuManagementDto> ExecuteWriteAsync(
        string procedure, RoleModuleMenuManagementRequestDto request,
        CancellationToken cancellationToken,
        int? originalRoleId = null, int? originalModuleId = null,
        int? originalMenuId = null) =>
        WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, procedure);
            if (originalRoleId.HasValue)
            {
                Add(command, "@OriginalRoleId", SqlDbType.Int, originalRoleId.Value);
                Add(command, "@OriginalApplicationModuleId", SqlDbType.Int, originalModuleId!.Value);
                Add(command, "@OriginalModuleMenuId", SqlDbType.Int, originalMenuId!.Value);
            }
            Add(command, "@RoleId", SqlDbType.Int, request.RoleId);
            Add(command, "@ApplicationModuleId", SqlDbType.Int, request.ApplicationModuleId);
            Add(command, "@ModuleMenuId", SqlDbType.Int, request.ModuleMenuId);
            Add(command, "@IsActive", SqlDbType.Bit, request.IsActive);
            AddAudit(command);
            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    throw new InvalidOperationException($"{procedure} did not return the saved assignment.");
                return MapSaved(reader);
            }
            catch (SqlException exception) { throw Translate(exception); }
        }, cancellationToken);

    private static RoleModuleMenuManagementDto MapSaved(SqlDataReader reader) => new(
        reader.GetInt32(reader.GetOrdinal("RoleId")),
        reader.GetString(reader.GetOrdinal("RoleName")),
        reader.GetInt32(reader.GetOrdinal("ApplicationModuleId")),
        reader.GetString(reader.GetOrdinal("ModuleName")),
        reader.GetInt32(reader.GetOrdinal("ModuleMenuId")),
        NullableInt(reader, "ParentMenuId"),
        reader.GetString(reader.GetOrdinal("MenuName")),
        reader.GetString(reader.GetOrdinal("MenuHierarchy")),
        reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
        reader.GetBoolean(reader.GetOrdinal("IsActive")),
        reader.GetDateTime(reader.GetOrdinal("AssignedAtUtc")),
        NullableString(reader, "AssignedBy"),
        NullableDateTime(reader, "ModifiedAtUtc"),
        NullableString(reader, "ModifiedBy"));

    private void AddAudit(SqlCommand command)
    {
        Add(command, "@ActorUserId", SqlDbType.Int, auditContext.UserId);
        Add(command, "@ActorName", SqlDbType.NVarChar, Normalize(auditContext.UserName), 256);
        Add(command, "@TraceId", SqlDbType.NVarChar, Normalize(auditContext.TraceId), 100);
        Add(command, "@IpAddress", SqlDbType.NVarChar, Normalize(auditContext.IpAddress), 45);
    }

    private static void Validate(RoleModuleMenuManagementRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RoleId <= 0 || request.ApplicationModuleId <= 0 || request.ModuleMenuId <= 0)
            throw new ArgumentException("A role, module, and menu are required.");
    }

    private async Task<T> WithConnectionAsync<T>(
        Func<SqlConnection, Task<T>> action, CancellationToken cancellationToken)
    {
        var connection = (SqlConnection)context.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;
        if (shouldClose) await context.Database.OpenConnectionAsync(cancellationToken);
        try { return await action(connection); }
        finally { if (shouldClose) await context.Database.CloseConnectionAsync(); }
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string procedure) =>
        new(procedure, connection) { CommandType = CommandType.StoredProcedure };

    private static void Add(SqlCommand command, string name, SqlDbType type, object? value, int? size = null)
    {
        var parameter = size.HasValue
            ? command.Parameters.Add(name, type, size.Value)
            : command.Parameters.Add(name, type);
        parameter.Value = value ?? DBNull.Value;
    }

    private static Exception Translate(SqlException exception) => exception.Number switch
    {
        50004 => new KeyNotFoundException(exception.Message, exception),
        >= 50000 and <= 50100 => new ArgumentException(exception.Message, exception),
        2601 or 2627 => new ArgumentException("This role already has the selected menu assignment.", exception),
        _ => exception
    };

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NullableString(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
    private static int? NullableInt(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }
    private static DateTime? NullableDateTime(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}
