namespace Apcloudpms.Infrastructure.Options;

public sealed class ProfileImageStorageOptions
{
    public const string SectionName = "ProfileImageStorage";

    public bool UseAzure { get; set; }

    public string LocalFolder { get; set; } = string.Empty;

    public long MaximumUploadBytes { get; set; } = 10 * 1024 * 1024;

    public long MaximumSavedBytes { get; set; } = 2 * 1024 * 1024;

    public int MaximumDimension { get; set; } = 1600;

    public AzureProfileImageStorageOptions Azure { get; set; } = new();
}

public sealed class AzureProfileImageStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = string.Empty;
}
