namespace Apcloudpms.Domain.Entities;

public class Department
{
    public int Id { get; set; }
    public int OfficeBranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public OfficeBranch OfficeBranch { get; set; } = null!;
    public ICollection<User> Users { get; set; } = new List<User>();
}
