using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Apcloudpms.API.Services;

public sealed class EmailQueueWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> options,
    ILogger<EmailQueueWorker> logger) : BackgroundService
{
    private readonly EmailOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_options.Enabled) await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Email queue processing failed."); }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollSeconds), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var queue = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var messages = await queue.ClaimAsync(_options.BatchSize, TimeSpan.FromMinutes(_options.LeaseMinutes), cancellationToken);
        foreach (var message in messages)
        {
            try
            {
                await sender.SendAsync(message, cancellationToken);
                await queue.MarkSentAsync(message.Id, message.LockToken, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Email queue item {EmailQueueId} could not be delivered.", message.Id);
                await queue.MarkFailedAsync(message.Id, message.LockToken, exception.Message, cancellationToken);
            }
        }
    }
}
