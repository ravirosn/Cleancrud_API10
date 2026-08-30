namespace Apcloudpms.Domain.Entities;

public sealed class RoleModule
{
    public int RoleId { get; set; }
    public int ApplicationModuleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public Role Role { get; set; } = null!;
    public ApplicationModule ApplicationModule { get; set; } = null!;
    public ICollection<RoleModuleMenu> RoleModuleMenus { get; set; } = new List<RoleModuleMenu>();
}
