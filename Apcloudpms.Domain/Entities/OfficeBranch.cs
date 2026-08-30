namespace Apcloudpms.Domain.Entities;

public class OfficeBranch
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsHeadOffice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Organization Organization { get; set; } = null!;
    public ICollection<Department> Departments { get; set; } = new List<Department>();
}
