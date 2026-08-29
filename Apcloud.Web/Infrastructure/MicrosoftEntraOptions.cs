using System.ComponentModel.DataAnnotations;

namespace Apcloud.Web.Infrastructure;

public sealed class MicrosoftEntraOptions
{
    public const string SectionName = "Authentication:MicrosoftEntra";

    public bool Enabled { get; init; }

    [Required]
    public string Instance { get; init; } = "https://login.microsoftonline.com/";

    public string TenantId { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    [Required]
    public string CallbackPath { get; init; } = "/signin-oidc";

    [Required]
    public string SignedOutCallbackPath { get; init; } = "/signout-callback-oidc";

    public string[] ApiScopes { get; init; } = [];

    public bool IsConfigured =>
        Enabled &&
        !string.IsNullOrWhiteSpace(TenantId) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        ApiScopes.Length > 0 &&
        ApiScopes.All(scope => !string.IsNullOrWhiteSpace(scope));
}
