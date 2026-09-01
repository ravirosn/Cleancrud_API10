using System.Net;
using System.Net.Mail;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Apcloudpms.Infrastructure.Services;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(QueuedEmailDto email, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) throw new InvalidOperationException("Email delivery is disabled.");
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = email.Subject,
            Body = email.HtmlBody ?? email.TextBody ?? string.Empty,
            IsBodyHtml = email.HtmlBody is not null
        };
        message.To.Add(new MailAddress(email.ToEmail, email.ToName));
        if (email.HtmlBody is not null && email.TextBody is not null)
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(email.TextBody, null, "text/plain"));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            UseDefaultCredentials = string.IsNullOrWhiteSpace(_options.UserName),
            Credentials = string.IsNullOrWhiteSpace(_options.UserName)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.UserName, _options.Password)
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}
