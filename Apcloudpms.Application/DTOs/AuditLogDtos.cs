using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs;

public sealed class AuditLogQueryDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(200)]
    public string? Search { get; set; }

    [StringLength(128)]
    public string? EntityName { get; set; }

    [StringLength(20)]
    public string? Action { get; set; }

    [StringLength(200)]
    public string? ChangedBy { get; set; }

    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    [RegularExpression("^(changedAtUtc|entityName|action|changedByName)$",
        ErrorMessage = "SortBy must be changedAtUtc, entityName, action, or changedByName.")]
    public string SortBy { get; set; } = "changedAtUtc";

    [RegularExpression("^(asc|desc)$", ErrorMessage = "SortDirection must be asc or desc.")]
    public string SortDirection { get; set; } = "desc";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromUtc.HasValue && ToUtc.HasValue && FromUtc.Value > ToUtc.Value)
        {
            yield return new ValidationResult(
                "FromUtc cannot be later than ToUtc.",
                [nameof(FromUtc), nameof(ToUtc)]);
        }
    }
}

public sealed record AuditLogItemDto(
    long Id,
    string EntityName,
    string EntityDisplayName,
    string Action,
    string? EntityKey,
    string? ChangedColumns,
    string? OldValues,
    string? NewValues,
    IReadOnlyDictionary<string, string> RelatedNames,
    int? ChangedByUserId,
    string? ChangedByName,
    string? TraceId,
    string? IpAddress,
    DateTime ChangedAtUtc);

public sealed record AuditLogPagedResponseDto(
    IReadOnlyList<AuditLogItemDto> Data,
    long TotalRecords,
    long TotalPages,
    int PageNumber,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record AuditLogFilterOptionsDto(
    IReadOnlyList<string> EntityNames,
    IReadOnlyList<string> Actions);
