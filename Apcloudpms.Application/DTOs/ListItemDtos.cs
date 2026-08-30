using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed record ListItemDto(
    int Id,
    int ListItemCategoryId,
    string Code,
    string Name,
    string? Description,
    int DisplayOrder);

public class ListItemManagementQueryDto
{
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
    [StringLength(200)] public string? SearchTerm { get; set; }
    [StringLength(30)] public string SortBy { get; set; } = "createdAtUtc";
    [RegularExpression("^(?i:asc|desc)$")] public string SortDirection { get; set; } = "desc";
    public bool IncludeInactive { get; set; }
}

public sealed class ListItemQueryDto : ListItemManagementQueryDto
{
    [Range(1, int.MaxValue)] public int? ListItemCategoryId { get; set; }
}

public sealed record ListItemCategoryGridDto(
    int Id, string Code, string Name, string? Description,
    bool IsActive, string Status, int ItemCount,
    DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

public sealed record ListItemGridDto(
    int Id, int ListItemCategoryId, string CategoryName,
    string Code, string Name, string? Description, int DisplayOrder,
    bool IsActive, string Status, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

public sealed record ListItemManagementPagedResponseDto<T>(
    IReadOnlyList<T> Data, long TotalRecords, long TotalPages,
    int PageNumber, int PageSize, bool HasPreviousPage, bool HasNextPage);

public sealed class ListItemCategoryRequestDto
{
    [Required, StringLength(50, MinimumLength = 2)]
    [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Code may contain letters, numbers, underscores, and hyphens only.")]
    public string Code { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ListItemRequestDto
{
    [Range(1, int.MaxValue)] public int ListItemCategoryId { get; set; }
    [Required, StringLength(50, MinimumLength = 2)]
    [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Code may contain letters, numbers, underscores, and hyphens only.")]
    public string Code { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    [Range(0, int.MaxValue)] public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record ListItemCategoryManagementDto(
    int Id, string Code, string Name, string? Description,
    bool IsActive, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

public sealed record ListItemManagementDto(
    int Id, int ListItemCategoryId, string CategoryName,
    string Code, string Name, string? Description, int DisplayOrder,
    bool IsActive, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

public sealed record ListItemCategoryOptionDto(int Id, string Code, string Name);
