namespace Apcloud.Web.Infrastructure;

public static class ModuleSessionContext
{
    public const string ActiveModuleIdKey = "Apcloud.ActiveModuleId";

    /// <summary>
    /// Resolves module workspaces that have stable MVC areas. This lets direct links
    /// and authentication return URLs recover their navigation after session expiry.
    /// </summary>
    public static string? GetModuleIdentifierForArea(string? area) => area switch
    {
        "PermitApplication" => "PERMIT",
        _ => null
    };
}
