using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed record OrganizationDetailsDto(
    int Id,
    string Code,
    string Name,
    string Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed class OrganizationUpdateRequestDto
{
    [Required, StringLength(20)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(500)] public string Address { get; set; } = string.Empty;
    [Phone, StringLength(30)] public string? PhoneNumber { get; set; }
    [EmailAddress, StringLength(320)] public string? Email { get; set; }
    [Url, StringLength(500)] public string? Website { get; set; }
}

public sealed record OfficeBranchDto(int Id, int OrganizationId, string OrganizationName,
    string Code, string Name, string? Address, bool IsHeadOffice, bool IsActive)
{
    public string HeadOffice => IsHeadOffice ? "Yes" : "No";
    public string Status => IsActive ? "Active" : "Inactive";
}

public class OrganizationQueryDto
{
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
    [StringLength(200)] public string? Search { get; set; }
    public bool IncludeInactive { get; set; }
    [StringLength(32)] public string SortBy { get; set; } = "name";
    [RegularExpression("^(asc|desc)$", ErrorMessage = "SortDirection must be asc or desc.")]
    public string SortDirection { get; set; } = "asc";
}

public sealed class DepartmentQueryDto : OrganizationQueryDto
{
    [Range(1, int.MaxValue)] public int? OfficeBranchId { get; set; }
}

public sealed record OrganizationPagedResponseDto<T>(
    IReadOnlyList<T> Data,
    long TotalRecords,
    long TotalPages,
    int PageNumber,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record DropdownItemDto(int Id, string Code, string Name);

public sealed class OfficeBranchRequestDto
{
    [Range(1, int.MaxValue)] public int OrganizationId { get; set; }
    [Required, StringLength(20)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Address { get; set; }
    public bool IsHeadOffice { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record DepartmentDto(int Id, int OfficeBranchId, string BranchName,
    string Code, string Name, bool IsActive)
{
    public string Status => IsActive ? "Active" : "Inactive";
}

public sealed class DepartmentRequestDto
{
    [Range(1, int.MaxValue)] public int OfficeBranchId { get; set; }
    [Required, StringLength(20)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
