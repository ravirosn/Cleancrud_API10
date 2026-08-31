using System.Data;
using System.Text.Json;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Apcloudpms.Infrastructure.Services;

public sealed class WorkflowSetupService(
    AppDbContext context,
    IAuditContext auditContext) : IWorkflowSetupService
{
    public Task<WorkflowSetupPagedResponseDto> GetAsync(
        WorkflowSetupQueryDto query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return WithConnectionAsync(async connection =>
        {
            await using var command = Command(connection, "dbo.SpWorkflowSetupGet");
            Add(command, "@PageNumber", SqlDbType.Int, query.PageNumber);
            Add(command, "@PageSize", SqlDbType.Int, query.PageSize);
            Add(command, "@SearchTerm", SqlDbType.NVarChar, Normalize(query.SearchTerm), 200);
            Add(command, "@SortBy", SqlDbType.NVarChar, query.SortBy, 30);
            Add(command, "@SortDirection", SqlDbType.VarChar, query.SortDirection, 4);
            Add(command, "@ApplicationModuleId", SqlDbType.Int, query.ApplicationModuleId);
            Add(command, "@IncludeInactive", SqlDbType.Bit, query.IncludeInactive);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("dbo.SpWorkflowSetupGet did not return a record count.");
            var totalRecords = reader.GetInt64(reader.GetOrdinal("TotalRecords"));
            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException("dbo.SpWorkflowSetupGet did not return workflows.");
            var data = new List<WorkflowSetupGridDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var active = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                data.Add(new WorkflowSetupGridDto(
                    reader.GetInt32(reader.GetOrdinal("Id")),
                    reader.GetString(reader.GetOrdinal("WorkflowCode")),
                    reader.GetInt32(reader.GetOrdinal("ApplicationModuleId")),
                    reader.GetString(reader.GetOrdinal("ModuleName")),
                    reader.GetString(reader.GetOrdinal("SubjectType")),
                    NullableInt(reader, "SubjectTypeListItemId"),
                    NullableString(reader, "SubjectTypeName"),
                    reader.GetString(reader.GetOrdinal("Name")),
                    reader.GetInt32(reader.GetOrdinal("LevelCount")), active,
                    active ? "Active" : "Inactive",
                    reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
                    NullableDateTime(reader, "UpdatedAtUtc")));
            }
            var totalPages = totalRecords == 0 ? 0 : (totalRecords + query.PageSize - 1L) / query.PageSize;
            return new WorkflowSetupPagedResponseDto(
                data, totalRecords, totalPages, query.PageNumber, query.PageSize,
                totalRecords > 0 && query.PageNumber > 1, query.PageNumber < totalPages);
        }, cancellationToken);
    }

    public Task<WorkflowSetupDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        WithConnectionAsync(async connection =>
        {
            await using var command = Command(connection, "dbo.SpWorkflowSetupGetById");
            Add(command, "@Id", SqlDbType.Int, id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var header = new WorkflowHeader(
                reader.GetInt32(reader.GetOrdinal("Id")),
                reader.GetInt32(reader.GetOrdinal("ApplicationModuleId")),
                reader.GetString(reader.GetOrdinal("ModuleCode")),
                reader.GetString(reader.GetOrdinal("ModuleName")),
                reader.GetString(reader.GetOrdinal("WorkflowCode")),
                reader.GetString(reader.GetOrdinal("SubjectType")),
                NullableInt(reader, "SubjectTypeListItemId"),
                NullableString(reader, "SubjectTypeName"),
                reader.GetString(reader.GetOrdinal("Name")),
                reader.GetBoolean(reader.GetOrdinal("IsActive")),
                reader.GetString(reader.GetOrdinal("PendingNotificationTitle")),
                reader.GetString(reader.GetOrdinal("PendingNotificationMessage")),
                reader.GetString(reader.GetOrdinal("ApprovedNotificationTitle")),
                reader.GetString(reader.GetOrdinal("ApprovedNotificationMessage")),
                reader.GetString(reader.GetOrdinal("RejectedNotificationTitle")),
                reader.GetString(reader.GetOrdinal("RejectedNotificationMessage")));
            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException("dbo.SpWorkflowSetupGetById did not return workflow levels.");
            var levels = new List<ApprovalWorkflowLevelDto>();
            while (await reader.ReadAsync(cancellationToken))
                levels.Add(new ApprovalWorkflowLevelDto(
                    reader.GetInt32(reader.GetOrdinal("Id")),
                    reader.GetByte(reader.GetOrdinal("LevelNumber")),
                    reader.GetInt32(reader.GetOrdinal("PrimaryApproverRoleId")),
                    reader.GetString(reader.GetOrdinal("PrimaryApproverRoleName")),
                    NullableInt(reader, "AlternateApproverRoleId"),
                    NullableString(reader, "AlternateApproverRoleName")));
            return header.ToDto(levels);
        }, cancellationToken);

    public async Task<WorkflowSetupDetailDto> CreateAsync(
        WorkflowSetupRequestDto request, CancellationToken cancellationToken)
    {
        Validate(request);
        var id = await ExecuteWriteAsync("dbo.SpWorkflowSetupAdd", null, request, cancellationToken);
        return await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("The saved workflow could not be reloaded.");
    }

    public async Task<WorkflowSetupDetailDto?> UpdateAsync(
        int id, WorkflowSetupRequestDto request, CancellationToken cancellationToken)
    {
        Validate(request);
        try
        {
            await ExecuteWriteAsync("dbo.SpWorkflowSetupEdit", id, request, cancellationToken);
            return await GetByIdAsync(id, cancellationToken);
        }
        catch (KeyNotFoundException) { return null; }
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        WithConnectionAsync(async connection =>
        {
            await using var command = Command(connection, "dbo.SpWorkflowSetupDelete");
            Add(command, "@Id", SqlDbType.Int, id);
            AddAudit(command);
            try { return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1; }
            catch (SqlException exception) { throw Translate(exception); }
        }, cancellationToken);

    public Task<IReadOnlyList<WorkflowModuleOptionDto>> GetModulesAsync(CancellationToken cancellationToken) =>
        ReadOptionsAsync("dbo.SpWorkflowSetupModulesDdl", null, reader =>
            new WorkflowModuleOptionDto(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)), cancellationToken);

    public Task<IReadOnlyList<WorkflowRoleOptionDto>> GetRolesAsync(CancellationToken cancellationToken) =>
        ReadOptionsAsync("dbo.SpWorkflowSetupRolesDdl", null, reader =>
            new WorkflowRoleOptionDto(reader.GetInt32(0), reader.GetString(1)), cancellationToken);

    public Task<IReadOnlyList<WorkflowSubjectCategoryOptionDto>> GetSubjectCategoriesAsync(CancellationToken cancellationToken) =>
        ReadOptionsAsync("dbo.SpWorkflowSetupSubjectCategoriesDdl", null, reader =>
            new WorkflowSubjectCategoryOptionDto(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)), cancellationToken);

    public Task<IReadOnlyList<WorkflowSubjectOptionDto>> GetSubjectsAsync(
        string categoryCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(categoryCode))
            throw new ArgumentException("A subject category is required.");
        return ReadOptionsAsync("dbo.SpWorkflowSetupSubjectsDdl",
            command => Add(command, "@CategoryCode", SqlDbType.NVarChar, categoryCode.Trim().ToUpperInvariant(), 100),
            reader => new WorkflowSubjectOptionDto(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)),
            cancellationToken);
    }

    private Task<int> ExecuteWriteAsync(
        string procedure, int? id, WorkflowSetupRequestDto request,
        CancellationToken cancellationToken) => WithConnectionAsync(async connection =>
    {
        await using var command = Command(connection, procedure);
        if (id.HasValue) Add(command, "@Id", SqlDbType.Int, id.Value);
        Add(command, "@ApplicationModuleId", SqlDbType.Int, request.ApplicationModuleId);
        Add(command, "@WorkflowCode", SqlDbType.NVarChar, request.WorkflowCode.Trim().ToUpperInvariant(), 100);
        Add(command, "@SubjectType", SqlDbType.NVarChar, request.SubjectType.Trim().ToUpperInvariant(), 100);
        Add(command, "@SubjectTypeListItemId", SqlDbType.Int, request.SubjectTypeListItemId);
        Add(command, "@Name", SqlDbType.NVarChar, request.Name.Trim(), 150);
        Add(command, "@IsActive", SqlDbType.Bit, request.IsActive);
        Add(command, "@LevelsJson", SqlDbType.NVarChar, JsonSerializer.Serialize(request.Levels), -1);
        Add(command, "@PendingTitle", SqlDbType.NVarChar, request.PendingNotificationTitle.Trim(), 200);
        Add(command, "@PendingMessage", SqlDbType.NVarChar, request.PendingNotificationMessage.Trim(), 1000);
        Add(command, "@ApprovedTitle", SqlDbType.NVarChar, request.ApprovedNotificationTitle.Trim(), 200);
        Add(command, "@ApprovedMessage", SqlDbType.NVarChar, request.ApprovedNotificationMessage.Trim(), 1000);
        Add(command, "@RejectedTitle", SqlDbType.NVarChar, request.RejectedNotificationTitle.Trim(), 200);
        Add(command, "@RejectedMessage", SqlDbType.NVarChar, request.RejectedNotificationMessage.Trim(), 1000);
        AddAudit(command);
        try { return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)); }
        catch (SqlException exception) { throw Translate(exception); }
    }, cancellationToken);

    private Task<IReadOnlyList<T>> ReadOptionsAsync<T>(
        string procedure, Action<SqlCommand>? parameters, Func<SqlDataReader, T> map,
        CancellationToken cancellationToken) => WithConnectionAsync<IReadOnlyList<T>>(async connection =>
    {
        await using var command = Command(connection, procedure);
        parameters?.Invoke(command);
        var result = new List<T>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(map(reader));
        return result;
    }, cancellationToken);

    private void AddAudit(SqlCommand command)
    {
        Add(command, "@ActorUserId", SqlDbType.Int, auditContext.UserId);
        Add(command, "@ActorName", SqlDbType.NVarChar, Normalize(auditContext.UserName), 256);
        Add(command, "@TraceId", SqlDbType.NVarChar, Normalize(auditContext.TraceId), 100);
        Add(command, "@IpAddress", SqlDbType.NVarChar, Normalize(auditContext.IpAddress), 45);
    }

    private static void Validate(WorkflowSetupRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var levels = request.Levels.OrderBy(x => x.LevelNumber).ToArray();
        if (levels.Length is < 1 or > 5 ||
            !levels.Select(x => (int)x.LevelNumber).SequenceEqual(Enumerable.Range(1, levels.Length)))
            throw new ArgumentException("Workflow levels must be sequential from level 1 and cannot exceed five levels.");
        if (levels.Any(x => x.PrimaryApproverRoleId <= 0 ||
            x.AlternateApproverRoleId == x.PrimaryApproverRoleId))
            throw new ArgumentException("Approver roles are invalid or duplicated within a level.");
        if (request.ApplicationModuleId <= 0) throw new ArgumentException("An application module is required.");
        if (string.IsNullOrWhiteSpace(request.WorkflowCode)) throw new ArgumentException("Workflow code is required.");
        if (string.IsNullOrWhiteSpace(request.SubjectType)) throw new ArgumentException("Subject type is required.");
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Workflow name is required.");
        var templates = new[] { request.PendingNotificationTitle, request.PendingNotificationMessage,
            request.ApprovedNotificationTitle, request.ApprovedNotificationMessage,
            request.RejectedNotificationTitle, request.RejectedNotificationMessage };
        if (templates.Any(string.IsNullOrWhiteSpace) || templates.Any(x => !x.Contains("{Reference}", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Every notification template must contain the {Reference} placeholder.");
    }

    private async Task<T> WithConnectionAsync<T>(Func<SqlConnection, Task<T>> action, CancellationToken cancellationToken)
    {
        var connection = (SqlConnection)context.Database.GetDbConnection();
        var close = connection.State == ConnectionState.Closed;
        if (close) await context.Database.OpenConnectionAsync(cancellationToken);
        try { return await action(connection); }
        finally { if (close) await context.Database.CloseConnectionAsync(); }
    }

    private static SqlCommand Command(SqlConnection connection, string procedure) =>
        new(procedure, connection) { CommandType = CommandType.StoredProcedure };
    private static void Add(SqlCommand command, string name, SqlDbType type, object? value, int? size = null)
    {
        var parameter = size.HasValue ? command.Parameters.Add(name, type, size.Value) : command.Parameters.Add(name, type);
        parameter.Value = value ?? DBNull.Value;
    }
    private static Exception Translate(SqlException exception) => exception.Number switch
    {
        50004 => new KeyNotFoundException(exception.Message, exception),
        >= 50000 and <= 50199 => new ArgumentException(exception.Message, exception),
        2601 or 2627 => new ArgumentException("A workflow with the same code or active scope already exists.", exception),
        _ => exception
    };
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NullableString(SqlDataReader reader, string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : reader.GetString(i); }
    private static int? NullableInt(SqlDataReader reader, string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : reader.GetInt32(i); }
    private static DateTime? NullableDateTime(SqlDataReader reader, string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : reader.GetDateTime(i); }

    private sealed record WorkflowHeader(
        int Id, int ApplicationModuleId, string ModuleCode, string ModuleName,
        string WorkflowCode, string SubjectType, int? SubjectTypeListItemId,
        string? SubjectTypeName, string Name, bool IsActive,
        string PendingTitle, string PendingMessage, string ApprovedTitle,
        string ApprovedMessage, string RejectedTitle, string RejectedMessage)
    {
        public WorkflowSetupDetailDto ToDto(IReadOnlyList<ApprovalWorkflowLevelDto> levels) => new(
            Id, ApplicationModuleId, ModuleCode, ModuleName, WorkflowCode, SubjectType,
            SubjectTypeListItemId, SubjectTypeName, Name, IsActive,
            PendingTitle, PendingMessage, ApprovedTitle, ApprovedMessage,
            RejectedTitle, RejectedMessage, levels);
    }
}
