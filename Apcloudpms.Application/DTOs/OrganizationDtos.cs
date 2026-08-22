using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed record OfficeBranchDto(int Id, string Code, string Name, string? Address,
    bool IsHeadOffice, bool IsActive);

public sealed class OfficeBranchRequestDto
{
    [Required, StringLength(20)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Address { get; set; }
    public bool IsHeadOffice { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record DepartmentDto(int Id, int OfficeBranchId, string BranchName,
    string Code, string Name, bool IsActive);

public sealed class DepartmentRequestDto
{
    [Range(1, int.MaxValue)] public int OfficeBranchId { get; set; }
    [Required, StringLength(20)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
