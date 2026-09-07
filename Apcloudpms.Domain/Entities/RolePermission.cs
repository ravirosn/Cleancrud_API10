namespace Apcloudpms.Domain.Entities;

public sealed class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionPolicyId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public int? AssignedByUserId { get; set; }
    public string? AssignedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public int? ModifiedByUserId { get; set; }
    public string? ModifiedBy { get; set; }

    public Role Role { get; set; } = null!;
    public PermissionPolicy PermissionPolicy { get; set; } = null!;
}
