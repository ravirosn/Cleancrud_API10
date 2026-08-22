using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed record RoleDto(int Id, string Name, bool IsActive);

public sealed class RoleRequestDto
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class UserRoleAssignmentDto
{
    [Range(1, int.MaxValue)] public int UserId { get; set; }
    [Range(1, int.MaxValue)] public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
}
