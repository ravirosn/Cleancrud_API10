using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed record ApplicationModuleDto(int Id, string Code, string Name,
    string? Description, string? Icon, int DisplayOrder, bool IsActive);

public sealed class ApplicationModuleRequestDto
{
    [Required, StringLength(30, MinimumLength = 2)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    [StringLength(100)] public string? Icon { get; set; }
    [Range(0, int.MaxValue)] public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record ModuleMenuDto(int Id, int ApplicationModuleId, int? ParentMenuId,
    string Name, string ControllerName, string ActionName, string QueryUrl,
    string? Icon, int DisplayOrder, bool IsActive);

public sealed class ModuleMenuRequestDto
{
    public int? ParentMenuId { get; set; }
    [Required, StringLength(100, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(100)] public string ControllerName { get; set; } = string.Empty;
    [Required, StringLength(100)] public string ActionName { get; set; } = string.Empty;
    [Required, StringLength(500)] public string QueryUrl { get; set; } = string.Empty;
    [StringLength(100)] public string? Icon { get; set; }
    [Range(0, int.MaxValue)] public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UserModuleAssignmentDto
{
    [Range(1, int.MaxValue)] public int UserId { get; set; }
    [Range(1, int.MaxValue)] public int ApplicationModuleId { get; set; }
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
    public string ControllerName { get; init; } = string.Empty;
    public string ActionName { get; init; } = string.Empty;
    public string QueryUrl { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int DisplayOrder { get; init; }
    public List<NavigationMenuDto> Children { get; init; } = [];
}

public sealed record ModuleNavigationDto(ApplicationModuleDto Module,
    IReadOnlyList<NavigationMenuDto> Navigation);
