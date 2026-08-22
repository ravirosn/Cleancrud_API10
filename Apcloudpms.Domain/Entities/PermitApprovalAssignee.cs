namespace Apcloudpms.Domain.Entities;

public class PermitApprovalAssignee
{
    public long PermitApprovalId { get; set; }
    public int UserId { get; set; }
    public bool IsActive { get; set; } = true;
    public int AssignedByUserId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public int? RevokedByUserId { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    public PermitApproval PermitApproval { get; set; } = null!;
    public User User { get; set; } = null!;
    public User AssignedByUser { get; set; } = null!;
    public User? RevokedByUser { get; set; }
}
