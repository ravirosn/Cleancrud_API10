namespace Apcloudpms.Application.DTOs;

public sealed record QueueEmailRequestDto(
    string ToEmail,
    string Subject,
    string? HtmlBody = null,
    string? TextBody = null,
    string? ToName = null,
    string? CorrelationId = null,
    int MaxAttempts = 5);

public sealed record QueuedEmailDto(
    long Id,
    Guid LockToken,
    string ToEmail,
    string? ToName,
    string Subject,
    string? HtmlBody,
    string? TextBody,
    int AttemptCount,
    int MaxAttempts);
