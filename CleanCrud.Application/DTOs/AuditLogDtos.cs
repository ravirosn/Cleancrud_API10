using System.ComponentModel.DataAnnotations;

namespace CleanCrud.Application.DTOs;

public sealed class AuditLogQueryDto
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
