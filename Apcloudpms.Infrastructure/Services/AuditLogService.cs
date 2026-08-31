using System.Data;
using System.Text.Json;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Apcloudpms.Infrastructure.Services;

public sealed class AuditLogService(AppDbContext context) : IAuditLogService
{
    private const int MaximumExportRows = 10_000;

    public async Task<AuditLogPagedResponseDto> GetPagedAsync(
        AuditLogQueryDto query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await ExecuteAsync(query, query.PageNumber, query.PageSize, cancellationToken);
    }

    public async Task<AuditLogPagedResponseDto> GetExportAsync(
        AuditLogQueryDto query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await ExecuteAsync(query, 1, MaximumExportRows, cancellationToken);
    }

    public async Task<AuditLogFilterOptionsDto> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = (SqlConnection)context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;
        if (shouldCloseConnection)
            await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.SpAuditLogFilterOptionsGet";
            command.CommandType = CommandType.StoredProcedure;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var entityNames = new List<string>();
            while (await reader.ReadAsync(cancellationToken))
                entityNames.Add(reader.GetString(0));

            var actions = new List<string>();
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                    actions.Add(reader.GetString(0));
            }

            return new AuditLogFilterOptionsDto(entityNames, actions);
        }
        finally
        {
            if (shouldCloseConnection)
                await context.Database.CloseConnectionAsync();
        }
    }

    private async Task<AuditLogPagedResponseDto> ExecuteAsync(
        AuditLogQueryDto query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var connection = (SqlConnection)context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;
        if (shouldCloseConnection)
            await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.SpAuditLogsAdminGet";
            command.CommandType = CommandType.StoredProcedure;
            Add(command, "@PageNumber", SqlDbType.Int, pageNumber);
            Add(command, "@PageSize", SqlDbType.Int, pageSize);
            Add(command, "@SearchTerm", SqlDbType.NVarChar, Normalize(query.Search), 200);
            Add(command, "@EntityName", SqlDbType.NVarChar, Normalize(query.EntityName), 128);
            Add(command, "@Action", SqlDbType.NVarChar, Normalize(query.Action), 20);
            Add(command, "@ChangedBy", SqlDbType.NVarChar, Normalize(query.ChangedBy), 200);
            Add(command, "@FromUtc", SqlDbType.DateTime2, query.FromUtc);
            Add(command, "@ToUtc", SqlDbType.DateTime2, query.ToUtc);
            Add(command, "@SortBy", SqlDbType.NVarChar, Normalize(query.SortBy) ?? "changedAtUtc", 32);
            Add(command, "@SortDirection", SqlDbType.NVarChar, Normalize(query.SortDirection) ?? "desc", 4);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException(
                    "dbo.SpAuditLogsGet did not return the total record count.");

            var totalRecords = reader.GetInt64(reader.GetOrdinal("TotalRecords"));
            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException(
                    "dbo.SpAuditLogsGet did not return the paged records.");

            var items = new List<AuditLogItemDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new AuditLogItemDto(
                    reader.GetInt64(reader.GetOrdinal("Id")),
                    reader.GetString(reader.GetOrdinal("EntityName")),
                    reader.GetString(reader.GetOrdinal("EntityDisplayName")),
                    reader.GetString(reader.GetOrdinal("Action")),
                    GetNullableString(reader, "EntityKey"),
                    GetNullableString(reader, "ChangedColumns"),
                    GetNullableString(reader, "OldValues"),
                    GetNullableString(reader, "NewValues"),
                    DeserializeRelatedNames(GetNullableString(reader, "RelatedNames")),
                    GetNullableInt32(reader, "ChangedByUserId"),
                    GetNullableString(reader, "ChangedByName"),
                    GetNullableString(reader, "TraceId"),
                    GetNullableString(reader, "IpAddress"),
                    reader.GetDateTime(reader.GetOrdinal("ChangedAtUtc"))));
            }

            var totalPages = totalRecords == 0
                ? 0
                : (totalRecords + pageSize - 1L) / pageSize;

            return new AuditLogPagedResponseDto(
                items,
                totalRecords,
                totalPages,
                pageNumber,
                pageSize,
                totalRecords > 0 && pageNumber > 1,
                pageNumber < totalPages);
        }
        finally
        {
            if (shouldCloseConnection)
                await context.Database.CloseConnectionAsync();
        }
    }

    private static IReadOnlyDictionary<string, string> DeserializeRelatedNames(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();

    private static void Add(
        SqlCommand command,
        string name,
        SqlDbType type,
        object? value,
        int size)
    {
        command.Parameters.Add(new SqlParameter(name, type, size)
        {
            Value = value ?? DBNull.Value
        });
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object? value) =>
        command.Parameters.Add(new SqlParameter(name, type) { Value = value ?? DBNull.Value });

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? GetNullableInt32(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
