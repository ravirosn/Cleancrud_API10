using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed record ApplicationModuleDto(int Id, string Code, string Name,
    string? Description, string? Icon, int DisplayOrder, bool IsActive);

public sealed class ApplicationModuleQueryDto
{
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
    [StringLength(200)] public string? SearchTerm { get; set; }
    public bool IncludeInactive { get; set; } = true;
}

public sealed record ApplicationModuleGridItemDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    string? Icon,
    int DisplayOrder,
    bool IsActive,
    string Status,
    DateTime CreatedAtUtc);

public sealed record ApplicationModulePagedResponseDto(
    IReadOnlyList<ApplicationModuleGridItemDto> Data,
    long TotalRecords,
    long TotalPages,
    int PageNumber,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed class ApplicationModuleRequestDto
{
    [Required, StringLength(30, MinimumLength = 2)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    [StringLength(100)] public string? Icon { get; set; }
    [Range(0, int.MaxValue)] public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record RoleModuleDto(int RoleId, string RoleName, bool IsActive);

public sealed class RoleModuleAssignmentRequestDto
{
    [Required]
    public IReadOnlyList<int> RoleIds { get; set; } = [];
}

public sealed class ApplicationModuleConfigurationRequestDto
{
    [Required]
    public ApplicationModuleRequestDto Module { get; set; } = new();

    [Required]
    public IReadOnlyList<int> RoleIds { get; set; } = [];
}

public sealed record ApplicationModuleConfigurationDto(
    ApplicationModuleDto Module,
    IReadOnlyList<RoleModuleDto> Roles);

public sealed class RoleModuleMenuAssignmentRequestDto
{
    [Required]
    public IReadOnlyList<int> MenuIds { get; set; } = [];
}

public sealed record RoleModuleMenuConfigurationDto(
    int RoleId,
    string RoleName,
    int ApplicationModuleId,
    string ModuleName,
    IReadOnlyList<ModuleMenuDto> Menus);

public sealed record ModuleMenuDto(int Id, int ApplicationModuleId, int? ParentMenuId,
    string Name, string? ControllerName, string? ActionName, string? QueryUrl,
    string? Icon, int DisplayOrder, bool IsActive)
{
    public IReadOnlyList<ModuleMenuDto> Children { get; init; } = [];
}

public sealed class ModuleMenuRequestDto
{
    public int? ParentMenuId { get; set; }
    [Required, StringLength(100, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [StringLength(100)] public string? ControllerName { get; set; }
    [StringLength(100)] public string? ActionName { get; set; }
    [StringLength(500)] public string? QueryUrl { get; set; }
    [StringLength(100)] public string? Icon { get; set; }
    [Range(0, int.MaxValue)] public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ModuleSelectionRequestDto
{
    [Range(1, int.MaxValue)] public int ApplicationModuleId { get; set; }
}

public sealed class NavigationMenuDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ControllerName { get; init; }
    public string? ActionName { get; init; }
    public string? QueryUrl { get; init; }
    public string? Icon { get; init; }
    public int DisplayOrder { get; init; }
    public List<NavigationMenuDto> Children { get; init; } = [];
}

public sealed record ModuleNavigationDto(ApplicationModuleDto Module,
    IReadOnlyList<NavigationMenuDto> Navigation);
