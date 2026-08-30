using System.Data;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Apcloudpms.Infrastructure.Services;

public sealed class ListItemService(
    AppDbContext context,
    IAuditContext auditContext) : IListItemService
{
    public Task<IReadOnlyList<ListItemDto>> GetByCategoryAsync(
        string categoryName, CancellationToken cancellationToken) =>
        WithConnectionAsync<IReadOnlyList<ListItemDto>>(async connection =>
        {
            await using var command = CreateCommand(connection, "dbo.SpGetListItemsByCategory");
            Add(command, "@CategoryName", SqlDbType.NVarChar, categoryName.Trim(), 100);
            var result = new List<ListItemDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                result.Add(new ListItemDto(
                    reader.GetInt32(reader.GetOrdinal("ListItemId")),
                    reader.GetInt32(reader.GetOrdinal("ListItemCategoryId")),
                    reader.GetString(reader.GetOrdinal("Code")),
                    reader.GetString(reader.GetOrdinal("Name")),
                    NullableString(reader, "Description"),
                    reader.GetInt32(reader.GetOrdinal("DisplayOrder"))));
            return result;
        }, cancellationToken);

    public Task<ListItemManagementPagedResponseDto<ListItemCategoryGridDto>> GetCategoriesAsync(
        ListItemManagementQueryDto query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return ReadPagedAsync(
            "dbo.SpListItemCategoriesGet", query, null,
            reader =>
            {
                var active = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                return new ListItemCategoryGridDto(
                    reader.GetInt32(reader.GetOrdinal("Id")),
                    reader.GetString(reader.GetOrdinal("Code")),
                    reader.GetString(reader.GetOrdinal("Name")),
                    NullableString(reader, "Description"), active,
                    active ? "Active" : "Inactive",
                    reader.GetInt32(reader.GetOrdinal("ItemCount")),
                    reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
                    NullableDateTime(reader, "UpdatedAtUtc"));
            }, cancellationToken);
    }

    public Task<IReadOnlyList<ListItemCategoryOptionDto>> GetCategoryOptionsAsync(
        CancellationToken cancellationToken) =>
        WithConnectionAsync<IReadOnlyList<ListItemCategoryOptionDto>>(async connection =>
        {
            await using var command = CreateCommand(connection, "dbo.SpListItemCategoriesDdl");
            var result = new List<ListItemCategoryOptionDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                result.Add(new ListItemCategoryOptionDto(
                    reader.GetInt32(reader.GetOrdinal("Id")),
                    reader.GetString(reader.GetOrdinal("Code")),
                    reader.GetString(reader.GetOrdinal("Name"))));
            return result;
        }, cancellationToken);

    public Task<ListItemCategoryManagementDto> CreateCategoryAsync(
        ListItemCategoryRequestDto request, CancellationToken cancellationToken)
    {
        ValidateCategory(request);
        return ExecuteCategoryWriteAsync("dbo.SpListItemCategoriesAdd", null, request, cancellationToken);
    }

    public async Task<ListItemCategoryManagementDto?> UpdateCategoryAsync(
        int id, ListItemCategoryRequestDto request, CancellationToken cancellationToken)
    {
        ValidateCategory(request);
        try
        {
            return await ExecuteCategoryWriteAsync(
                "dbo.SpListItemCategoriesEdit", id, request, cancellationToken);
        }
        catch (KeyNotFoundException) { return null; }
    }

    public Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken) =>
        ExecuteDeleteAsync("dbo.SpListItemCategoriesDelete", id, cancellationToken);

    public Task<ListItemManagementPagedResponseDto<ListItemGridDto>> GetItemsAsync(
        ListItemQueryDto query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return ReadPagedAsync(
            "dbo.SpListItemsGet", query,
            command => Add(command, "@ListItemCategoryId", SqlDbType.Int, query.ListItemCategoryId),
            reader =>
            {
                var active = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                return new ListItemGridDto(
                    reader.GetInt32(reader.GetOrdinal("Id")),
                    reader.GetInt32(reader.GetOrdinal("ListItemCategoryId")),
                    reader.GetString(reader.GetOrdinal("CategoryName")),
                    reader.GetString(reader.GetOrdinal("Code")),
                    reader.GetString(reader.GetOrdinal("Name")),
                    NullableString(reader, "Description"),
                    reader.GetInt32(reader.GetOrdinal("DisplayOrder")), active,
                    active ? "Active" : "Inactive",
                    reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
                    NullableDateTime(reader, "UpdatedAtUtc"));
            }, cancellationToken);
    }

    public Task<ListItemManagementDto> CreateItemAsync(
        ListItemRequestDto request, CancellationToken cancellationToken)
    {
        ValidateItem(request);
        return ExecuteItemWriteAsync("dbo.SpListItemsAdd", null, request, cancellationToken);
    }

    public async Task<ListItemManagementDto?> UpdateItemAsync(
        int id, ListItemRequestDto request, CancellationToken cancellationToken)
    {
        ValidateItem(request);
        try
        {
            return await ExecuteItemWriteAsync("dbo.SpListItemsEdit", id, request, cancellationToken);
        }
        catch (KeyNotFoundException) { return null; }
    }

    public Task<bool> DeleteItemAsync(int id, CancellationToken cancellationToken) =>
        ExecuteDeleteAsync("dbo.SpListItemsDelete", id, cancellationToken);

    private Task<ListItemManagementPagedResponseDto<T>> ReadPagedAsync<T>(
        string procedure, ListItemManagementQueryDto query,
        Action<SqlCommand>? addFilter, Func<SqlDataReader, T> map,
        CancellationToken cancellationToken) =>
        WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, procedure);
            Add(command, "@PageNumber", SqlDbType.Int, query.PageNumber);
            Add(command, "@PageSize", SqlDbType.Int, query.PageSize);
            Add(command, "@SearchTerm", SqlDbType.NVarChar, Normalize(query.SearchTerm), 200);
            Add(command, "@SortBy", SqlDbType.NVarChar, query.SortBy, 30);
            Add(command, "@SortDirection", SqlDbType.VarChar, query.SortDirection, 4);
            Add(command, "@IncludeInactive", SqlDbType.Bit, query.IncludeInactive);
            addFilter?.Invoke(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException($"{procedure} did not return a record count.");
            var totalRecords = reader.GetInt64(reader.GetOrdinal("TotalRecords"));
            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException($"{procedure} did not return records.");
            var records = new List<T>();
            while (await reader.ReadAsync(cancellationToken)) records.Add(map(reader));
            var totalPages = totalRecords == 0 ? 0 : (totalRecords + query.PageSize - 1) / query.PageSize;
            return new ListItemManagementPagedResponseDto<T>(
                records, totalRecords, totalPages, query.PageNumber, query.PageSize,
                totalRecords > 0 && query.PageNumber > 1, query.PageNumber < totalPages);
        }, cancellationToken);

    private Task<ListItemCategoryManagementDto> ExecuteCategoryWriteAsync(
        string procedure, int? id, ListItemCategoryRequestDto request,
        CancellationToken cancellationToken) =>
        WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, procedure);
            if (id.HasValue) Add(command, "@Id", SqlDbType.Int, id.Value);
            Add(command, "@Code", SqlDbType.NVarChar, request.Code.Trim().ToUpperInvariant(), 50);
            Add(command, "@Name", SqlDbType.NVarChar, request.Name.Trim(), 100);
            Add(command, "@Description", SqlDbType.NVarChar, Normalize(request.Description), 500);
            Add(command, "@IsActive", SqlDbType.Bit, request.IsActive);
            AddAudit(command);
            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    throw new InvalidOperationException($"{procedure} did not return the saved category.");
                return new ListItemCategoryManagementDto(
                    reader.GetInt32(reader.GetOrdinal("Id")),
                    reader.GetString(reader.GetOrdinal("Code")),
                    reader.GetString(reader.GetOrdinal("Name")),
                    NullableString(reader, "Description"),
                    reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
                    NullableDateTime(reader, "UpdatedAtUtc"));
            }
            catch (SqlException exception) { throw Translate(exception); }
        }, cancellationToken);

    private Task<ListItemManagementDto> ExecuteItemWriteAsync(
        string procedure, int? id, ListItemRequestDto request,
        CancellationToken cancellationToken) =>
        WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, procedure);
            if (id.HasValue) Add(command, "@Id", SqlDbType.Int, id.Value);
            Add(command, "@ListItemCategoryId", SqlDbType.Int, request.ListItemCategoryId);
            Add(command, "@Code", SqlDbType.NVarChar, request.Code.Trim().ToUpperInvariant(), 50);
            Add(command, "@Name", SqlDbType.NVarChar, request.Name.Trim(), 100);
            Add(command, "@Description", SqlDbType.NVarChar, Normalize(request.Description), 500);
            Add(command, "@DisplayOrder", SqlDbType.Int, request.DisplayOrder);
            Add(command, "@IsActive", SqlDbType.Bit, request.IsActive);
            AddAudit(command);
            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    throw new InvalidOperationException($"{procedure} did not return the saved item.");
                return new ListItemManagementDto(
                    reader.GetInt32(reader.GetOrdinal("Id")),
                    reader.GetInt32(reader.GetOrdinal("ListItemCategoryId")),
                    reader.GetString(reader.GetOrdinal("CategoryName")),
                    reader.GetString(reader.GetOrdinal("Code")),
                    reader.GetString(reader.GetOrdinal("Name")),
                    NullableString(reader, "Description"),
                    reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
                    reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
                    NullableDateTime(reader, "UpdatedAtUtc"));
            }
            catch (SqlException exception) { throw Translate(exception); }
        }, cancellationToken);

    private Task<bool> ExecuteDeleteAsync(
        string procedure, int id, CancellationToken cancellationToken) =>
        WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection, procedure);
            Add(command, "@Id", SqlDbType.Int, id);
            AddAudit(command);
            try { return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1; }
            catch (SqlException exception) { throw Translate(exception); }
        }, cancellationToken);

    private void AddAudit(SqlCommand command)
    {
        Add(command, "@ActorUserId", SqlDbType.Int, auditContext.UserId);
        Add(command, "@ActorName", SqlDbType.NVarChar, Normalize(auditContext.UserName), 256);
        Add(command, "@TraceId", SqlDbType.NVarChar, Normalize(auditContext.TraceId), 100);
        Add(command, "@IpAddress", SqlDbType.NVarChar, Normalize(auditContext.IpAddress), 45);
    }

    private static void ValidateCategory(ListItemCategoryRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length < 2)
            throw new ArgumentException("Category code must contain at least two characters.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 2)
            throw new ArgumentException("Category name must contain at least two characters.");
    }

    private static void ValidateItem(ListItemRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ListItemCategoryId <= 0) throw new ArgumentException("A category is required.");
        if (request.DisplayOrder < 0) throw new ArgumentException("Display order cannot be negative.");
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length < 2)
            throw new ArgumentException("Item code must contain at least two characters.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 2)
            throw new ArgumentException("Item name must contain at least two characters.");
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
        var parameter = size.HasValue ? command.Parameters.Add(name, type, size.Value) : command.Parameters.Add(name, type);
        parameter.Value = value ?? DBNull.Value;
    }

    private static Exception Translate(SqlException exception) => exception.Number switch
    {
        50004 => new KeyNotFoundException(exception.Message, exception),
        >= 50000 and <= 50100 => new ArgumentException(exception.Message, exception),
        2601 or 2627 => new ArgumentException("A record with the same code already exists.", exception),
        _ => exception
    };
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NullableString(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
    private static DateTime? NullableDateTime(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}
