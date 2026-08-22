using System.Data;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
                x.PermitIssuerName,
                x.PermitIssuerContactNumber,
                x.PermitReceiverName,
                x.PermitReceiverContactNumber,
                x.PreRiskAssessmentNumber,
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
            permitApplication.PermitIssuerName,
            permitApplication.PermitIssuerContactNumber,
            permitApplication.PermitReceiverName,
            permitApplication.PermitReceiverContactNumber,
            permitApplication.PreRiskAssessmentNumber,
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
                    reader.GetString(reader.GetOrdinal("PermitIssuerName")),
                    reader.GetString(reader.GetOrdinal("PermitReceiverName")),
                    reader.GetInt32(reader.GetOrdinal("PermitTypeListItemId")),
                    reader.GetString(reader.GetOrdinal("PermitTypeName")),
                    reader.GetInt32(reader.GetOrdinal("PermitStatusListItemId")),
                    reader.GetString(reader.GetOrdinal("PermitStatusName")),
                    GetNullableDateTime(reader, "SubmittedAtUtc"),
                    GetNullableInt32(reader, "CreatedByUserId"),
                    reader.GetString(reader.GetOrdinal("CreatedByUserName")),
                    GetNullableString(reader, "PreRiskAssessmentNumber"),
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

        var permitApplication = await context.PermitApplications
            .Include(x => x.PermitTypeListItem)
            .Include(x => x.PermitStatusListItem)
            .Include(x => x.InspectionsPriorToComm)
            .Include(x => x.WallWorks)
            .Include(x => x.ConfinedSpaces)
            .SingleOrDefaultAsync(x => x.Id == permitApplicationId, cancellationToken);

        if (permitApplication is null)
            return new PermitApplicationUpdateResult(PermitApplicationUpdateOutcome.NotFound);

        if (finalizeForApproval
            && permitApplication.PermitStatusListItem.Code != PermitDraftStatus)
        {
            return new PermitApplicationUpdateResult(
                PermitApplicationUpdateOutcome.NotEditable,
                Message: "Only a permit application in PERMIT_DRAFT status can be finalized for approval.");
        }

        if (!finalizeForApproval
            && permitApplication.PermitStatusListItem.Code is not PermitDraftStatus
                and not PermitRejectedStatus)
        {
            return new PermitApplicationUpdateResult(
                PermitApplicationUpdateOutcome.NotEditable,
                Message: "This permit application cannot be edited because its status is not Draft or Rejected.");
        }

        if (permitApplication.PermitTypeListItem.Code != HotWorkPermitType)
        {
            return new PermitApplicationUpdateResult(
                PermitApplicationUpdateOutcome.UnsupportedPermitType,
                Message: $"Editing permit type '{permitApplication.PermitTypeListItem.Name}' is not supported yet.");
        }

        if (request.IssueDate == default)
        {
            return new PermitApplicationUpdateResult(
                PermitApplicationUpdateOutcome.InvalidSelections,
                Message: "IssueDate is required.");
        }

        var inspections = GetSelectedIds(request.InspectionPriorToCommencement);
        var wallWorks = GetSelectedIds(request.WorksOnWall);
        var confinedSpaces = GetSelectedIds(request.WorkingInConfinedSpace);

        if (HasDuplicateIds(request.InspectionPriorToCommencement)
            || HasDuplicateIds(request.WorksOnWall)
            || HasDuplicateIds(request.WorkingInConfinedSpace))
        {
            return new PermitApplicationUpdateResult(
                PermitApplicationUpdateOutcome.InvalidSelections,
                Message: "A list item may only appear once in each selection collection.");
        }

        var selectionValidation = await ValidateHotWorkSelectionsAsync(
            inspections, wallWorks, confinedSpaces, cancellationToken);
        if (selectionValidation is not null)
        {
            return new PermitApplicationUpdateResult(
                PermitApplicationUpdateOutcome.InvalidSelections,
                Message: selectionValidation);
        }

        int? finalizedStatusId = null;
        if (finalizeForApproval)
        {
            finalizedStatusId = await context.ListItems
                .Where(x => x.IsActive
                    && x.Code == FinalizedForApprovalStatus
                    && x.ListItemCategory.Code == PermitStatusCategory)
                .Select(x => (int?)x.Id)
                .SingleOrDefaultAsync(cancellationToken);
            if (!finalizedStatusId.HasValue)
            {
                return new PermitApplicationUpdateResult(
                    PermitApplicationUpdateOutcome.StatusNotConfigured,
                    Message: "The active FINALIZED_FOR_APPROVAL permit status is not configured.");
            }
        }

        permitApplication.IssueDate = request.IssueDate;
        permitApplication.PermitIssuerName = request.PermitIssuerName.Trim();
        permitApplication.PermitIssuerContactNumber = Normalize(request.PermitIssuerContactNumber);
        permitApplication.PermitReceiverName = request.PermitReceiverName.Trim();
        permitApplication.PermitReceiverContactNumber = Normalize(request.PermitReceiverContactNumber);
        permitApplication.PreRiskAssessmentNumber = Normalize(request.PreRiskAssessmentNumber);
        permitApplication.WorkLocation = request.WorkLocation.Trim();
        permitApplication.WorkDescription = request.WorkDescription.Trim();
        permitApplication.SpecialInstructions = Normalize(request.SpecialInstructions);
        permitApplication.WorkHeightBelowSurface = Normalize(request.WorkHeightBelowSurface);
        permitApplication.CompletionOfWorks = Normalize(request.CompletionOfWorks);

        SynchronizeInspections(permitApplication, inspections);
        SynchronizeWallWorks(permitApplication, wallWorks);
        SynchronizeConfinedSpaces(permitApplication, confinedSpaces);

        if (finalizedStatusId.HasValue)
            permitApplication.PermitStatusListItemId = finalizedStatusId.Value;

        var updatedAtUtc = DateTime.UtcNow;
        permitApplication.UpdatedByUserId = userId;
        permitApplication.UpdatedAtUtc = updatedAtUtc;
        await context.SaveChangesAsync(cancellationToken);

        return new PermitApplicationUpdateResult(
            PermitApplicationUpdateOutcome.Success,
            new PermitApplicationUpdateResponseDto(
                permitApplication.Id,
                permitApplication.PermitStatusListItemId,
                finalizeForApproval
                    ? FinalizedForApprovalStatus
                    : permitApplication.PermitStatusListItem.Code,
                updatedAtUtc));
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
