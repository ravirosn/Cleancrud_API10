using System.Data;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Domain.Entities;
using Apcloudpms.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Apcloudpms.Infrastructure.Services;

public sealed class EmailQueueService(AppDbContext context) : IEmailQueueService
{
    public async Task<long> EnqueueAsync(QueueEmailRequestDto email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        if (string.IsNullOrWhiteSpace(email.ToEmail)) throw new ArgumentException("A recipient email address is required.");
        if (string.IsNullOrWhiteSpace(email.Subject)) throw new ArgumentException("An email subject is required.");
        if (string.IsNullOrWhiteSpace(email.HtmlBody) && string.IsNullOrWhiteSpace(email.TextBody))
            throw new ArgumentException("An HTML or text email body is required.");

        var now = DateTime.UtcNow;
        var item = new EmailQueueItem
        {
            ToEmail = email.ToEmail.Trim(), ToName = Normalize(email.ToName), Subject = email.Subject.Trim(),
            HtmlBody = Normalize(email.HtmlBody), TextBody = Normalize(email.TextBody), Status = "Pending",
            MaxAttempts = Math.Clamp(email.MaxAttempts, 1, 20), NextAttemptAtUtc = now,
            CorrelationId = Normalize(email.CorrelationId), CreatedAtUtc = now
        };
        context.EmailQueue.Add(item);
        await context.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task<IReadOnlyList<QueuedEmailDto>> ClaimAsync(
        int batchSize, TimeSpan lease, CancellationToken cancellationToken = default)
    {
        var connection = (SqlConnection)context.Database.GetDbConnection();
        var close = connection.State == ConnectionState.Closed;
        if (close) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.SPEmailQueueClaim";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@BatchSize", SqlDbType.Int) { Value = Math.Clamp(batchSize, 1, 100) });
            command.Parameters.Add(new SqlParameter("@LeaseSeconds", SqlDbType.Int) { Value = Math.Clamp((int)lease.TotalSeconds, 30, 3600) });
            var result = new List<QueuedEmailDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                result.Add(new QueuedEmailDto(reader.GetInt64(0), reader.GetGuid(1), reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3), reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.GetInt32(7), reader.GetInt32(8)));
            return result;
        }
        finally { if (close) await connection.CloseAsync(); }
    }

    public Task MarkSentAsync(long id, Guid lockToken, CancellationToken cancellationToken = default) =>
        ExecuteCompletionAsync("dbo.SPEmailQueueMarkSent", id, lockToken, null, cancellationToken);

    public Task MarkFailedAsync(long id, Guid lockToken, string error, CancellationToken cancellationToken = default) =>
        ExecuteCompletionAsync("dbo.SPEmailQueueMarkFailed", id, lockToken,
            string.IsNullOrWhiteSpace(error) ? "Unknown email delivery error." : error[..Math.Min(error.Length, 2000)], cancellationToken);

    private async Task ExecuteCompletionAsync(string procedure, long id, Guid lockToken, string? error, CancellationToken cancellationToken)
    {
        var connection = (SqlConnection)context.Database.GetDbConnection();
        var close = connection.State == ConnectionState.Closed;
        if (close) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = procedure; command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.BigInt) { Value = id });
            command.Parameters.Add(new SqlParameter("@LockToken", SqlDbType.UniqueIdentifier) { Value = lockToken });
            if (error is not null) command.Parameters.Add(new SqlParameter("@Error", SqlDbType.NVarChar, 2000) { Value = error });
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { if (close) await connection.CloseAsync(); }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
