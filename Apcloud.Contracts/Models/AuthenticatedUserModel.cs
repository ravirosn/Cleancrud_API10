using Apcloud.Contracts.Enums;

namespace Apcloud.Contracts.Models;

public class AuthenticatedUserModel
{
    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public AuthenticationMethod AuthenticationMethod { get; set; }
}
