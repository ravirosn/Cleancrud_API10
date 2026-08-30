namespace Apcloudpms.Domain.Entities;

public sealed class RoleModuleMenu
{
    public int RoleId { get; set; }
    public int ApplicationModuleId { get; set; }
    public int ModuleMenuId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public RoleModule RoleModule { get; set; } = null!;
    public ModuleMenu ModuleMenu { get; set; } = null!;
}
