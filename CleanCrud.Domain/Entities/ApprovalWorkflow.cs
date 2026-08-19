namespace CleanCrud.Domain.Entities;

public class ApprovalWorkflow
{
    public int Id { get; set; }
    public int PermitTypeListItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ListItem PermitTypeListItem { get; set; } = null!;
    public ICollection<ApprovalWorkflowLevel> Levels { get; set; } =
        new List<ApprovalWorkflowLevel>();
}
