namespace Apcloudpms.Infrastructure.Services;

public sealed class EntraProvisioningOptions
{
    public const string SectionName = "EntraProvisioning";
    public bool AutoProvisionUsers { get; set; }
    public string[] DefaultModuleCodes { get; set; } = [];
}
