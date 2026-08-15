namespace CleanCrud.Infrastructure.Services;

public sealed class PowerBiOptions
{
    public const string SectionName = "PowerBi";
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
}
