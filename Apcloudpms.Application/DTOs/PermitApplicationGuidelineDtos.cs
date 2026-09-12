using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed class PermitApplicationGuidelineQueryDto
{
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
    [StringLength(200)] public string? SearchTerm { get; set; }
    public bool IncludeInactive { get; set; } = true;
    [RegularExpression("^(?i:permitTypeName|status|createdAtUtc|updatedAtUtc)$")]
    public string SortBy { get; set; } = "permitTypeName";
    [RegularExpression("^(?i:asc|desc)$")] public string SortDirection { get; set; } = "asc";
}

public sealed record PermitApplicationGuidelineGridDto(
    int Id,
    int PermitTypeListItemId,
    string PermitTypeSystemName,
    string PermitTypeName,
    bool IsActive,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record PermitApplicationGuidelineDto(
    int Id,
    int PermitTypeListItemId,
    string PermitTypeSystemName,
    string PermitTypeName,
    string Guidelines,
    bool IsActive,
    int? CreatedByUserId,
    string? CreatedByUserName,
    DateTime CreatedAtUtc,
    int? UpdatedByUserId,
    string? UpdatedByUserName,
    DateTime? UpdatedAtUtc);

public sealed class PermitApplicationGuidelineRequestDto
{
    [Range(1, int.MaxValue)] public int PermitTypeListItemId { get; set; }
    [Required, StringLength(200000)] public string Guidelines { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed record PermitApplicationGuidelinePagedResponseDto(
    IReadOnlyList<PermitApplicationGuidelineGridDto> Data,
    long TotalRecords,
    long TotalPages,
    int PageNumber,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record PermitTypeGuidelineOptionDto(int Id, string Code, string Name);
