namespace Apcloudpms.Domain.Entities;

public class ModuleMenu
{
    public int Id { get; set; }
    public int ApplicationModuleId { get; set; }
    public int? ParentMenuId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ControllerName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string QueryUrl { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ApplicationModule ApplicationModule { get; set; } = null!;
    public ModuleMenu? ParentMenu { get; set; }
    public ICollection<ModuleMenu> Children { get; set; } = new List<ModuleMenu>();
}
