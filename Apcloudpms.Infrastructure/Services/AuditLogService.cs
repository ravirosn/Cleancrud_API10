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
    public async Task<AuditLogPagedResponseDto> GetPagedAsync(
        AuditLogQueryDto query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var connection = (SqlConnection)context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;
        if (shouldCloseConnection)
            await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.SpAuditLogsGet";
            command.CommandType = CommandType.StoredProcedure;
            Add(command, "@PageNumber", SqlDbType.Int, query.PageNumber);
            Add(command, "@PageSize", SqlDbType.Int, query.PageSize);
            Add(command, "@SearchTerm", SqlDbType.NVarChar, Normalize(query.Search), 200);
            Add(command, "@EntityName", SqlDbType.NVarChar, Normalize(query.EntityName), 128);
            Add(command, "@Action", SqlDbType.NVarChar, Normalize(query.Action), 20);

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
                : (totalRecords + query.PageSize - 1L) / query.PageSize;

            return new AuditLogPagedResponseDto(
                items,
                totalRecords,
                totalPages,
                query.PageNumber,
                query.PageSize,
                totalRecords > 0 && query.PageNumber > 1,
                query.PageNumber < totalPages);
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

    private static void Add(SqlCommand command, string name, SqlDbType type, object value) =>
        command.Parameters.Add(new SqlParameter(name, type) { Value = value });

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
