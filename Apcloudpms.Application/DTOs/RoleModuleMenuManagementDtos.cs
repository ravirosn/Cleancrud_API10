using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed class RoleModuleMenuQueryDto
{
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
    [StringLength(200)] public string? SearchTerm { get; set; }
    [StringLength(30)] public string SortBy { get; set; } = "assignedAtUtc";
    [RegularExpression("^(?i:asc|desc)$")] public string SortDirection { get; set; } = "desc";
    [Range(1, int.MaxValue)] public int? RoleId { get; set; }
    [Range(1, int.MaxValue)] public int? ApplicationModuleId { get; set; }
    public bool IncludeInactive { get; set; }
}

public sealed record RoleModuleMenuGridDto(
    string Id, int RoleId, string RoleName, int ApplicationModuleId,
    string ModuleName, int ModuleMenuId, int? ParentMenuId,
    string MenuName, string MenuHierarchy, int DisplayOrder,
    bool IsActive, string Status, DateTime AssignedAtUtc,
    string? AssignedBy, DateTime? ModifiedAtUtc, string? ModifiedBy);

public sealed record RoleModuleMenuPagedResponseDto(
    IReadOnlyList<RoleModuleMenuGridDto> Data, long TotalRecords,
    long TotalPages, int PageNumber, int PageSize,
    bool HasPreviousPage, bool HasNextPage);

public sealed class RoleModuleMenuManagementRequestDto
{
    [Range(1, int.MaxValue)] public int RoleId { get; set; }
    [Range(1, int.MaxValue)] public int ApplicationModuleId { get; set; }
    [Range(1, int.MaxValue)] public int ModuleMenuId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record RoleModuleMenuManagementDto(
    int RoleId, string RoleName, int ApplicationModuleId, string ModuleName,
    int ModuleMenuId, int? ParentMenuId, string MenuName,
    string MenuHierarchy, int DisplayOrder, bool IsActive,
    DateTime AssignedAtUtc, string? AssignedBy,
    DateTime? ModifiedAtUtc, string? ModifiedBy);

public sealed record RoleModuleMenuRoleOptionDto(int Id, string Name);
public sealed record RoleModuleMenuModuleOptionDto(int Id, string Code, string Name);
public sealed record RoleModuleMenuMenuOptionDto(
    int Id, int? ParentMenuId, string Name, string Hierarchy,
    int Depth, int DisplayOrder, bool IsAssigned, bool CanAssign);
