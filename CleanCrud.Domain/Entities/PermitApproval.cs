namespace CleanCrud.Domain.Entities;

public class PermitApproval
{
    public long Id { get; set; }
    public long PermitApplicationId { get; set; }
    public byte LevelNumber { get; set; }
    public int PrimaryApproverRoleId { get; set; }
    public int? AlternateApproverRoleId { get; set; }
    public string Status { get; set; } = ApprovalState.Waiting;
    public int? ActionedByUserId { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ActionedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public PermitApplication PermitApplication { get; set; } = null!;
    public Role PrimaryApproverRole { get; set; } = null!;
    public Role? AlternateApproverRole { get; set; }
    public User? ActionedByUser { get; set; }
    public ICollection<ApprovalNotification> Notifications { get; set; } =
        new List<ApprovalNotification>();
    public ICollection<PermitApprovalAssignee> AssignedUsers { get; set; } =
        new List<PermitApprovalAssignee>();
}

public static class ApprovalState
{
    public const string Waiting = "WAITING";
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string Cancelled = "CANCELLED";
}
