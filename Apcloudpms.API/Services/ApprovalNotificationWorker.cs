using Apcloudpms.Application.Interfaces;
using Apcloudpms.Domain.Entities;
using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Apcloudpms.API.Services;

public sealed class ApprovalNotificationWorker(
    IServiceScopeFactory scopeFactory,
    IApprovalNotificationQueue queue,
    ILogger<ApprovalNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(30));
                try
                {
                    await queue.WaitAsync(timeout.Token);
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // Periodic polling makes database notifications durable across API restarts.
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Approval notification processing failed.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pending = await context.ApprovalNotifications
            .Include(x => x.RecipientUser)
            .Where(x => (x.Status == NotificationState.Pending ||
                         x.Status == NotificationState.Failed) && x.AttemptCount < 5)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var notification in pending)
        {
            notification.AttemptCount++;
            try
            {
                // This is an in-app delivery adapter. Replace this block with a Power Automate
                // webhook later; the durable outbox and workflow code remain unchanged.
                logger.LogInformation(
                    "Approval notification {NotificationId} delivered to user {UserId} ({Email}): {Title}",
                    notification.Id, notification.RecipientUserId,
                    notification.RecipientUser.Email, notification.Title);
                notification.Status = NotificationState.Sent;
                notification.SentAtUtc = DateTime.UtcNow;
                notification.LastError = null;
            }
            catch (Exception exception)
            {
                notification.Status = NotificationState.Failed;
                notification.LastError = exception.Message[..Math.Min(exception.Message.Length, 1000)];
            }
        }

        if (pending.Count > 0)
            await context.SaveChangesAsync(cancellationToken);
    }
}
