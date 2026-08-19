using System.Data;
using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using CleanCrud.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CleanCrud.Infrastructure.Services;

public sealed class RiskAssessmentService(AppDbContext context) : IRiskAssessmentService
{
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
