namespace Apcloudpms.Application.Interfaces;

/// <summary>
/// Provides the effective authorization snapshot for a user. Implementations may cache
/// the snapshot, but must make <see cref="Invalidate"/> immediately retire cached grants
/// after role, module, menu, policy, or user-role configuration changes.
/// </summary>
public interface IAuthorizationCacheInvalidator
{
    void Invalidate();
}

public interface IAuthorizationAccessCache : IAuthorizationCacheInvalidator
{
    Task<bool> HasModuleAccessAsync(int userId, string moduleCode, CancellationToken cancellationToken);
    Task<bool> HasMenuAccessAsync(
        int userId, string moduleCode, string menuController, string menuAction,
        CancellationToken cancellationToken);
    Task<bool> HasPermissionAsync(int userId, string permissionCode, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetGrantedCodesAsync(
        int userId, string moduleCode, string menuController, string menuAction,
        CancellationToken cancellationToken);
}
