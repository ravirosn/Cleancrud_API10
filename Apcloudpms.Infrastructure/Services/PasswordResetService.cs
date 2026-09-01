using System.Net;
using System.Security.Cryptography;
using System.Text;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Domain.Entities;
using Apcloudpms.Infrastructure.Data;
using Apcloudpms.Infrastructure.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Apcloudpms.Infrastructure.Services;

public sealed class PasswordResetService(
    AppDbContext context, IPasswordService passwordService, IOptions<PasswordResetOptions> options)
    : IPasswordResetService
{
    private readonly PasswordResetOptions _options = options.Value;

    public async Task RequestAsync(string userNameOrEmail, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var identifier = userNameOrEmail.Trim();
        var normalized = identifier.ToUpperInvariant();
        var user = await context.Users.SingleOrDefaultAsync(x => x.IsActive && x.PasswordHash != null &&
            (x.NormalizedUserName == normalized || (x.Email != null && x.Email == identifier)), cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.Email)) return;

        var now = DateTime.UtcNow;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.PasswordResetTokens.Where(x => x.UserId == user.Id && x.UsedAtUtc == null && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAtUtc, now), cancellationToken);

        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id, TokenHash = Hash(rawToken), CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(_options.TokenLifetimeMinutes), RequestedByIp = NormalizeIp(ipAddress)
        });
        var resetUrl = QueryHelpers.AddQueryString(_options.ResetPageUrl, "token", rawToken);
        var name = WebUtility.HtmlEncode(user.DisplayName ?? user.UserName);
        var safeUrl = WebUtility.HtmlEncode(resetUrl);
        context.EmailQueue.Add(new EmailQueueItem
        {
            ToEmail = user.Email.Trim(), ToName = user.DisplayName, Subject = "Reset your Operations Hub password",
            TextBody = $"Hello {user.DisplayName ?? user.UserName},\n\nReset your password using this link: {resetUrl}\n\nThis link expires in {_options.TokenLifetimeMinutes} minutes. If you did not request this, ignore this email.",
            HtmlBody = $"<p>Hello {name},</p><p>Use the link below to reset your Operations Hub password.</p><p><a href=\"{safeUrl}\">Reset password</a></p><p>This link expires in {_options.TokenLifetimeMinutes} minutes. If you did not request this, ignore this email.</p>",
            Status = "Pending", MaxAttempts = 5, NextAttemptAtUtc = now,
            CorrelationId = $"PASSWORD_RESET:{user.Id}:{now:yyyyMMddHHmmss}", CreatedAtUtc = now
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> ResetAsync(string token, string newPassword, string? ipAddress, CancellationToken cancellationToken = default)
    {
        ValidatePassword(newPassword);
        var tokenHash = Hash(token);
        var now = DateTime.UtcNow;
        var resetToken = await context.PasswordResetTokens.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (resetToken is null || resetToken.UsedAtUtc != null || resetToken.RevokedAtUtc != null ||
            resetToken.ExpiresAtUtc <= now || !resetToken.User.IsActive || resetToken.User.PasswordHash is null)
            return false;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        resetToken.User.PasswordHash = passwordService.HashPassword(newPassword);
        resetToken.User.ModifiedAtUtc = now;
        resetToken.UsedAtUtc = now;
        await context.RefreshTokens.Where(x => x.UserId == resetToken.UserId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAtUtc, now)
                .SetProperty(x => x.RevokedByIp, NormalizeIp(ipAddress))
                .SetProperty(x => x.RevocationReason, "Password reset"), cancellationToken);
        await context.PasswordResetTokens.Where(x => x.UserId == resetToken.UserId && x.Id != resetToken.Id &&
                x.UsedAtUtc == null && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAtUtc, now), cancellationToken);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string? NormalizeIp(string? value) => string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, 45)];
    private static void ValidatePassword(string password)
    {
        if (password.Length < 12 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new ArgumentException("Password must be at least 12 characters and include uppercase, lowercase, number, and special characters.");
        if (Encoding.UTF8.GetByteCount(password) > 72) throw new ArgumentException("Password must not exceed 72 UTF-8 bytes.");
    }
}
