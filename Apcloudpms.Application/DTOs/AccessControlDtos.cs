using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed record RoleDto(
    int Id, string Name, bool IsActive, IReadOnlyList<int> ModuleIds);

public sealed record RoleModuleOptionDto(
    int Id, string Code, string Name, int DisplayOrder, bool IsAssigned);

public sealed class RoleQueryDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    [StringLength(100)]
    public string? SearchTerm { get; set; }

    public bool IncludeInactive { get; set; } = true;
}

public sealed record RoleGridItemDto(
    int Id,
    string Name,
    bool IsActive,
    string Status,
    DateTime CreatedAtUtc);

public sealed record RolePagedResponseDto(
    IReadOnlyList<RoleGridItemDto> Data,
    long TotalRecords,
    long TotalPages,
    int PageNumber,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed class RoleRequestDto
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public IReadOnlyList<int>? ModuleIds { get; set; }
}

public sealed class UserRoleAssignmentDto
{
    [Range(1, int.MaxValue)] public int UserId { get; set; }
    [Range(1, int.MaxValue)] public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
}
