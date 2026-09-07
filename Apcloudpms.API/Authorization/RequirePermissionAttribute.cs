using Microsoft.AspNetCore.Authorization;

namespace Apcloudpms.API.Authorization;

public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public RequirePermissionAttribute(string permissionCode) =>
        Policy = $"{PolicyPrefix}{permissionCode.Trim()}";
}
