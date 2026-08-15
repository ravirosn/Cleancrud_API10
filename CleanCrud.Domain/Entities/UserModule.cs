namespace CleanCrud.Domain.Entities;

public class UserModule
{
    public int UserId { get; set; }
    public int ApplicationModuleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ApplicationModule ApplicationModule { get; set; } = null!;
}
