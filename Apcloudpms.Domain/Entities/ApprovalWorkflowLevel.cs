namespace Apcloudpms.Domain.Entities;

public class ApprovalWorkflowLevel
{
    public int Id { get; set; }
    public int ApprovalWorkflowId { get; set; }
    public byte LevelNumber { get; set; }
    public int PrimaryApproverRoleId { get; set; }
    public int? AlternateApproverRoleId { get; set; }

    public ApprovalWorkflow ApprovalWorkflow { get; set; } = null!;
    public Role PrimaryApproverRole { get; set; } = null!;
    public Role? AlternateApproverRole { get; set; }
}
