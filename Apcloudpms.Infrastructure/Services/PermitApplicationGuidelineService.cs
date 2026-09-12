using System.Net;
using System.Text.RegularExpressions;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Domain.Entities;
using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Apcloudpms.Infrastructure.Services;

public sealed class PermitApplicationGuidelineService(
    AppDbContext context,
    IAuditContext auditContext) : IPermitApplicationGuidelineService
{
    private const string PermitTypeCategory = "PERMIT_TYPE";
    private static readonly Regex HtmlTags = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex UnsafeHtml = new(
        @"<\s*(script|style|iframe|object|embed|form|input|button|svg|math)\b|\bon[a-z]+\s*=|\b(?:href|src)\s*=\s*(['\""']?)\s*(?:javascript|data):",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<PermitApplicationGuidelinePagedResponseDto> GetPagedAsync(
        PermitApplicationGuidelineQueryDto query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var source = context.PermitApplicationGuidelines.AsNoTracking();
        if (!query.IncludeInactive) source = source.Where(x => x.IsActive);
        var search = Normalize(query.SearchTerm);
        if (search is not null)
            source = source.Where(x => x.PermitTypeListItem.Name.Contains(search)
                || x.PermitTypeListItem.Code.Contains(search));

        var descending = query.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        source = query.SortBy.ToLowerInvariant() switch
        {
            "status" => descending
                ? source.OrderByDescending(x => x.IsActive).ThenByDescending(x => x.Id)
                : source.OrderBy(x => x.IsActive).ThenBy(x => x.Id),
            "createdatutc" => descending
                ? source.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
                : source.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),
            "updatedatutc" => descending
                ? source.OrderByDescending(x => x.UpdatedAtUtc).ThenByDescending(x => x.Id)
                : source.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Id),
            _ => descending
                ? source.OrderByDescending(x => x.PermitTypeListItem.Name).ThenByDescending(x => x.Id)
                : source.OrderBy(x => x.PermitTypeListItem.Name).ThenBy(x => x.Id)
        };

        var totalRecords = await source.LongCountAsync(cancellationToken);
        var data = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new PermitApplicationGuidelineGridDto(
                x.Id, x.PermitTypeListItemId, x.PermitTypeListItem.Code,
                x.PermitTypeListItem.Name, x.IsActive, x.IsActive ? "Active" : "Inactive",
                x.CreatedAtUtc, x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
        var totalPages = totalRecords == 0 ? 0 : (totalRecords + query.PageSize - 1) / query.PageSize;
        return new(data, totalRecords, totalPages, query.PageNumber, query.PageSize,
            query.PageNumber > 1, query.PageNumber < totalPages);
    }

    public async Task<IReadOnlyList<PermitTypeGuidelineOptionDto>> GetPermitTypesAsync(
        CancellationToken cancellationToken = default) =>
        await context.ListItems.AsNoTracking()
            .Where(x => x.IsActive && x.ListItemCategory.IsActive
                && x.ListItemCategory.Code == PermitTypeCategory)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new PermitTypeGuidelineOptionDto(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);

    public Task<PermitApplicationGuidelineDto?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default) => ReadAsync(id, cancellationToken);

    public async Task<PermitApplicationGuidelineDto> CreateAsync(
        PermitApplicationGuidelineRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        await ValidatePermitTypeAndUniquenessAsync(null, request, cancellationToken);
        var entity = new PermitApplicationGuideline
        {
            PermitTypeListItemId = request.PermitTypeListItemId,
            Guidelines = request.Guidelines.Trim(),
            IsActive = request.IsActive,
            CreatedByUserId = auditContext.UserId,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.PermitApplicationGuidelines.Add(entity);
        await SaveAsync(cancellationToken);
        return (await ReadAsync(entity.Id, cancellationToken))!;
    }

    public async Task<PermitApplicationGuidelineDto?> UpdateAsync(
        int id, PermitApplicationGuidelineRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        var entity = await context.PermitApplicationGuidelines.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        await ValidatePermitTypeAndUniquenessAsync(id, request, cancellationToken);
        entity.PermitTypeListItemId = request.PermitTypeListItemId;
        entity.Guidelines = request.Guidelines.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedByUserId = auditContext.UserId;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await SaveAsync(cancellationToken);
        return await ReadAsync(id, cancellationToken);
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.PermitApplicationGuidelines.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;
        if (entity.IsActive)
        {
            entity.IsActive = false;
            entity.UpdatedByUserId = auditContext.UserId;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
        return true;
    }

    private async Task ValidatePermitTypeAndUniquenessAsync(
        int? id, PermitApplicationGuidelineRequestDto request, CancellationToken cancellationToken)
    {
        var isPermitType = await context.ListItems.AnyAsync(x =>
            x.Id == request.PermitTypeListItemId && x.IsActive && x.ListItemCategory.IsActive
            && x.ListItemCategory.Code == PermitTypeCategory, cancellationToken);
        if (!isPermitType) throw new ArgumentException("Select an active PERMIT_TYPE list item.");
        if (request.IsActive && await context.PermitApplicationGuidelines.AnyAsync(x =>
            x.PermitTypeListItemId == request.PermitTypeListItemId && x.IsActive
            && (!id.HasValue || x.Id != id.Value), cancellationToken))
            throw new ArgumentException("An active guideline already exists for this permit type.");
    }

    private Task<PermitApplicationGuidelineDto?> ReadAsync(int id, CancellationToken cancellationToken) =>
        context.PermitApplicationGuidelines.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new PermitApplicationGuidelineDto(
                x.Id, x.PermitTypeListItemId, x.PermitTypeListItem.Code, x.PermitTypeListItem.Name,
                x.Guidelines, x.IsActive, x.CreatedByUserId,
                x.CreatedByUser == null ? null : x.CreatedByUser.DisplayName ?? x.CreatedByUser.UserName,
                x.CreatedAtUtc, x.UpdatedByUserId,
                x.UpdatedByUser == null ? null : x.UpdatedByUser.DisplayName ?? x.UpdatedByUser.UserName,
                x.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException?.Message.Contains(
            "UX_PermitApplicationGuidelines_ActivePermitType", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new ArgumentException("An active guideline already exists for this permit type.", exception);
        }
    }

    private static void Validate(PermitApplicationGuidelineRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Guidelines)
            || string.IsNullOrWhiteSpace(WebUtility.HtmlDecode(HtmlTags.Replace(request.Guidelines, string.Empty))))
            throw new ArgumentException("Guidelines content is required.");
        if (request.Guidelines.Length > 200000)
            throw new ArgumentException("Guidelines content cannot exceed 200,000 characters.");
        if (UnsafeHtml.IsMatch(request.Guidelines))
            throw new ArgumentException("Guidelines contain an unsafe HTML element or attribute.");
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
