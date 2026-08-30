namespace Apcloudpms.Domain.Entities;

public class ApplicationModule
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ModuleMenu> Menus { get; set; } = new List<ModuleMenu>();
    public ICollection<RoleModule> RoleModules { get; set; } = new List<RoleModule>();
}
