namespace Apcloudpms.Domain.Entities;

public class ApprovalWorkflow
{
    public int Id { get; set; }
    public int ApplicationModuleId { get; set; }
    public string WorkflowCode { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public int? SubjectTypeListItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PendingNotificationTitle { get; set; } = "{Reference} requires approval";
    public string PendingNotificationMessage { get; set; } = "{Reference} is waiting for level {Level} approval.";
    public string ApprovedNotificationTitle { get; set; } = "{Reference} was approved";
    public string ApprovedNotificationMessage { get; set; } = "{Reference} completed its approval workflow.";
    public string RejectedNotificationTitle { get; set; } = "{Reference} was rejected";
    public string RejectedNotificationMessage { get; set; } = "{Reference} was rejected at level {Level}.";
    public bool IsActive { get; set; } = true;
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ApplicationModule ApplicationModule { get; set; } = null!;
    public ListItem? SubjectTypeListItem { get; set; }
    public ICollection<ApprovalWorkflowLevel> Levels { get; set; } =
        new List<ApprovalWorkflowLevel>();
}
