namespace Apcloudpms.Domain.Enums;

/// <summary>
/// Built-in application roles. The member names are the canonical role names
/// stored in role claims; enum values are not database role IDs.
/// </summary>
public enum ApplicationRole
{
    User,
    Admin,
    SuperAdmin
}
