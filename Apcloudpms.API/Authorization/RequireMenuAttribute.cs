using Microsoft.AspNetCore.Authorization;

namespace Apcloudpms.API.Authorization;

public sealed class RequireMenuAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Menu:";

    public RequireMenuAttribute(string moduleCode, string controller, string action) =>
        Policy = $"{PolicyPrefix}{moduleCode.Trim()}:{controller.Trim()}:{action.Trim()}";
}
