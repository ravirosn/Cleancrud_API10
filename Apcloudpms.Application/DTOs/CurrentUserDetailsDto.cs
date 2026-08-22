namespace Apcloudpms.Application.DTOs;

public sealed record CurrentUserDetailsDto(
    int Id,
    string UserName,
    string? DisplayName,
    string? Email,
    string? ContactNumber,
    Guid? EntraTenantId,
    Guid? EntraObjectId,
    bool IsActive,
    DateTime CreatedAtUtc,
    IReadOnlyList<string> Roles,
    int? DepartmentId,
    string? DepartmentName,
    int? BranchId,
    string? BranchName);
