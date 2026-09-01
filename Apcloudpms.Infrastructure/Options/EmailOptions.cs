namespace Apcloudpms.Infrastructure.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Operations Hub";
    public int PollSeconds { get; set; } = 15;
    public int BatchSize { get; set; } = 20;
    public int LeaseMinutes { get; set; } = 5;
}

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";
    public string ResetPageUrl { get; set; } = string.Empty;
    public int TokenLifetimeMinutes { get; set; } = 30;
}
