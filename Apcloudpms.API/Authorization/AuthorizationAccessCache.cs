using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Apcloudpms.API.Authorization;

public sealed class AuthorizationCacheVersion : IAuthorizationCacheInvalidator
{
    private long _value;
    public long Value => Interlocked.Read(ref _value);
    public void Advance() => Interlocked.Increment(ref _value);
    public void Invalidate() => Advance();
}

/// <summary>
/// Builds one effective authorization snapshot per user and reuses it across module,
/// menu, and action checks. The versioned key gives mutations immediate logical
/// invalidation without scanning or clearing unrelated entries in IMemoryCache.
/// </summary>
public sealed class AuthorizationAccessCache(
    AppDbContext context,
    IMemoryCache memoryCache,
    AuthorizationCacheVersion version) : IAuthorizationAccessCache, IDisposable
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public async Task<bool> HasModuleAccessAsync(
        int userId, string moduleCode, CancellationToken cancellationToken)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(moduleCode)) return false;
        var snapshot = await GetSnapshotAsync(userId, cancellationToken);
        return snapshot.IsSuperAdmin || snapshot.Modules.Contains(moduleCode.Trim());
    }

    public async Task<bool> HasMenuAccessAsync(
        int userId, string moduleCode, string menuController, string menuAction,
        CancellationToken cancellationToken)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(moduleCode) ||
            string.IsNullOrWhiteSpace(menuController) || string.IsNullOrWhiteSpace(menuAction))
            return false;
        var snapshot = await GetSnapshotAsync(userId, cancellationToken);
        return snapshot.IsSuperAdmin || snapshot.Menus.Contains(RouteKey(moduleCode, menuController, menuAction));
    }

    public async Task<bool> HasPermissionAsync(
        int userId, string permissionCode, CancellationToken cancellationToken)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(permissionCode)) return false;
        var snapshot = await GetSnapshotAsync(userId, cancellationToken);
        return snapshot.IsSuperAdmin || snapshot.PermissionCodes.Contains(permissionCode.Trim());
    }

    public async Task<IReadOnlyList<string>> GetGrantedCodesAsync(
        int userId, string moduleCode, string menuController, string menuAction,
        CancellationToken cancellationToken)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(moduleCode) ||
            string.IsNullOrWhiteSpace(menuController) || string.IsNullOrWhiteSpace(menuAction))
            return [];
        var snapshot = await GetSnapshotAsync(userId, cancellationToken);
        var route = RouteKey(moduleCode, menuController, menuAction);
        return snapshot.Permissions
            .Where(permission => RouteKey(
                permission.ModuleCode, permission.MenuController, permission.MenuAction) == route)
            .Select(permission => permission.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public void Invalidate() => version.Advance();

    private async Task<AuthorizationSnapshot> GetSnapshotAsync(
        int userId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey(userId, version.Value);
        if (memoryCache.TryGetValue(cacheKey, out AuthorizationSnapshot? cached) && cached is not null)
            return cached;

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            cacheKey = CacheKey(userId, version.Value);
            if (memoryCache.TryGetValue(cacheKey, out cached) && cached is not null)
                return cached;

            var snapshot = await LoadSnapshotAsync(userId, cancellationToken);
            memoryCache.Set(cacheKey, snapshot, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            });
            return snapshot;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task<AuthorizationSnapshot> LoadSnapshotAsync(
        int userId, CancellationToken cancellationToken)
    {
        var userRoles = await context.UserRoles.AsNoTracking()
            .Where(userRole => userRole.UserId == userId && userRole.User.IsActive &&
                userRole.IsActive && userRole.Role.IsActive)
            .Select(userRole => new { userRole.RoleId, userRole.Role.NormalizedName })
            .ToListAsync(cancellationToken);
        var roleIds = userRoles.Select(userRole => userRole.RoleId).ToArray();
        var isSuperAdmin = userRoles.Any(userRole => userRole.NormalizedName == "SUPERADMIN");

        var permissions = isSuperAdmin
            ? await context.PermissionPolicies.AsNoTracking()
                .Where(policy => policy.IsActive)
                .Select(policy => new PermissionGrant(
                    policy.Code, policy.ModuleCode, policy.MenuController, policy.MenuAction))
                .ToListAsync(cancellationToken)
            : await context.RolePermissions.AsNoTracking()
                .Where(rolePermission => rolePermission.IsActive &&
                    roleIds.Contains(rolePermission.RoleId) && rolePermission.Role.IsActive &&
                    rolePermission.PermissionPolicy.IsActive &&
                    (!rolePermission.PermissionPolicy.RequiresMenuAccess ||
                     rolePermission.Role.RoleModules.Any(roleModule => roleModule.IsActive &&
                        roleModule.ApplicationModule.IsActive &&
                        roleModule.ApplicationModule.Code == rolePermission.PermissionPolicy.ModuleCode &&
                        roleModule.RoleModuleMenus.Any(roleMenu => roleMenu.IsActive &&
                            roleMenu.ModuleMenu.IsActive &&
                            roleMenu.ModuleMenu.ControllerName == rolePermission.PermissionPolicy.MenuController &&
                            roleMenu.ModuleMenu.ActionName == rolePermission.PermissionPolicy.MenuAction))))
                .Select(rolePermission => new PermissionGrant(
                    rolePermission.PermissionPolicy.Code,
                    rolePermission.PermissionPolicy.ModuleCode,
                    rolePermission.PermissionPolicy.MenuController,
                    rolePermission.PermissionPolicy.MenuAction))
                .Distinct()
                .ToListAsync(cancellationToken);

        if (isSuperAdmin)
            return new AuthorizationSnapshot(true, [], [], permissions);

        var modules = await context.RoleModules.AsNoTracking()
            .Where(roleModule => roleModule.IsActive && roleIds.Contains(roleModule.RoleId) &&
                roleModule.Role.IsActive && roleModule.ApplicationModule.IsActive)
            .Select(roleModule => roleModule.ApplicationModule.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var menus = await context.RoleModuleMenus.AsNoTracking()
            .Where(roleMenu => roleMenu.IsActive && roleIds.Contains(roleMenu.RoleId) &&
                roleMenu.RoleModule.IsActive && roleMenu.RoleModule.Role.IsActive &&
                roleMenu.RoleModule.ApplicationModule.IsActive && roleMenu.ModuleMenu.IsActive)
            .Select(roleMenu => new
            {
                roleMenu.RoleModule.ApplicationModule.Code,
                roleMenu.ModuleMenu.ControllerName,
                roleMenu.ModuleMenu.ActionName
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        return new AuthorizationSnapshot(
            false,
            modules,
            menus.Select(menu => RouteKey(menu.Code, menu.ControllerName ?? string.Empty, menu.ActionName ?? string.Empty)),
            permissions);
    }

    private static string CacheKey(int userId, long cacheVersion) =>
        $"authorization-access:{cacheVersion}:{userId}";

    private static string RouteKey(string moduleCode, string controller, string action) =>
        $"{moduleCode.Trim()}\u001f{controller.Trim()}\u001f{action.Trim()}".ToUpperInvariant();

    public void Dispose() => _loadLock.Dispose();

    private sealed record PermissionGrant(
        string Code, string ModuleCode, string MenuController, string MenuAction);

    private sealed class AuthorizationSnapshot
    {
        public static AuthorizationSnapshot Empty { get; } = new(false, [], [], []);

        public AuthorizationSnapshot(
            bool isSuperAdmin,
            IEnumerable<string> modules,
            IEnumerable<string> menus,
            IReadOnlyList<PermissionGrant> permissions)
        {
            IsSuperAdmin = isSuperAdmin;
            Modules = new HashSet<string>(modules, StringComparer.OrdinalIgnoreCase);
            Menus = new HashSet<string>(menus, StringComparer.OrdinalIgnoreCase);
            Permissions = permissions;
            PermissionCodes = new HashSet<string>(permissions.Select(item => item.Code), StringComparer.OrdinalIgnoreCase);
        }

        public bool IsSuperAdmin { get; }
        public HashSet<string> Modules { get; }
        public HashSet<string> Menus { get; }
        public IReadOnlyList<PermissionGrant> Permissions { get; }
        public HashSet<string> PermissionCodes { get; }
    }
}
