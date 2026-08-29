namespace Apcloud.Web.Areas.Portal.Models;

public sealed class ModuleDashboardViewModel
{
    public IReadOnlyList<AssignedModuleViewModel> Modules { get; init; } = [];

    public string? ErrorMessage { get; init; }
}

public sealed class AssignedModuleViewModel
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Code { get; init; }

    public string? Description { get; init; }

    public string? Icon { get; init; }

    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; }
}

public sealed class ModuleMenusViewModel
{
    public required string ModuleId { get; init; }

    public string ModuleName { get; init; } = "Module";

    public IReadOnlyList<NavigationMenuViewModel> Menus { get; init; } = [];

    public string? ErrorMessage { get; init; }
}

public sealed class NavigationMenuViewModel
{
    public required string Id { get; init; }

    public string? ParentId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public string? Icon { get; init; }

    public string? Url { get; set; }

    public bool IsCurrent { get; set; }

    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; }

    public List<NavigationMenuViewModel> Children { get; } = [];
}

public sealed class ModuleSidebarViewModel
{
    public AssignedModuleViewModel? Module { get; init; }

    public IReadOnlyList<NavigationMenuViewModel> Menus { get; init; } = [];

    public string? ErrorMessage { get; init; }
}
