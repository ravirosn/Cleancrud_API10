using System.Data;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Apcloudpms.Infrastructure.Services;

public sealed class RiskAssessmentService(AppDbContext context) : IRiskAssessmentService
{
    public async Task<IReadOnlyList<RiskAssessmentPermitApplicationDto>> GetPermitApplicationsAsync(
        int riskAssessmentId,
        CancellationToken cancellationToken = default) =>
        await context.PermitApplications
            .AsNoTracking()
            .Where(x => x.RiskAssessmentId == riskAssessmentId)
            .OrderBy(x => x.Id)
            .Select(x => new RiskAssessmentPermitApplicationDto(
                x.Id,
                x.PermitNumber,
                x.IssueDate,
                x.PermitIssuerName,
                x.PermitReceiverName,
                x.PermitTypeListItemId,
                x.PermitTypeListItem.Name,
                x.PermitStatusListItemId,
                x.PermitStatusListItem.Name,
                x.RiskAssessmentId))
            .ToListAsync(cancellationToken);

    public Task<RiskAssessmentDetailsDto?> GetByIdAsync(
        int riskAssessmentId,
        CancellationToken cancellationToken = default) =>
        context.RiskAssessments
            .AsNoTracking()
            .Where(x => x.Id == riskAssessmentId)
            .Select(x => new RiskAssessmentDetailsDto(
                x.Id,
                x.PreRiskAssessmentNumber,
                x.IssueDate,
                x.PermitIssuerName,
                x.PermitReceiverName,
                x.AreaResponsibleName,
                x.LocationOfWork,
                x.DescriptionOfWork,
                x.SpecialInstructions,
                x.OtherEquipmentsPPE,
                x.OtherProtectionMeasures,
                x.PlannedStartDateTime,
                x.PlannedEndDateTime,
                x.RiskAssessmentStatusListItemId,
                x.RiskAssessmentStatusListItem.Name,
                x.CreatedBy,
                x.ModifiedBy,
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                x.AdditionalPersonalProtectiveEquipment
                    .OrderBy(item => item.AdditionalProtectiveMeasuresListItemId)
                    .Select(item => new RiskAssessmentSelectionDto
                    {
                        ListItemId = item.AdditionalProtectiveMeasuresListItemId,
                        IsSelected = item.IsSelected ?? false
                    }).ToList(),
                x.HazardCategories
                    .OrderBy(item => item.HazardCategoriesListItemId)
                    .Select(item => new RiskAssessmentSelectionDto
                    {
                        ListItemId = item.HazardCategoriesListItemId,
                        IsSelected = item.IsSelected ?? false
                    }).ToList(),
                x.PersonalProtectiveEquipment
                    .OrderBy(item => item.SpecialPermitListItemId)
                    .Select(item => new RiskAssessmentSelectionDto
                    {
                        ListItemId = item.SpecialPermitListItemId,
                        IsSelected = item.IsSelected ?? false
                    }).ToList(),
                x.SpecialPermits
                    .OrderBy(item => item.SpecialPermitListItemId)
                    .Select(item => new RiskAssessmentSelectionDto
                    {
                        ListItemId = item.SpecialPermitListItemId,
                        IsSelected = item.IsSelected ?? false
                    }).ToList()))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<RiskAssessmentPagedResponseDto> GetPagedAsync(
        RiskAssessmentQueryDto query,
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
            command.CommandText = "dbo.SpRiskAssessmentGet";
            command.CommandType = CommandType.StoredProcedure;
            Add(command, "@PageNumber", SqlDbType.Int, query.PageNumber);
            Add(command, "@PageSize", SqlDbType.Int, query.PageSize);
            Add(command, "@SearchTerm", SqlDbType.NVarChar, Normalize(query.Search), 200);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException(
                    "dbo.SpRiskAssessmentGet did not return the total record count.");

            var totalRecords = reader.GetInt64(reader.GetOrdinal("TotalRecords"));

            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException(
                    "dbo.SpRiskAssessmentGet did not return the paged records.");

            var items = new List<RiskAssessmentGridItemDto>();
            var idOrdinal = reader.GetOrdinal("Id");
            var numberOrdinal = reader.GetOrdinal("PreRiskAssessmentNumber");
            var issueDateOrdinal = reader.GetOrdinal("IssueDate");
            var issuerOrdinal = reader.GetOrdinal("PermitIssuerName");
            var receiverOrdinal = reader.GetOrdinal("PermitReceiverName");
            var responsibleOrdinal = reader.GetOrdinal("AreaResponsibleName");
            var startOrdinal = reader.GetOrdinal("PlannedStartDateTime");
            var endOrdinal = reader.GetOrdinal("PlannedEndDateTime");
            var statusIdOrdinal = reader.GetOrdinal("RiskAssessmentStatusListItemId");
            var statusOrdinal = reader.GetOrdinal("RiskAssessmentStatus");

            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new RiskAssessmentGridItemDto(
                    reader.GetInt32(idOrdinal),
                    reader.GetString(numberOrdinal),
                    DateOnly.FromDateTime(reader.GetDateTime(issueDateOrdinal)),
                    reader.GetString(issuerOrdinal),
                    reader.GetString(receiverOrdinal),
                    reader.GetString(responsibleOrdinal),
                    reader.IsDBNull(startOrdinal) ? null : reader.GetDateTime(startOrdinal),
                    reader.IsDBNull(endOrdinal) ? null : reader.GetDateTime(endOrdinal),
                    reader.GetInt32(statusIdOrdinal),
                    reader.GetString(statusOrdinal)));
            }

            var totalPages = totalRecords == 0
                ? 0
                : (totalRecords + query.PageSize - 1L) / query.PageSize;

            return new RiskAssessmentPagedResponseDto(
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

    public Task<RiskAssessmentWriteResult> CreateAsync(
        RiskAssessmentRequestDto request,
        int userId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync("dbo.SpRiskAssessmentIns", null, request, userId, cancellationToken);

    public Task<RiskAssessmentWriteResult> UpdateAsync(
        int riskAssessmentId,
        RiskAssessmentRequestDto request,
        int userId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync("dbo.SpRiskAssessmentUpd", riskAssessmentId, request, userId, cancellationToken);

    private async Task<RiskAssessmentWriteResult> ExecuteAsync(
        string procedureName,
        int? riskAssessmentId,
        RiskAssessmentRequestDto request,
        int userId,
        CancellationToken cancellationToken)
    {
        var connection = (SqlConnection)context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;
        if (shouldCloseConnection)
            await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;

            if (riskAssessmentId.HasValue)
                command.Parameters.Add(new SqlParameter("@RiskAssessmentId", SqlDbType.Int)
                    { Value = riskAssessmentId.Value });

            Add(command, "@PreRiskAssessmentNumber", SqlDbType.NVarChar, request.PreRiskAssessmentNumber.Trim(), 50);
            Add(command, "@IssueDate", SqlDbType.Date, request.IssueDate.ToDateTime(TimeOnly.MinValue));
            Add(command, "@PermitIssuerName", SqlDbType.NVarChar, request.PermitIssuerName.Trim(), 100);
            Add(command, "@PermitReceiverName", SqlDbType.NVarChar, request.PermitReceiverName.Trim(), 100);
            Add(command, "@AreaResponsibleName", SqlDbType.NVarChar, request.AreaResponsibleName.Trim(), 100);
            Add(command, "@LocationOfWork", SqlDbType.NVarChar, request.LocationOfWork.Trim(), 255);
            Add(command, "@DescriptionOfWork", SqlDbType.NVarChar, Normalize(request.DescriptionOfWork), -1);
            Add(command, "@SpecialInstructions", SqlDbType.NVarChar, Normalize(request.SpecialInstructions), -1);
            Add(command, "@OtherEquipmentsPPE", SqlDbType.NVarChar, Normalize(request.OtherEquipmentsPPE), 500);
            Add(command, "@OtherProtectionMeasures", SqlDbType.NVarChar, Normalize(request.OtherProtectionMeasures), 500);
            Add(command, "@PlannedStartDateTime", SqlDbType.DateTime2, request.PlannedStartDateTime);
            Add(command, "@PlannedEndDateTime", SqlDbType.DateTime2, request.PlannedEndDateTime);
            Add(command, riskAssessmentId.HasValue ? "@ModifiedBy" : "@CreatedBy", SqlDbType.Int, userId);

            AddSelections(command, "@AdditionalPpe", request.AdditionalPpe);
            AddSelections(command, "@HazardCategories", request.HazardCategories);
            AddSelections(command, "@PersonalProtectiveEquipment", request.PersonalProtectiveEquipment);
            AddSelections(command, "@SpecialPermits", request.SpecialPermits);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException($"{procedureName} did not return the saved risk assessment.");

            return new RiskAssessmentWriteResult(
                RiskAssessmentWriteOutcome.Success,
                new RiskAssessmentWriteResponseDto(
                    reader.GetInt32(reader.GetOrdinal("RiskAssessmentId")),
                    reader.GetInt32(reader.GetOrdinal("RiskAssessmentStatusListItemId")),
                    reader.GetString(reader.GetOrdinal("Status")),
                    reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc"))));
        }
        catch (SqlException exception) when (exception.Number == 50001)
        {
            return new RiskAssessmentWriteResult(RiskAssessmentWriteOutcome.NotFound);
        }
        catch (SqlException exception) when (exception.Number == 50002)
        {
            return new RiskAssessmentWriteResult(RiskAssessmentWriteOutcome.NotDraft);
        }
        finally
        {
            if (shouldCloseConnection)
                await context.Database.CloseConnectionAsync();
        }
    }

    private static void Add(
        SqlCommand command,
        string name,
        SqlDbType type,
        object? value,
        int? size = null)
    {
        var parameter = new SqlParameter(name, type) { Value = value ?? DBNull.Value };
        if (size.HasValue)
            parameter.Size = size.Value;
        command.Parameters.Add(parameter);
    }

    private static void AddSelections(
        SqlCommand command,
        string name,
        IEnumerable<RiskAssessmentSelectionDto>? selections)
    {
        var table = new DataTable();
        table.Columns.Add("ListItemId", typeof(int));
        table.Columns.Add("IsSelected", typeof(bool));
        foreach (var selection in selections ?? [])
            table.Rows.Add(selection.ListItemId, selection.IsSelected);

        command.Parameters.Add(new SqlParameter(name, SqlDbType.Structured)
        {
            TypeName = "dbo.RiskAssessmentSelectionTableType",
            Value = table
        });
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
