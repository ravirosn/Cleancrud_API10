using System.Data;
using System.Text;
using System.Text.Json;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Apcloudpms.Infrastructure.Services;

public sealed class UserManagementService(
    AppDbContext context,
    IPasswordService passwordService,
    IAuditContext auditContext) : IUserManagementService
{
    public Task<UserManagementPagedResponseDto> GetUsersAsync(
        UserManagementQueryDto query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, "dbo.SpUsersGet");
            Add(command, "@PageNumber", SqlDbType.Int, query.PageNumber);
            Add(command, "@PageSize", SqlDbType.Int, query.PageSize);
            Add(command, "@SearchTerm", SqlDbType.NVarChar, Normalize(query.SearchTerm), 200);
            Add(command, "@SortBy", SqlDbType.NVarChar, query.SortBy, 30);
            Add(command, "@SortDirection", SqlDbType.VarChar, query.SortDirection, 4);
            Add(command, "@IncludeInactive", SqlDbType.Bit, query.IncludeInactive);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("dbo.SpUsersGet did not return a record count.");
            var totalRecords = reader.GetInt64(reader.GetOrdinal("TotalRecords"));
            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException("dbo.SpUsersGet did not return users.");

            var users = new List<UserManagementGridItemDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var active = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                users.Add(new UserManagementGridItemDto(
                    reader.GetInt32(reader.GetOrdinal("Id")),
                    reader.GetString(reader.GetOrdinal("UserName")),
                    NullableString(reader, "DisplayName"),
                    NullableString(reader, "Email"),
                    NullableString(reader, "ContactNumber"),
                    NullableInt(reader, "OfficeBranchId"),
                    NullableString(reader, "OfficeBranchName"),
                    NullableInt(reader, "DepartmentId"),
                    NullableString(reader, "DepartmentName"),
                    active,
                    active ? "Active" : "Inactive",
                    reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
                    NullableInt(reader, "CreatedByUserId"),
                    NullableString(reader, "CreatedBy"),
                    NullableDateTime(reader, "ModifiedAtUtc"),
                    NullableInt(reader, "ModifiedByUserId"),
                    NullableString(reader, "ModifiedBy")));
            }

            var totalPages = totalRecords == 0 ? 0 : (totalRecords + query.PageSize - 1) / query.PageSize;
            return new UserManagementPagedResponseDto(
                users, totalRecords, totalPages, query.PageNumber, query.PageSize,
                totalRecords > 0 && query.PageNumber > 1, query.PageNumber < totalPages);
        }, cancellationToken);
    }

    public async Task<UserManagementDto> CreateUserAsync(
        UserCreateRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateOfficeAndDepartment(request.OfficeBranchId, request.DepartmentId);
        ValidatePassword(request.Password, required: true);
        var passwordHash = passwordService.HashPassword(request.Password);

        return await ExecuteUserWriteAsync(
            "dbo.SpUsersAdd", null, request.UserName, passwordHash,
            request.DisplayName, request.Email, request.ContactNumber,
            request.OfficeBranchId, request.DepartmentId, request.IsActive,
            cancellationToken);
    }

    public async Task<UserManagementDto?> UpdateUserAsync(
        int id, UserUpdateRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateOfficeAndDepartment(request.OfficeBranchId, request.DepartmentId);
        ValidatePassword(request.Password, required: false);
        var passwordHash = string.IsNullOrWhiteSpace(request.Password)
            ? null
            : passwordService.HashPassword(request.Password);
        try
        {
            return await ExecuteUserWriteAsync(
                "dbo.SpUsersEdit", id, request.UserName, passwordHash,
                request.DisplayName, request.Email, request.ContactNumber,
                request.OfficeBranchId, request.DepartmentId, request.IsActive,
                cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    public Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken) =>
        WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, "dbo.SpUsersDelete");
            Add(command, "@Id", SqlDbType.Int, id);
            AddAuditParameters(command);
            try
            {
                return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
            }
            catch (SqlException exception)
            {
                throw Translate(exception);
            }
        }, cancellationToken);

    public Task<UserRoleConfigurationDto?> GetUserRolesAsync(
        int id, CancellationToken cancellationToken) =>
        WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, "dbo.SpUserRolesGet");
            Add(command, "@UserId", SqlDbType.Int, id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var userName = reader.GetString(reader.GetOrdinal("UserName"));
            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException("dbo.SpUserRolesGet did not return roles.");
            var roles = new List<UserRoleOptionDto>();
            while (await reader.ReadAsync(cancellationToken))
                roles.Add(new UserRoleOptionDto(
                    reader.GetInt32(reader.GetOrdinal("RoleId")),
                    reader.GetString(reader.GetOrdinal("RoleName")),
                    reader.GetBoolean(reader.GetOrdinal("IsAssigned"))));
            return new UserRoleConfigurationDto(id, userName, roles);
        }, cancellationToken);

    public async Task<UserRoleConfigurationDto?> SetUserRolesAsync(
        int id, UserRolesUpdateRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RoleIds.Any(roleId => roleId <= 0))
            throw new ArgumentException("Every role ID must be greater than zero.");
        var roleIds = request.RoleIds.Distinct().ToArray();
        if (roleIds.Length != request.RoleIds.Count)
            throw new ArgumentException("Duplicate role IDs are not allowed.");

        try
        {
            await WithConnectionAsync(async connection =>
            {
                await using var command = CreateCommand(connection, "dbo.SpUserRolesSet");
                Add(command, "@UserId", SqlDbType.Int, id);
                Add(command, "@RoleIdsJson", SqlDbType.NVarChar, JsonSerializer.Serialize(roleIds), -1);
                AddAuditParameters(command);
                try
                {
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException exception)
                {
                    throw Translate(exception);
                }
                return true;
            }, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        return await GetUserRolesAsync(id, cancellationToken);
    }

    private Task<UserManagementDto> ExecuteUserWriteAsync(
        string procedure, int? id, string userName, string? passwordHash,
        string? displayName, string? email, string? contactNumber,
        int? officeBranchId, int? departmentId, bool isActive,
        CancellationToken cancellationToken) =>
        WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, procedure);
            if (id.HasValue) Add(command, "@Id", SqlDbType.Int, id.Value);
            Add(command, "@UserName", SqlDbType.NVarChar, userName.Trim(), 100);
            Add(command, "@PasswordHash", SqlDbType.NVarChar, passwordHash, 255);
            Add(command, "@DisplayName", SqlDbType.NVarChar, Normalize(displayName), 200);
            Add(command, "@Email", SqlDbType.NVarChar, Normalize(email), 320);
            Add(command, "@ContactNumber", SqlDbType.NVarChar, Normalize(contactNumber), 20);
            Add(command, "@OfficeBranchId", SqlDbType.Int, officeBranchId);
            Add(command, "@DepartmentId", SqlDbType.Int, departmentId);
            Add(command, "@IsActive", SqlDbType.Bit, isActive);
            AddAuditParameters(command);
            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    throw new InvalidOperationException($"{procedure} did not return the saved user.");
                return ReadUser(reader);
            }
            catch (SqlException exception)
            {
                throw Translate(exception);
            }
        }, cancellationToken);

    private void AddAuditParameters(SqlCommand command)
    {
        Add(command, "@ActorUserId", SqlDbType.Int, auditContext.UserId);
        Add(command, "@ActorName", SqlDbType.NVarChar, Normalize(auditContext.UserName), 256);
        Add(command, "@TraceId", SqlDbType.NVarChar, Normalize(auditContext.TraceId), 100);
        Add(command, "@IpAddress", SqlDbType.NVarChar, Normalize(auditContext.IpAddress), 45);
    }

    private static void ValidateOfficeAndDepartment(int? officeBranchId, int? departmentId)
    {
        if (officeBranchId.HasValue != departmentId.HasValue)
            throw new ArgumentException("Select both an office and a department, or leave both empty.");
    }

    private static void ValidatePassword(string? password, bool required)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            if (required) throw new ArgumentException("Password is required.");
            return;
        }
        if (password.Length < 12)
            throw new ArgumentException("Password must contain at least 12 characters.");
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(character => !char.IsLetterOrDigit(character)))
            throw new ArgumentException(
                "Password must include uppercase, lowercase, number, and special characters.");
        if (Encoding.UTF8.GetByteCount(password) > 72)
            throw new ArgumentException("Password must not exceed 72 UTF-8 bytes.");
    }

    private static UserManagementDto ReadUser(SqlDataReader reader) => new(
        reader.GetInt32(reader.GetOrdinal("Id")),
        reader.GetString(reader.GetOrdinal("UserName")),
        NullableString(reader, "DisplayName"), NullableString(reader, "Email"),
        NullableString(reader, "ContactNumber"), NullableInt(reader, "OfficeBranchId"),
        NullableString(reader, "OfficeBranchName"), NullableInt(reader, "DepartmentId"),
        NullableString(reader, "DepartmentName"), reader.GetBoolean(reader.GetOrdinal("IsActive")),
        reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")), NullableInt(reader, "CreatedByUserId"),
        NullableDateTime(reader, "ModifiedAtUtc"), NullableInt(reader, "ModifiedByUserId"));

    private static Exception Translate(SqlException exception) => exception.Number switch
    {
        50004 => new KeyNotFoundException(exception.Message, exception),
        >= 50000 and <= 50100 => new ArgumentException(exception.Message, exception),
        2601 or 2627 => new ArgumentException("Username is already in use.", exception),
        _ => exception
    };

    private async Task<T> WithConnectionAsync<T>(
        Func<SqlConnection, Task<T>> action, CancellationToken cancellationToken)
    {
        var connection = (SqlConnection)context.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;
        if (shouldClose) await context.Database.OpenConnectionAsync(cancellationToken);
        try { return await action(connection); }
        finally { if (shouldClose) await context.Database.CloseConnectionAsync(); }
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string procedure) => new(procedure, connection)
    {
        CommandType = CommandType.StoredProcedure
    };

    private static void Add(SqlCommand command, string name, SqlDbType type, object? value, int? size = null)
    {
        var parameter = size.HasValue
            ? command.Parameters.Add(name, type, size.Value)
            : command.Parameters.Add(name, type);
        parameter.Value = value ?? DBNull.Value;
    }

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
