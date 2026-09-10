using System.Data;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Apcloudpms.Infrastructure.Services;

public sealed class PermitApplicationService(AppDbContext context) : IPermitApplicationService
{
    private const string HotWorkPermitType = "HOT_WORK";
    private const string PermitDraftStatus = "PERMIT_DRAFT";
    private const string PermitRejectedStatus = "PERMIT_REJECTED";
    private const string PermitStatusCategory = "PERMIT_STATUS";
    private const string FinalizedForApprovalStatus = "FINALIZED_FOR_APPROVAL";
    private const string InspectionPriorToCommencementCategory = "INSPECTIONPRIORTOCOMMENCEMENT";
    private const string WorksOnWallCategory = "WORKSONWALL";
    private const string WorkingInConfinedSpaceCategory = "WORNING_IN_CONFINES_SPACE";

    public async Task<PermitApplicationDetailsDto?> GetByIdAsync(
        long permitApplicationId,
        CancellationToken cancellationToken = default)
    {
        var permitApplication = await context.PermitApplications
            .AsNoTracking()
            .Where(x => x.Id == permitApplicationId)
            .Select(x => new
            {
                x.Id,
                x.RiskAssessmentId,
                x.PermitNumber,
                x.IssueDate,
                x.PlannedStartDateTime,
                x.PlannedEndDateTime,
                x.PermitIssuerId,
                PermitIssuerName = x.PermitIssuer.DisplayName ?? x.PermitIssuer.UserName,
                PermitIssuerContactNumber = x.PermitIssuer.ContactNumber,
                x.PermitReceiverId,
                PermitReceiverName = x.PermitReceiver.DisplayName ?? x.PermitReceiver.UserName,
                PermitReceiverContactNumber = x.PermitReceiver.ContactNumber,
                x.RiskAssessmentNumber,
                x.WorkLocation,
                x.WorkDescription,
                x.SpecialInstructions,
                x.WorkHeightBelowSurface,
                x.PermitTypeListItemId,
                PermitTypeSystemName = x.PermitTypeListItem.Code,
                PermitTypeName = x.PermitTypeListItem.Name,
                x.PermitStatusListItemId,
                PermitStatusSystemName = x.PermitStatusListItem.Code,
                PermitStatusName = x.PermitStatusListItem.Name,
                x.SubmittedAtUtc,
                x.CreatedByUserId,
                x.UpdatedByUserId,
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                x.CompletionOfWorks,
                x.CompletionApprovedBy,
                x.CompletionDate,
                x.CompletionRemarks,
                x.CancelledBy,
                x.CancelledDate,
                x.CancelledRemarks
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (permitApplication is null)
            return null;

        var selections = permitApplication.PermitTypeSystemName == HotWorkPermitType
            ? await GetHotWorkSelectionsAsync(permitApplicationId, cancellationToken)
            : [];

        var inspectionPriorToCommencement = GetCategorySelections(
            selections, InspectionPriorToCommencementCategory);
        var worksOnWall = GetCategorySelections(selections, WorksOnWallCategory);
        var workingInConfinedSpace = GetCategorySelections(
            selections, WorkingInConfinedSpaceCategory);

        return new PermitApplicationDetailsDto(
            permitApplication.Id,
            permitApplication.RiskAssessmentId,
            permitApplication.PermitNumber,
            permitApplication.IssueDate,
            permitApplication.PlannedStartDateTime,
            permitApplication.PlannedEndDateTime,
            permitApplication.PermitIssuerId,
            permitApplication.PermitIssuerName,
            permitApplication.PermitIssuerContactNumber,
            permitApplication.PermitReceiverId,
            permitApplication.PermitReceiverName,
            permitApplication.PermitReceiverContactNumber,
            permitApplication.RiskAssessmentNumber,
            permitApplication.WorkLocation,
            permitApplication.WorkDescription,
            permitApplication.SpecialInstructions,
            permitApplication.WorkHeightBelowSurface,
            permitApplication.PermitTypeListItemId,
            permitApplication.PermitTypeSystemName,
            permitApplication.PermitTypeName,
            permitApplication.PermitStatusListItemId,
            permitApplication.PermitStatusSystemName,
            permitApplication.PermitStatusName,
            permitApplication.SubmittedAtUtc,
            permitApplication.CreatedByUserId,
            permitApplication.UpdatedByUserId,
            permitApplication.CreatedAtUtc,
            permitApplication.UpdatedAtUtc,
            permitApplication.CompletionOfWorks,
            permitApplication.CompletionApprovedBy,
            permitApplication.CompletionDate,
            permitApplication.CompletionRemarks,
            permitApplication.CancelledBy,
            permitApplication.CancelledDate,
            permitApplication.CancelledRemarks,
            inspectionPriorToCommencement,
            worksOnWall,
            workingInConfinedSpace);
    }

    public async Task<PermitApplicationDetailsDto?> GetPreviewAsync(
        long permitApplicationId,
        CancellationToken cancellationToken = default)
    {
        var connection = (SqlConnection)context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;
        if (shouldCloseConnection)
            await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.SpPermitApplicationPreviewGet";
            command.CommandType = CommandType.StoredProcedure;
            Add(command, "@PermitApplicationId", SqlDbType.BigInt, permitApplicationId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            var values = new
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                RiskAssessmentId = GetNullableInt32(reader, "RiskAssessmentId"),
                PermitNumber = reader.GetString(reader.GetOrdinal("PermitNumber")),
                IssueDate = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("IssueDate"))),
                PlannedStartDateTime = GetNullableDateTime(reader, "PlannedStartDateTime"),
                PlannedEndDateTime = GetNullableDateTime(reader, "PlannedEndDateTime"),
                PermitIssuerId = reader.GetInt32(reader.GetOrdinal("PermitIssuerId")),
                PermitIssuerName = reader.GetString(reader.GetOrdinal("PermitIssuerName")),
                PermitIssuerContactNumber = GetNullableString(reader, "PermitIssuerContactNumber"),
                PermitReceiverId = reader.GetInt32(reader.GetOrdinal("PermitReceiverId")),
                PermitReceiverName = reader.GetString(reader.GetOrdinal("PermitReceiverName")),
                PermitReceiverContactNumber = GetNullableString(reader, "PermitReceiverContactNumber"),
                RiskAssessmentNumber = GetNullableString(reader, "RiskAssessmentNumber"),
                WorkLocation = reader.GetString(reader.GetOrdinal("WorkLocation")),
                WorkDescription = reader.GetString(reader.GetOrdinal("WorkDescription")),
                SpecialInstructions = GetNullableString(reader, "SpecialInstructions"),
                WorkHeightBelowSurface = GetNullableString(reader, "WorkHeightBelowSurface"),
                PermitTypeListItemId = reader.GetInt32(reader.GetOrdinal("PermitTypeListItemId")),
                PermitTypeSystemName = reader.GetString(reader.GetOrdinal("PermitTypeSystemName")),
                PermitTypeName = reader.GetString(reader.GetOrdinal("PermitTypeName")),
                PermitStatusListItemId = reader.GetInt32(reader.GetOrdinal("PermitStatusListItemId")),
                PermitStatusSystemName = reader.GetString(reader.GetOrdinal("PermitStatusSystemName")),
                PermitStatusName = reader.GetString(reader.GetOrdinal("PermitStatusName")),
                SubmittedAtUtc = GetNullableDateTime(reader, "SubmittedAtUtc"),
                CreatedByUserId = GetNullableInt32(reader, "CreatedByUserId"),
                UpdatedByUserId = GetNullableInt32(reader, "UpdatedByUserId"),
                CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
                UpdatedAtUtc = GetNullableDateTime(reader, "UpdatedAtUtc"),
                CompletionOfWorks = GetNullableString(reader, "CompletionOfWorks"),
                CompletionApprovedBy = GetNullableInt32(reader, "CompletionApprovedBy"),
                CompletionDate = GetNullableDateTime(reader, "CompletionDate"),
                CompletionRemarks = GetNullableString(reader, "CompletionRemarks"),
                CancelledBy = GetNullableInt32(reader, "CancelledBy"),
                CancelledDate = GetNullableDateTime(reader, "CancelledDate"),
                CancelledRemarks = GetNullableString(reader, "CancelledRemarks")
            };

            var inspections = await ReadPreviewSelectionsAsync(reader, cancellationToken);
            var wallWorks = await ReadPreviewSelectionsAsync(reader, cancellationToken);
            var confinedSpaces = await ReadPreviewSelectionsAsync(reader, cancellationToken);

            return new PermitApplicationDetailsDto(
                values.Id, values.RiskAssessmentId, values.PermitNumber, values.IssueDate,
                values.PlannedStartDateTime, values.PlannedEndDateTime,
                values.PermitIssuerId, values.PermitIssuerName, values.PermitIssuerContactNumber,
                values.PermitReceiverId, values.PermitReceiverName, values.PermitReceiverContactNumber,
                values.RiskAssessmentNumber, values.WorkLocation, values.WorkDescription,
                values.SpecialInstructions, values.WorkHeightBelowSurface, values.PermitTypeListItemId,
                values.PermitTypeSystemName, values.PermitTypeName, values.PermitStatusListItemId,
                values.PermitStatusSystemName, values.PermitStatusName, values.SubmittedAtUtc,
                values.CreatedByUserId, values.UpdatedByUserId, values.CreatedAtUtc, values.UpdatedAtUtc,
                values.CompletionOfWorks, values.CompletionApprovedBy, values.CompletionDate,
                values.CompletionRemarks, values.CancelledBy, values.CancelledDate, values.CancelledRemarks,
                inspections, wallWorks, confinedSpaces);
        }
        finally
        {
            if (shouldCloseConnection)
                await context.Database.CloseConnectionAsync();
        }
    }

    public async Task<PermitApplicationPagedResponseDto> GetByCreatedUserAsync(
        int userId,
        PermitApplicationQueryDto query,
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
            command.CommandText = "dbo.SpPermitApplicationsGet";
            command.CommandType = CommandType.StoredProcedure;
            Add(command, "@CreatedByUserId", SqlDbType.Int, userId);
            Add(command, "@PageNumber", SqlDbType.Int, query.PageNumber);
            Add(command, "@PageSize", SqlDbType.Int, query.PageSize);
            Add(command, "@SearchTerm", SqlDbType.NVarChar, Normalize(query.Search), 200);
            Add(command, "@SortBy", SqlDbType.NVarChar, query.SortBy, 40);
            Add(command, "@SortDirection", SqlDbType.VarChar, query.SortDirection, 4);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException(
                    "dbo.SpPermitApplicationsGet did not return the total record count.");

            var totalRecords = reader.GetInt64(reader.GetOrdinal("TotalRecords"));

            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException(
                    "dbo.SpPermitApplicationsGet did not return the paged records.");

            var items = new List<UserPermitApplicationDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new UserPermitApplicationDto(
                    reader.GetInt64(reader.GetOrdinal("Id")),
                    reader.GetString(reader.GetOrdinal("PermitNumber")),
                    DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("IssueDate"))),
                    reader.GetInt32(reader.GetOrdinal("PermitIssuerId")),
                    reader.GetString(reader.GetOrdinal("PermitIssuerName")),
                    reader.GetInt32(reader.GetOrdinal("PermitReceiverId")),
                    reader.GetString(reader.GetOrdinal("PermitReceiverName")),
                    reader.GetInt32(reader.GetOrdinal("PermitTypeListItemId")),
                    reader.GetString(reader.GetOrdinal("PermitTypeName")),
                    reader.GetInt32(reader.GetOrdinal("PermitStatusListItemId")),
                    reader.GetString(reader.GetOrdinal("PermitStatusName")),
                    GetNullableDateTime(reader, "SubmittedAtUtc"),
                    GetNullableInt32(reader, "CreatedByUserId"),
                    reader.GetString(reader.GetOrdinal("CreatedByUserName")),
                    GetNullableString(reader, "RiskAssessmentNumber"),
                    GetNullableInt32(reader, "RiskAssessmentId")));
            }

            var totalPages = totalRecords == 0
                ? 0
                : (totalRecords + query.PageSize - 1L) / query.PageSize;

            return new PermitApplicationPagedResponseDto(
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

    public async Task<PermitApplicationActionResponseDto?> CompleteAsync(
        long permitApplicationId,
        string? remarks,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var permitApplication = await context.PermitApplications
            .SingleOrDefaultAsync(x => x.Id == permitApplicationId, cancellationToken);
        if (permitApplication is null)
            return null;

        var actionedAtUtc = DateTime.UtcNow;
        permitApplication.CompletionRemarks = Normalize(remarks);
        permitApplication.CompletionApprovedBy = userId;
        permitApplication.CompletionDate = actionedAtUtc;
        permitApplication.UpdatedByUserId = userId;
        permitApplication.UpdatedAtUtc = actionedAtUtc;

        await context.SaveChangesAsync(cancellationToken);
        return new PermitApplicationActionResponseDto(
            permitApplication.Id,
            permitApplication.CompletionRemarks,
            userId,
            actionedAtUtc);
    }

    public async Task<PermitApplicationActionResponseDto?> CancelAsync(
        long permitApplicationId,
        string? remarks,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var permitApplication = await context.PermitApplications
            .SingleOrDefaultAsync(x => x.Id == permitApplicationId, cancellationToken);
        if (permitApplication is null)
            return null;

        var actionedAtUtc = DateTime.UtcNow;
        permitApplication.CancelledRemarks = Normalize(remarks);
        permitApplication.CancelledBy = userId;
        permitApplication.CancelledDate = actionedAtUtc;
        permitApplication.UpdatedByUserId = userId;
        permitApplication.UpdatedAtUtc = actionedAtUtc;

        await context.SaveChangesAsync(cancellationToken);
        return new PermitApplicationActionResponseDto(
            permitApplication.Id,
            permitApplication.CancelledRemarks,
            userId,
            actionedAtUtc);
    }

    public Task<PermitApplicationUpdateResult> UpdateAsync(
        long permitApplicationId,
        PermitApplicationUpdateRequestDto request,
        int userId,
        CancellationToken cancellationToken = default) =>
        UpdateInternalAsync(
            permitApplicationId, request, userId, finalizeForApproval: false, cancellationToken);

    public Task<PermitApplicationUpdateResult> UpdateAndFinalizeAsync(
        long permitApplicationId,
        PermitApplicationUpdateRequestDto request,
        int userId,
        CancellationToken cancellationToken = default) =>
        UpdateInternalAsync(
            permitApplicationId, request, userId, finalizeForApproval: true, cancellationToken);

    private async Task<PermitApplicationUpdateResult> UpdateInternalAsync(
        long permitApplicationId,
        PermitApplicationUpdateRequestDto request,
        int userId,
        bool finalizeForApproval,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IssueDate == default)
        {
            return new PermitApplicationUpdateResult(
                PermitApplicationUpdateOutcome.InvalidSelections,
                Message: "IssueDate is required.");
        }

        if (request.PlannedStartDateTime.HasValue
            && request.PlannedEndDateTime.HasValue
            && request.PlannedEndDateTime < request.PlannedStartDateTime)
        {
            return new PermitApplicationUpdateResult(
                PermitApplicationUpdateOutcome.InvalidSelections,
                Message: "Planned end date/time must be on or after planned start date/time.");
        }

        if (HasDuplicateIds(request.InspectionPriorToCommencement)
            || HasDuplicateIds(request.WorksOnWall)
            || HasDuplicateIds(request.WorkingInConfinedSpace))
        {
            return new PermitApplicationUpdateResult(
                PermitApplicationUpdateOutcome.InvalidSelections,
                Message: "A list item may only appear once in each selection collection.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var connection = (SqlConnection)context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.Transaction = (SqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "dbo.SpPermitApplicationUpd";
            command.CommandType = CommandType.StoredProcedure;
            Add(command, "@PermitApplicationId", SqlDbType.BigInt, permitApplicationId);
            Add(command, "@IssueDate", SqlDbType.Date, request.IssueDate.ToDateTime(TimeOnly.MinValue));
            Add(command, "@PlannedStartDateTime", SqlDbType.DateTime2, request.PlannedStartDateTime);
            Add(command, "@PlannedEndDateTime", SqlDbType.DateTime2, request.PlannedEndDateTime);
            Add(command, "@PermitIssuerId", SqlDbType.Int, request.PermitIssuerId);
            Add(command, "@PermitIssuerContactNumber", SqlDbType.NVarChar, Normalize(request.PermitIssuerContactNumber), 30);
            Add(command, "@PermitReceiverId", SqlDbType.Int, request.PermitReceiverId);
            Add(command, "@PermitReceiverContactNumber", SqlDbType.NVarChar, Normalize(request.PermitReceiverContactNumber), 30);
            Add(command, "@WorkLocation", SqlDbType.NVarChar, request.WorkLocation.Trim(), 500);
            Add(command, "@WorkDescription", SqlDbType.NVarChar, request.WorkDescription.Trim(), -1);
            Add(command, "@SpecialInstructions", SqlDbType.NVarChar, Normalize(request.SpecialInstructions), -1);
            Add(command, "@WorkHeightBelowSurface", SqlDbType.NVarChar, Normalize(request.WorkHeightBelowSurface), 200);
            Add(command, "@CompletionOfWorks", SqlDbType.NVarChar, Normalize(request.CompletionOfWorks), 500);
            Add(command, "@UpdatedByUserId", SqlDbType.Int, userId);
            Add(command, "@FinalizeForApproval", SqlDbType.Bit, finalizeForApproval);

            long savedId;
            int statusId;
            string statusCode;
            string permitTypeCode;
            DateTime updatedAtUtc;
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                if (!await reader.ReadAsync(cancellationToken))
                    throw new InvalidOperationException("dbo.SpPermitApplicationUpd did not return the updated permit.");
                savedId = reader.GetInt64(reader.GetOrdinal("PermitApplicationId"));
                statusId = reader.GetInt32(reader.GetOrdinal("PermitStatusListItemId"));
                statusCode = reader.GetString(reader.GetOrdinal("PermitStatusSystemName"));
                permitTypeCode = reader.GetString(reader.GetOrdinal("PermitTypeSystemName"));
                updatedAtUtc = reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc"));
            }

            if (permitTypeCode != HotWorkPermitType)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new PermitApplicationUpdateResult(
                    PermitApplicationUpdateOutcome.UnsupportedPermitType,
                    Message: $"Editing permit type '{permitTypeCode}' is not supported yet.");
            }

            await using var extensionCommand = connection.CreateCommand();
            extensionCommand.Transaction = (SqlTransaction)transaction.GetDbTransaction();
            extensionCommand.CommandText = "dbo.SpPermitApplicationHotWorkUpd";
            extensionCommand.CommandType = CommandType.StoredProcedure;
            Add(extensionCommand, "@PermitApplicationId", SqlDbType.BigInt, permitApplicationId);
            AddSelections(extensionCommand, "@InspectionPriorToCommencement", request.InspectionPriorToCommencement);
            AddSelections(extensionCommand, "@WorksOnWall", request.WorksOnWall);
            AddSelections(extensionCommand, "@WorkingInConfinedSpace", request.WorkingInConfinedSpace);
            await extensionCommand.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new PermitApplicationUpdateResult(
                PermitApplicationUpdateOutcome.Success,
                new PermitApplicationUpdateResponseDto(savedId, statusId, statusCode, updatedAtUtc));
        }
        catch (SqlException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            PermitApplicationUpdateResult? result = exception.Number switch
            {
                50001 => new(PermitApplicationUpdateOutcome.NotFound),
                50002 => new(PermitApplicationUpdateOutcome.NotEditable, Message: exception.Message),
                50003 => new(PermitApplicationUpdateOutcome.UnsupportedPermitType, Message: exception.Message),
                50004 => new(PermitApplicationUpdateOutcome.InvalidSelections, Message: exception.Message),
                50005 => new(PermitApplicationUpdateOutcome.InvalidUsers, Message: exception.Message),
                50006 => new(PermitApplicationUpdateOutcome.StatusNotConfigured, Message: exception.Message),
                50007 => new(PermitApplicationUpdateOutcome.InvalidSelections, Message: exception.Message),
                _ => null
            };
            if (result is null) throw;
            return result;
        }
    }

    private Task<List<HotWorkSelection>> GetHotWorkSelectionsAsync(
        long permitApplicationId,
        CancellationToken cancellationToken) =>
        context.ListItems
            .AsNoTracking()
            .Where(x => x.ListItemCategory.Code == InspectionPriorToCommencementCategory
                || x.ListItemCategory.Code == WorksOnWallCategory
                || x.ListItemCategory.Code == WorkingInConfinedSpaceCategory)
            .OrderBy(x => x.ListItemCategory.Code)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .Select(x => new HotWorkSelection(
                x.ListItemCategory.Code,
                x.Id,
                x.Code,
                x.Name,
                x.Description,
                x.DisplayOrder,
                x.ListItemCategory.Code == InspectionPriorToCommencementCategory
                    ? context.PermitApplicationInspectionsPriorToComm.Any(selection =>
                        selection.PermitApplicationId == permitApplicationId
                        && selection.InspectionPriorToCommListItemId == x.Id
                        && selection.IsSelected)
                    : x.ListItemCategory.Code == WorksOnWallCategory
                        ? context.PermitApplicationWallWorks.Any(selection =>
                            selection.PermitApplicationId == permitApplicationId
                            && selection.WorksonWallListItemId == x.Id
                            && selection.IsSelected)
                        : context.PermitApplicationConfinedSpaces.Any(selection =>
                            selection.PermitApplicationId == permitApplicationId
                            && selection.WorkingInConfinedSpaceListItemId == x.Id
                            && selection.IsSelected)))
            .ToListAsync(cancellationToken);

    private static IReadOnlyList<PermitApplicationListItemSelectionDto> GetCategorySelections(
        IEnumerable<HotWorkSelection> selections,
        string categoryCode) =>
        selections
            .Where(x => x.CategoryCode == categoryCode)
            .Select(x => new PermitApplicationListItemSelectionDto(
                x.ListItemId,
                x.SystemName,
                x.Name,
                x.Description,
                x.DisplayOrder,
                x.IsSelected))
            .ToList();

    private async Task<string?> ValidateHotWorkSelectionsAsync(
        HashSet<int> inspections,
        HashSet<int> wallWorks,
        HashSet<int> confinedSpaces,
        CancellationToken cancellationToken)
    {
        var allIds = inspections.Concat(wallWorks).Concat(confinedSpaces).Distinct().ToList();
        if (allIds.Count == 0)
            return null;

        var categoriesByListItemId = await context.ListItems
            .AsNoTracking()
            .Where(x => allIds.Contains(x.Id))
            .Select(x => new { x.Id, CategoryCode = x.ListItemCategory.Code })
            .ToDictionaryAsync(x => x.Id, x => x.CategoryCode, cancellationToken);

        if (inspections.Any(id => !categoriesByListItemId.TryGetValue(id, out var category)
                || category != InspectionPriorToCommencementCategory))
            return $"Inspection selections must belong to {InspectionPriorToCommencementCategory}.";

        if (wallWorks.Any(id => !categoriesByListItemId.TryGetValue(id, out var category)
                || category != WorksOnWallCategory))
            return $"Works-on-wall selections must belong to {WorksOnWallCategory}.";

        if (confinedSpaces.Any(id => !categoriesByListItemId.TryGetValue(id, out var category)
                || category != WorkingInConfinedSpaceCategory))
            return $"Confined-space selections must belong to {WorkingInConfinedSpaceCategory}.";

        return null;
    }

    private void SynchronizeInspections(
        Apcloudpms.Domain.Entities.PermitApplication permitApplication,
        HashSet<int> selectedIds)
    {
        foreach (var existing in permitApplication.InspectionsPriorToComm.ToList())
        {
            if (selectedIds.Contains(existing.InspectionPriorToCommListItemId))
                existing.IsSelected = true;
            else
                context.PermitApplicationInspectionsPriorToComm.Remove(existing);
        }

        var existingIds = permitApplication.InspectionsPriorToComm
            .Select(x => x.InspectionPriorToCommListItemId).ToHashSet();
        foreach (var id in selectedIds.Except(existingIds))
        {
            permitApplication.InspectionsPriorToComm.Add(
                new Apcloudpms.Domain.Entities.PermitApplicationInspectionPriorToComm
                {
                    InspectionPriorToCommListItemId = id,
                    IsSelected = true
                });
        }
    }

    private void SynchronizeWallWorks(
        Apcloudpms.Domain.Entities.PermitApplication permitApplication,
        HashSet<int> selectedIds)
    {
        foreach (var existing in permitApplication.WallWorks.ToList())
        {
            if (selectedIds.Contains(existing.WorksonWallListItemId))
                existing.IsSelected = true;
            else
                context.PermitApplicationWallWorks.Remove(existing);
        }

        var existingIds = permitApplication.WallWorks.Select(x => x.WorksonWallListItemId).ToHashSet();
        foreach (var id in selectedIds.Except(existingIds))
        {
            permitApplication.WallWorks.Add(new Apcloudpms.Domain.Entities.PermitApplicationWallWorks
            {
                WorksonWallListItemId = id,
                IsSelected = true
            });
        }
    }

    private void SynchronizeConfinedSpaces(
        Apcloudpms.Domain.Entities.PermitApplication permitApplication,
        HashSet<int> selectedIds)
    {
        foreach (var existing in permitApplication.ConfinedSpaces.ToList())
        {
            if (selectedIds.Contains(existing.WorkingInConfinedSpaceListItemId))
                existing.IsSelected = true;
            else
                context.PermitApplicationConfinedSpaces.Remove(existing);
        }

        var existingIds = permitApplication.ConfinedSpaces
            .Select(x => x.WorkingInConfinedSpaceListItemId).ToHashSet();
        foreach (var id in selectedIds.Except(existingIds))
        {
            permitApplication.ConfinedSpaces.Add(
                new Apcloudpms.Domain.Entities.PermitApplicationConfinedSpace
                {
                    WorkingInConfinedSpaceListItemId = id,
                    IsSelected = true
                });
        }
    }

    private static HashSet<int> GetSelectedIds(
        IEnumerable<PermitApplicationUpdateSelectionDto>? selections) =>
        selections?.Where(x => x.IsSelected).Select(x => x.ListItemId).ToHashSet() ?? [];

    private static bool HasDuplicateIds(
        IEnumerable<PermitApplicationUpdateSelectionDto>? selections)
    {
        if (selections is null)
            return false;

        var ids = new HashSet<int>();
        return selections.Any(x => !ids.Add(x.ListItemId));
    }

    private sealed record HotWorkSelection(
        string CategoryCode,
        int ListItemId,
        string SystemName,
        string Name,
        string? Description,
        int DisplayOrder,
        bool IsSelected);

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
        IEnumerable<PermitApplicationUpdateSelectionDto>? selections)
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

    private static async Task<IReadOnlyList<PermitApplicationListItemSelectionDto>>
        ReadPreviewSelectionsAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        if (!await reader.NextResultAsync(cancellationToken))
            throw new InvalidOperationException("The permit preview procedure returned incomplete selection data.");

        var result = new List<PermitApplicationListItemSelectionDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new PermitApplicationListItemSelectionDto(
                reader.GetInt32(reader.GetOrdinal("ListItemId")),
                reader.GetString(reader.GetOrdinal("SystemName")),
                reader.GetString(reader.GetOrdinal("Name")),
                GetNullableString(reader, "Description"),
                reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
                reader.GetBoolean(reader.GetOrdinal("IsSelected"))));
        }
        return result;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? GetNullableInt32(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
