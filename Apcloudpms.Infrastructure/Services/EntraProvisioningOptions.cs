namespace Apcloudpms.Infrastructure.Services;

public sealed class EntraProvisioningOptions
{
    public const string SectionName = "EntraProvisioning";
    public bool AutoProvisionUsers { get; set; } = true;
    public string[] DefaultModuleCodes { get; set; } = [];
}
