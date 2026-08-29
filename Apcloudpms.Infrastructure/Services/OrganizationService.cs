using System.Data;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Apcloudpms.Infrastructure.Services;

public sealed class OrganizationService(AppDbContext context) : IOrganizationService
{
    public Task<OrganizationPagedResponseDto<OfficeBranchDto>> GetBranchesAsync(
        OrganizationQueryDto query, CancellationToken cancellationToken) =>
        ReadPagedAsync(
            "dbo.SPOfficeBranchGet", query,
            command => { }, ReadBranch, cancellationToken);

    public Task<IReadOnlyList<DropdownItemDto>> GetBranchDropdownAsync(
        CancellationToken cancellationToken) =>
        ReadDropdownAsync("dbo.SPOfficeBranchDdl", null, cancellationToken);

    public Task<OfficeBranchDto?> GetBranchByIdAsync(
        int id, CancellationToken cancellationToken) =>
        ReadSingleAsync("dbo.SPOfficeBranchGetById", id, ReadBranch, cancellationToken);

    public Task<OfficeBranchDto> CreateBranchAsync(
        OfficeBranchRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return ExecuteBranchWriteAsync("dbo.SPOfficeBranchIns", null, dto, cancellationToken);
    }

    public async Task<OfficeBranchDto?> UpdateBranchAsync(
        int id, OfficeBranchRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        try { return await ExecuteBranchWriteAsync("dbo.SPOfficeBranchUpd", id, dto, cancellationToken); }
        catch (KeyNotFoundException) { return null; }
    }

    public Task<bool> DeleteBranchAsync(int id, CancellationToken cancellationToken) =>
        ExecuteDeleteAsync("dbo.SPOfficeBranchDel", id, cancellationToken);

    public Task<OrganizationPagedResponseDto<DepartmentDto>> GetDepartmentsAsync(
        DepartmentQueryDto query, CancellationToken cancellationToken) =>
        ReadPagedAsync(
            "dbo.SPDepartmentGet", query,
            command => Add(command, "@OfficeBranchId", SqlDbType.Int, query.OfficeBranchId),
            ReadDepartment, cancellationToken);

    public Task<IReadOnlyList<DropdownItemDto>> GetDepartmentDropdownAsync(
        int? officeBranchId, CancellationToken cancellationToken) =>
        ReadDropdownAsync("dbo.SPDepartmentDdl", officeBranchId, cancellationToken);

    public Task<DepartmentDto?> GetDepartmentByIdAsync(
        int id, CancellationToken cancellationToken) =>
        ReadSingleAsync("dbo.SPDepartmentGetById", id, ReadDepartment, cancellationToken);

    public Task<DepartmentDto> CreateDepartmentAsync(
        DepartmentRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return ExecuteDepartmentWriteAsync("dbo.SPDepartmentIns", null, dto, cancellationToken);
    }

    public async Task<DepartmentDto?> UpdateDepartmentAsync(
        int id, DepartmentRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        try { return await ExecuteDepartmentWriteAsync("dbo.SPDepartmentUpd", id, dto, cancellationToken); }
        catch (KeyNotFoundException) { return null; }
    }

    public Task<bool> DeleteDepartmentAsync(int id, CancellationToken cancellationToken) =>
        ExecuteDeleteAsync("dbo.SPDepartmentDel", id, cancellationToken);

    private async Task<OrganizationPagedResponseDto<T>> ReadPagedAsync<T>(
        string procedure, OrganizationQueryDto query, Action<SqlCommand> addFilters,
        Func<SqlDataReader, T> map, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, procedure);
            Add(command, "@PageNumber", SqlDbType.Int, query.PageNumber);
            Add(command, "@PageSize", SqlDbType.Int, query.PageSize);
            Add(command, "@SearchTerm", SqlDbType.NVarChar, Normalize(query.Search), 200);
            Add(command, "@IncludeInactive", SqlDbType.Bit, query.IncludeInactive);
            addFilters(command);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException($"{procedure} did not return the total record count.");
            var totalRecords = reader.GetInt64(reader.GetOrdinal("TotalRecords"));
            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException($"{procedure} did not return the paged records.");

