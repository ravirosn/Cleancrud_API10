using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IEmailQueueService
{
    Task<long> EnqueueAsync(QueueEmailRequestDto email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueuedEmailDto>> ClaimAsync(int batchSize, TimeSpan lease, CancellationToken cancellationToken = default);
    Task MarkSentAsync(long id, Guid lockToken, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(long id, Guid lockToken, string error, CancellationToken cancellationToken = default);
}

public interface IEmailSender
{
    Task SendAsync(QueuedEmailDto email, CancellationToken cancellationToken = default);
}
