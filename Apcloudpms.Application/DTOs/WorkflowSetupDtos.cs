using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed class WorkflowSetupQueryDto
{
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
    [StringLength(200)] public string? SearchTerm { get; set; }
    [StringLength(30)] public string SortBy { get; set; } = "updatedAtUtc";
    [RegularExpression("^(?i:asc|desc)$")] public string SortDirection { get; set; } = "desc";
    [Range(1, int.MaxValue)] public int? ApplicationModuleId { get; set; }
    public bool IncludeInactive { get; set; }
}

public sealed record WorkflowSetupGridDto(
    int Id, string WorkflowCode, int ApplicationModuleId, string ModuleName,
    string SubjectType, int? SubjectTypeListItemId, string? SubjectTypeName,
    string Name, int LevelCount, bool IsActive, string Status,
    DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

public sealed record WorkflowSetupPagedResponseDto(
    IReadOnlyList<WorkflowSetupGridDto> Data, long TotalRecords,
    long TotalPages, int PageNumber, int PageSize,
    bool HasPreviousPage, bool HasNextPage);

public sealed class WorkflowSetupRequestDto
{
    [Range(1, int.MaxValue)] public int ApplicationModuleId { get; set; }
    [Required, StringLength(100, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$")]
    public string WorkflowCode { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 2)]
    [RegularExpression(@"^[A-Za-z0-9_-]+$")]
    public string SubjectType { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int? SubjectTypeListItemId { get; set; }
    [Required, StringLength(150, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    [Required, MinLength(1), MaxLength(5)]
    public IReadOnlyList<ApprovalWorkflowLevelRequestDto> Levels { get; set; } = [];
    [Required, StringLength(200)] public string PendingNotificationTitle { get; set; } = "{Reference} requires approval";
    [Required, StringLength(1000)] public string PendingNotificationMessage { get; set; } = "{Reference} is waiting for level {Level} approval.";
    [Required, StringLength(200)] public string ApprovedNotificationTitle { get; set; } = "{Reference} was approved";
    [Required, StringLength(1000)] public string ApprovedNotificationMessage { get; set; } = "{Reference} completed its approval workflow.";
    [Required, StringLength(200)] public string RejectedNotificationTitle { get; set; } = "{Reference} was rejected";
    [Required, StringLength(1000)] public string RejectedNotificationMessage { get; set; } = "{Reference} was rejected at level {Level}.";
}

public sealed record WorkflowSetupDetailDto(
    int Id, int ApplicationModuleId, string ModuleCode, string ModuleName,
    string WorkflowCode, string SubjectType, int? SubjectTypeListItemId,
    string? SubjectTypeName, string Name, bool IsActive,
    string PendingNotificationTitle, string PendingNotificationMessage,
    string ApprovedNotificationTitle, string ApprovedNotificationMessage,
    string RejectedNotificationTitle, string RejectedNotificationMessage,
    IReadOnlyList<ApprovalWorkflowLevelDto> Levels);

public sealed record WorkflowModuleOptionDto(int Id, string Code, string Name);
public sealed record WorkflowRoleOptionDto(int Id, string Name);
public sealed record WorkflowSubjectCategoryOptionDto(int Id, string Code, string Name);
public sealed record WorkflowSubjectOptionDto(int Id, string Code, string Name);
