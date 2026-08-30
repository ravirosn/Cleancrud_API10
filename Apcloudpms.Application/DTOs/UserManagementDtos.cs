using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed class UserManagementQueryDto
{
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
    [StringLength(200)] public string? SearchTerm { get; set; }
    [StringLength(30)] public string SortBy { get; set; } = "createdAtUtc";
    [RegularExpression("^(?i:asc|desc)$")] public string SortDirection { get; set; } = "desc";
    public bool IncludeInactive { get; set; }
}

public sealed record UserManagementGridItemDto(
    int Id,
    string UserName,
    string? DisplayName,
    string? Email,
    string? ContactNumber,
    int? OfficeBranchId,
    string? OfficeBranchName,
    int? DepartmentId,
    string? DepartmentName,
    bool IsActive,
    string Status,
    DateTime CreatedAtUtc,
    int? CreatedByUserId,
    string? CreatedBy,
    DateTime? ModifiedAtUtc,
    int? ModifiedByUserId,
    string? ModifiedBy);

public sealed record UserManagementPagedResponseDto(
    IReadOnlyList<UserManagementGridItemDto> Data,
    long TotalRecords,
    long TotalPages,
    int PageNumber,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed class UserCreateRequestDto
{
    [Required, StringLength(100, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._@-]+$", ErrorMessage = "Username contains unsupported characters.")]
    public string UserName { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{12,128}$",
        ErrorMessage = "Password must include uppercase, lowercase, number, and special characters.")]
    public string Password { get; set; } = string.Empty;

    [StringLength(200)] public string? DisplayName { get; set; }
    [EmailAddress, StringLength(320)] public string? Email { get; set; }
    [Phone, StringLength(20)] public string? ContactNumber { get; set; }
    [Range(1, int.MaxValue)] public int? OfficeBranchId { get; set; }
    [Range(1, int.MaxValue)] public int? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UserUpdateRequestDto
{
    [Required, StringLength(100, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._@-]+$", ErrorMessage = "Username contains unsupported characters.")]
    public string UserName { get; set; } = string.Empty;

    [StringLength(128, MinimumLength = 12)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{12,128}$",
        ErrorMessage = "Password must include uppercase, lowercase, number, and special characters.")]
    public string? Password { get; set; }
    [StringLength(200)] public string? DisplayName { get; set; }
    [EmailAddress, StringLength(320)] public string? Email { get; set; }
    [Phone, StringLength(20)] public string? ContactNumber { get; set; }
    [Range(1, int.MaxValue)] public int? OfficeBranchId { get; set; }
    [Range(1, int.MaxValue)] public int? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record UserManagementDto(
    int Id, string UserName, string? DisplayName, string? Email,
    string? ContactNumber, int? OfficeBranchId, string? OfficeBranchName,
    int? DepartmentId, string? DepartmentName, bool IsActive,
    DateTime CreatedAtUtc, int? CreatedByUserId, DateTime? ModifiedAtUtc,
    int? ModifiedByUserId);

public sealed record UserRoleOptionDto(int RoleId, string RoleName, bool IsAssigned);

public sealed record UserRoleConfigurationDto(
    int UserId, string UserName, IReadOnlyList<UserRoleOptionDto> Roles);

public sealed class UserRolesUpdateRequestDto
{
    [Required] public IReadOnlyList<int> RoleIds { get; set; } = [];
}
