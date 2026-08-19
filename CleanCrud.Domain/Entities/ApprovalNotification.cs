namespace CleanCrud.Domain.Entities;

public class ApprovalNotification
{
    public long Id { get; set; }
    public long PermitApprovalId { get; set; }
    public int RecipientUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = NotificationState.Pending;
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }

    public PermitApproval PermitApproval { get; set; } = null!;
    public User RecipientUser { get; set; } = null!;
}

public static class NotificationState
{
    public const string Pending = "PENDING";
    public const string Sent = "SENT";
    public const string Failed = "FAILED";
}