            var data = new List<T>();
            while (await reader.ReadAsync(cancellationToken)) data.Add(map(reader));
            var totalPages = totalRecords == 0 ? 0 : (totalRecords + query.PageSize - 1L) / query.PageSize;
            return new OrganizationPagedResponseDto<T>(data, totalRecords, totalPages,
                query.PageNumber, query.PageSize, totalRecords > 0 && query.PageNumber > 1,
                query.PageNumber < totalPages);
        }, cancellationToken);
    }

    private async Task<IReadOnlyList<DropdownItemDto>> ReadDropdownAsync(
        string procedure, int? officeBranchId, CancellationToken cancellationToken) =>
        await WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, procedure);
            if (procedure == "dbo.SPDepartmentDdl")
                Add(command, "@OfficeBranchId", SqlDbType.Int, officeBranchId);
            var data = new List<DropdownItemDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                data.Add(new DropdownItemDto(reader.GetInt32(reader.GetOrdinal("Id")),
                    reader.GetString(reader.GetOrdinal("Code")),
                    reader.GetString(reader.GetOrdinal("Name"))));
            return data;
        }, cancellationToken);

    private async Task<T?> ReadSingleAsync<T>(string procedure, int id,
        Func<SqlDataReader, T> map, CancellationToken cancellationToken) =>
        await WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, procedure);
            Add(command, "@Id", SqlDbType.Int, id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? map(reader) : default;
        }, cancellationToken);

    private async Task<OfficeBranchDto> ExecuteBranchWriteAsync(string procedure, int? id,
        OfficeBranchRequestDto dto, CancellationToken cancellationToken) =>
        await ExecuteWriteAsync(procedure, command =>
        {
            if (id.HasValue) Add(command, "@Id", SqlDbType.Int, id.Value);
            Add(command, "@Code", SqlDbType.NVarChar, dto.Code.Trim().ToUpperInvariant(), 20);
            Add(command, "@Name", SqlDbType.NVarChar, dto.Name.Trim(), 150);
            Add(command, "@Address", SqlDbType.NVarChar, Normalize(dto.Address), 500);
            Add(command, "@IsHeadOffice", SqlDbType.Bit, dto.IsHeadOffice);
            Add(command, "@IsActive", SqlDbType.Bit, dto.IsActive);
        }, ReadBranch, cancellationToken);

    private async Task<DepartmentDto> ExecuteDepartmentWriteAsync(string procedure, int? id,
        DepartmentRequestDto dto, CancellationToken cancellationToken) =>
        await ExecuteWriteAsync(procedure, command =>
        {
            if (id.HasValue) Add(command, "@Id", SqlDbType.Int, id.Value);
            Add(command, "@OfficeBranchId", SqlDbType.Int, dto.OfficeBranchId);
            Add(command, "@Code", SqlDbType.NVarChar, dto.Code.Trim().ToUpperInvariant(), 20);
            Add(command, "@Name", SqlDbType.NVarChar, dto.Name.Trim(), 150);
            Add(command, "@IsActive", SqlDbType.Bit, dto.IsActive);
        }, ReadDepartment, cancellationToken);

    private async Task<T> ExecuteWriteAsync<T>(string procedure, Action<SqlCommand> addParameters,
        Func<SqlDataReader, T> map, CancellationToken cancellationToken) =>
        await WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, procedure);
            addParameters(command);
            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    throw new KeyNotFoundException($"The requested record for {procedure} was not found.");
                return map(reader);
            }
            catch (SqlException ex) when (ex.Number >= 50000 || ex.Number is 2601 or 2627 or 547)
            {
                throw new ArgumentException(ex.Message, ex);
            }
        }, cancellationToken);

    private async Task<bool> ExecuteDeleteAsync(
        string procedure, int id, CancellationToken cancellationToken) =>
        await WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, procedure);
            Add(command, "@Id", SqlDbType.Int, id);
            try
            {
                var result = await command.ExecuteScalarAsync(cancellationToken);
                return result is not null && result != DBNull.Value && Convert.ToBoolean(result);
            }
            catch (SqlException ex) when (ex.Number >= 50000 || ex.Number is 2601 or 2627 or 547)
            {
                throw new ArgumentException(ex.Message, ex);
            }
        }, cancellationToken);

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
        var parameter = size.HasValue ? new SqlParameter(name, type, size.Value) : new SqlParameter(name, type);
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static OfficeBranchDto ReadBranch(SqlDataReader reader) => new(
        reader.GetInt32(reader.GetOrdinal("Id")), reader.GetString(reader.GetOrdinal("Code")),
        reader.GetString(reader.GetOrdinal("Name")), GetNullableString(reader, "Address"),
        reader.GetBoolean(reader.GetOrdinal("IsHeadOffice")), reader.GetBoolean(reader.GetOrdinal("IsActive")));

    private static DepartmentDto ReadDepartment(SqlDataReader reader) => new(
        reader.GetInt32(reader.GetOrdinal("Id")), reader.GetInt32(reader.GetOrdinal("OfficeBranchId")),
        reader.GetString(reader.GetOrdinal("BranchName")), reader.GetString(reader.GetOrdinal("Code")),
        reader.GetString(reader.GetOrdinal("Name")), reader.GetBoolean(reader.GetOrdinal("IsActive")));

    private static string? GetNullableString(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
