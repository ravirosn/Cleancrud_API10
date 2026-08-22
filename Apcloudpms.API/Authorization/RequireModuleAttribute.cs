using Microsoft.AspNetCore.Authorization;

namespace Apcloudpms.API.Authorization;

public sealed class RequireModuleAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Module:";

    public RequireModuleAttribute(string moduleCode) =>
        Policy = $"{PolicyPrefix}{moduleCode.Trim().ToUpperInvariant()}";
}
