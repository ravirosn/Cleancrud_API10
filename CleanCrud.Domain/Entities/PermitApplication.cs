namespace CleanCrud.Domain.Entities;

public class PermitApplication
{
    public long Id { get; set; }
    public int? RiskAssessmentId { get; set; }
    public string PermitNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public string PermitIssuerName { get; set; } = string.Empty;
    public string? PermitIssuerContactNumber { get; set; }
    public string PermitReceiverName { get; set; } = string.Empty;
    public string? PermitReceiverContactNumber { get; set; }
    public string? PreRiskAssessmentNumber { get; set; }
    public string WorkLocation { get; set; } = string.Empty;
    public string WorkDescription { get; set; } = string.Empty;
    public string? SpecialInstructions { get; set; }
    public string? WorkHeightBelowSurface { get; set; }
    public int PermitTypeListItemId { get; set; }
    public int PermitStatusListItemId { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ListItem PermitTypeListItem { get; set; } = null!;
    public ListItem PermitStatusListItem { get; set; } = null!;
    public RiskAssessment? RiskAssessment { get; set; }
    public ICollection<PermitApproval> Approvals { get; set; } = new List<PermitApproval>();
}
