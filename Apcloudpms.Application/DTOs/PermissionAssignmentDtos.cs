using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed class PermissionAssignmentQueryDto
{
    [Range(1,int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1,100)] public int PageSize { get; set; } = 10;
    [StringLength(200)] public string? SearchTerm { get; set; }
    [StringLength(30)] public string SortBy { get; set; } = "assignedAtUtc";
    [RegularExpression("^(?i:asc|desc)$")] public string SortDirection { get; set; } = "desc";
    public bool IncludeInactive { get; set; }
}

public sealed record PermissionAssignmentDto(
    string Id, int RoleId, string RoleName, int PermissionPolicyId,
    string PermissionCode, string PermissionName, string ModuleCode,
    string MenuController, string MenuAction, bool IsActive, string Status,
    DateTime AssignedAtUtc, string? AssignedBy, DateTime? ModifiedAtUtc, string? ModifiedBy);

public sealed record PermissionAssignmentPagedResponseDto(
    IReadOnlyList<PermissionAssignmentDto> Data, long TotalRecords, long TotalPages,
    int PageNumber, int PageSize, bool HasPreviousPage, bool HasNextPage);

public sealed class PermissionAssignmentRequestDto
{
    [Range(1,int.MaxValue)] public int RoleId { get; set; }
    [Range(1,int.MaxValue)] public int PermissionPolicyId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record PermissionRoleOptionDto(int Id, string Name);
public sealed record PermissionPolicyOptionDto(int Id, string Code, string Name, string ModuleCode, string MenuName, bool IsAssigned, bool CanAssign);
public sealed record CurrentPermissionDto(string Code);
