using System.Data;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Domain.Entities;
using Apcloudpms.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Apcloudpms.Infrastructure.Services;

public sealed class ModuleAccessService : IModuleAccessService
{
    private readonly AppDbContext _context;

    public ModuleAccessService(AppDbContext context) => _context = context;

    public async Task<ApplicationModulePagedResponseDto> GetModulesAsync(
        ApplicationModuleQueryDto query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var connection = (SqlConnection)_context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;
        if (shouldCloseConnection)
            await _context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.SpApplicationModulesGet";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@PageNumber", SqlDbType.Int)
                { Value = query.PageNumber });
            command.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int)
                { Value = query.PageSize });
            command.Parameters.Add(new SqlParameter("@SearchTerm", SqlDbType.NVarChar, 200)
                { Value = NormalizeOptional(query.SearchTerm) ?? (object)DBNull.Value });
            command.Parameters.Add(new SqlParameter("@IncludeInactive", SqlDbType.Bit)
                { Value = query.IncludeInactive });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException(
                    "dbo.SpApplicationModulesGet did not return the total record count.");
            var totalRecords = reader.GetInt64(reader.GetOrdinal("TotalRecords"));

            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException(
                    "dbo.SpApplicationModulesGet did not return the paged modules.");

            var modules = new List<ApplicationModuleGridItemDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                modules.Add(new ApplicationModuleGridItemDto(
                    reader.GetInt32(reader.GetOrdinal("Id")),
                    reader.GetString(reader.GetOrdinal("Code")),
                    reader.GetString(reader.GetOrdinal("Name")),
                    GetNullableString(reader, "Description"),
                    GetNullableString(reader, "Icon"),
                    reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
                    isActive,
                    isActive ? "Active" : "Inactive",
                    reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc"))));
            }

            var totalPages = totalRecords == 0
                ? 0
                : (totalRecords + query.PageSize - 1L) / query.PageSize;
            return new ApplicationModulePagedResponseDto(
                modules, totalRecords, totalPages, query.PageNumber, query.PageSize,
                totalRecords > 0 && query.PageNumber > 1,
                query.PageNumber < totalPages);
        }
        finally
        {
            if (shouldCloseConnection)
                await _context.Database.CloseConnectionAsync();
        }
    }

    public async Task<ApplicationModuleDto> CreateModuleAsync(
        ApplicationModuleRequestDto dto, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(dto.Code);
        if (await _context.ApplicationModules.AnyAsync(x => x.Code == code, cancellationToken))
            throw new ArgumentException("A module with this code already exists.");

        var module = new ApplicationModule
        {
            Code = code,
            Name = dto.Name.Trim(),
            Description = NormalizeOptional(dto.Description),
            Icon = NormalizeOptional(dto.Icon),
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ApplicationModules.Add(module);
        await _context.SaveChangesAsync(cancellationToken);
        return MapModule(module);
    }

    public async Task<ApplicationModuleDto?> UpdateModuleAsync(
        int id, ApplicationModuleRequestDto dto, CancellationToken cancellationToken)
    {
        var module = await _context.ApplicationModules.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (module is null) return null;

        var code = NormalizeCode(dto.Code);
        if (await _context.ApplicationModules.AnyAsync(
                x => x.Id != id && x.Code == code, cancellationToken))
            throw new ArgumentException("A module with this code already exists.");

        module.Code = code;
        module.Name = dto.Name.Trim();
        module.Description = NormalizeOptional(dto.Description);
        module.Icon = NormalizeOptional(dto.Icon);
        module.DisplayOrder = dto.DisplayOrder;
        module.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return MapModule(module);
    }

    public async Task<bool> DeleteModuleAsync(
        int id, CancellationToken cancellationToken)
    {
        var module = await _context.ApplicationModules.SingleOrDefaultAsync(
            x => x.Id == id, cancellationToken);
        if (module is null) return false;

        module.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ApplicationModuleConfigurationDto?> GetModuleConfigurationAsync(
        int id, CancellationToken cancellationToken)
    {
        var module = await _context.ApplicationModules.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ApplicationModuleDto(x.Id, x.Code, x.Name, x.Description,
                x.Icon, x.DisplayOrder, x.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
        if (module is null) return null;

        return new ApplicationModuleConfigurationDto(
            module, await GetAssignedRolesAsync(id, cancellationToken));
    }

    public async Task<ApplicationModuleConfigurationDto?> SetModuleRolesAsync(
        int id, RoleModuleAssignmentRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var module = await _context.ApplicationModules.SingleOrDefaultAsync(
            x => x.Id == id, cancellationToken);
        if (module is null) return null;

        var roleIds = await ValidateRoleIdsAsync(dto.RoleIds, cancellationToken);
        await ApplyRoleAssignmentsAsync(id, roleIds, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new ApplicationModuleConfigurationDto(
            MapModule(module), await GetAssignedRolesAsync(id, cancellationToken));
    }

    public async Task<ApplicationModuleConfigurationDto?> UpdateModuleConfigurationAsync(
        int id, ApplicationModuleConfigurationRequestDto dto,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(dto.Module);

        var module = await _context.ApplicationModules.SingleOrDefaultAsync(
            x => x.Id == id, cancellationToken);
        if (module is null) return null;

        var code = NormalizeCode(dto.Module.Code);
        if (await _context.ApplicationModules.AnyAsync(
                x => x.Id != id && x.Code == code, cancellationToken))
            throw new ArgumentException("A module with this code already exists.");

        var roleIds = await ValidateRoleIdsAsync(dto.RoleIds, cancellationToken);
        module.Code = code;
        module.Name = dto.Module.Name.Trim();
        module.Description = NormalizeOptional(dto.Module.Description);
        module.Icon = NormalizeOptional(dto.Module.Icon);
        module.DisplayOrder = dto.Module.DisplayOrder;
        module.IsActive = dto.Module.IsActive;
        await ApplyRoleAssignmentsAsync(id, roleIds, cancellationToken);

        // One SaveChanges call makes the module and role-assignment update atomic.
        await _context.SaveChangesAsync(cancellationToken);
        return new ApplicationModuleConfigurationDto(
            MapModule(module), await GetAssignedRolesAsync(id, cancellationToken));
    }

    public async Task<IReadOnlyList<ModuleMenuDto>> GetMenusAsync(
        int moduleId, bool includeInactive, CancellationToken cancellationToken)
    {
        var menus = await _context.ModuleMenus.AsNoTracking()
            .Where(x => x.ApplicationModuleId == moduleId && (includeInactive || x.IsActive))
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new ModuleMenuDto(x.Id, x.ApplicationModuleId, x.ParentMenuId,
                x.Name, x.ControllerName, x.ActionName, x.QueryUrl, x.Icon,
                x.DisplayOrder, x.IsActive))
            .ToListAsync(cancellationToken);

        return BuildMenuHierarchy(menus);
    }

    public async Task<ModuleMenuDto?> CreateMenuAsync(
        int moduleId, ModuleMenuRequestDto dto, CancellationToken cancellationToken)
    {
        if (!await _context.ApplicationModules.AnyAsync(x => x.Id == moduleId, cancellationToken))
            return null;

        await ValidateMenuAsync(moduleId, null, dto, cancellationToken);
        var menu = new ModuleMenu
        {
            ApplicationModuleId = moduleId,
            ParentMenuId = dto.ParentMenuId,
            Name = dto.Name.Trim(),
            ControllerName = NormalizeOptional(dto.ControllerName),
            ActionName = NormalizeOptional(dto.ActionName),
            QueryUrl = NormalizeQueryUrl(dto.QueryUrl),
            Icon = NormalizeOptional(dto.Icon),
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ModuleMenus.Add(menu);
        await _context.SaveChangesAsync(cancellationToken);
        return MapMenu(menu);
    }

    public async Task<ModuleMenuDto?> UpdateMenuAsync(
        int moduleId, int menuId, ModuleMenuRequestDto dto, CancellationToken cancellationToken)
    {
        var menu = await _context.ModuleMenus.SingleOrDefaultAsync(
            x => x.Id == menuId && x.ApplicationModuleId == moduleId, cancellationToken);
        if (menu is null) return null;

        await ValidateMenuAsync(moduleId, menuId, dto, cancellationToken);
        menu.ParentMenuId = dto.ParentMenuId;
        menu.Name = dto.Name.Trim();
        menu.ControllerName = NormalizeOptional(dto.ControllerName);
        menu.ActionName = NormalizeOptional(dto.ActionName);
        menu.QueryUrl = NormalizeQueryUrl(dto.QueryUrl);
        menu.Icon = NormalizeOptional(dto.Icon);
        menu.DisplayOrder = dto.DisplayOrder;
        menu.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return MapMenu(menu);
    }

    public async Task<RoleModuleMenuConfigurationDto?> GetRoleModuleMenusAsync(
        int moduleId, int roleId, CancellationToken cancellationToken)
    {
        var assignment = await GetActiveRoleModuleAsync(moduleId, roleId, cancellationToken);
        if (assignment is null) return null;

        return await BuildRoleModuleMenuConfigurationAsync(
            assignment, cancellationToken);
    }

    public async Task<RoleModuleMenuConfigurationDto?> SetRoleModuleMenusAsync(
        int moduleId, int roleId, RoleModuleMenuAssignmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var assignment = await GetActiveRoleModuleAsync(moduleId, roleId, cancellationToken);
        if (assignment is null) return null;

        var menuIds = await ValidateMenuIdsAsync(moduleId, dto.MenuIds, cancellationToken);
        var existingAssignments = await _context.RoleModuleMenus
            .Where(x => x.RoleId == roleId && x.ApplicationModuleId == moduleId)
            .ToListAsync(cancellationToken);
        var existingMenuIds = existingAssignments.Select(x => x.ModuleMenuId).ToHashSet();
        var assignedAtUtc = DateTime.UtcNow;

        foreach (var menuAssignment in existingAssignments)
        {
            var shouldBeActive = menuIds.Contains(menuAssignment.ModuleMenuId);
            if (shouldBeActive && !menuAssignment.IsActive)
                menuAssignment.AssignedAtUtc = assignedAtUtc;
            menuAssignment.IsActive = shouldBeActive;
        }

        foreach (var menuId in menuIds.Except(existingMenuIds))
        {
            _context.RoleModuleMenus.Add(new RoleModuleMenu
            {
                RoleId = roleId,
                ApplicationModuleId = moduleId,
                ModuleMenuId = menuId,
                IsActive = true,
                AssignedAtUtc = assignedAtUtc
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await BuildRoleModuleMenuConfigurationAsync(
            assignment, cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationModuleDto>> GetAssignedModulesAsync(
        int userId, CancellationToken cancellationToken) =>
        await _context.ApplicationModules.AsNoTracking()
            .Where(x => x.IsActive && x.RoleModules.Any(rm =>
                rm.IsActive && rm.Role.IsActive &&
                rm.Role.UserRoles.Any(ur => ur.UserId == userId && ur.IsActive)))
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new ApplicationModuleDto(x.Id, x.Code, x.Name,
                x.Description, x.Icon, x.DisplayOrder, x.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<ModuleNavigationDto?> SelectModuleAsync(
        int userId, int moduleId, CancellationToken cancellationToken)
    {
        var module = await _context.ApplicationModules.AsNoTracking()
            .Where(x => x.Id == moduleId && x.IsActive && x.RoleModules.Any(rm =>
                rm.IsActive && rm.Role.IsActive &&
                rm.Role.UserRoles.Any(ur => ur.UserId == userId && ur.IsActive)))
            .Select(x => new ApplicationModuleDto(x.Id, x.Code, x.Name,
                x.Description, x.Icon, x.DisplayOrder, x.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
        if (module is null) return null;

        var menus = await _context.ModuleMenus.AsNoTracking()
            .Where(x => x.ApplicationModuleId == moduleId && x.IsActive &&
                x.RoleModuleMenus.Any(rmm =>
                    rmm.IsActive && rmm.RoleModule.IsActive &&
                    rmm.RoleModule.Role.IsActive &&
                    rmm.RoleModule.Role.UserRoles.Any(ur =>
                        ur.UserId == userId && ur.IsActive)))
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new MenuProjection(x.Id, x.ParentMenuId, x.Name, x.ControllerName,
                x.ActionName, x.QueryUrl, x.Icon, x.DisplayOrder))
            .ToListAsync(cancellationToken);

        var menuIds = menus.Select(x => x.Id).ToHashSet();
        var children = menus.Where(x => x.ParentMenuId.HasValue && menuIds.Contains(x.ParentMenuId.Value))
            .GroupBy(x => x.ParentMenuId!.Value)
            .ToDictionary(x => x.Key, x => x.ToList());

        NavigationMenuDto Build(MenuProjection item) => new()
        {
            Id = item.Id,
            Name = item.Name,
            ControllerName = item.ControllerName,
            ActionName = item.ActionName,
            QueryUrl = item.QueryUrl,
            Icon = item.Icon,
            DisplayOrder = item.DisplayOrder,
            Children = children.TryGetValue(item.Id, out var childItems)
                ? childItems.Select(Build).ToList()
                : []
        };

        var navigation = menus.Where(x => x.ParentMenuId is null)
            .Select(Build).ToList();
        return new ModuleNavigationDto(module, navigation);
    }

    private async Task ValidateMenuAsync(int moduleId, int? menuId,
        ModuleMenuRequestDto dto, CancellationToken cancellationToken)
    {
        var queryUrl = NormalizeQueryUrl(dto.QueryUrl);
        if (queryUrl is not null && await _context.ModuleMenus.AnyAsync(x => x.ApplicationModuleId == moduleId &&
                x.Id != menuId && x.QueryUrl == queryUrl, cancellationToken))
            throw new ArgumentException("This query URL already exists in the module.");

        if (!dto.ParentMenuId.HasValue) return;
        if (string.IsNullOrWhiteSpace(dto.ControllerName) ||
            string.IsNullOrWhiteSpace(dto.ActionName) ||
            string.IsNullOrWhiteSpace(dto.QueryUrl))
            throw new ArgumentException(
                "Controller name, action name, and query URL are required for a child menu.");
        if (dto.ParentMenuId == menuId)
            throw new ArgumentException("A menu cannot be its own parent.");

        var menus = await _context.ModuleMenus.AsNoTracking()
            .Where(x => x.ApplicationModuleId == moduleId)
            .Select(x => new { x.Id, x.ParentMenuId })
            .ToDictionaryAsync(x => x.Id, x => x.ParentMenuId, cancellationToken);
        if (!menus.ContainsKey(dto.ParentMenuId.Value))
            throw new ArgumentException("The parent menu does not belong to this module.");

        var ancestorId = dto.ParentMenuId;
        while (ancestorId.HasValue && menus.TryGetValue(ancestorId.Value, out var next))
        {
            if (ancestorId == menuId)
                throw new ArgumentException("The selected parent would create a menu cycle.");
            ancestorId = next;
        }
    }

    private async Task<HashSet<int>> ValidateRoleIdsAsync(
        IReadOnlyList<int>? requestedRoleIds, CancellationToken cancellationToken)
    {
        if (requestedRoleIds is null)
            throw new ArgumentException("Role IDs are required.");
        if (requestedRoleIds.Any(x => x <= 0))
            throw new ArgumentException("Every role ID must be greater than zero.");

        var roleIds = requestedRoleIds.ToHashSet();
        if (roleIds.Count != requestedRoleIds.Count)
            throw new ArgumentException("Duplicate role IDs are not allowed.");
        if (roleIds.Count == 0) return roleIds;

        var roles = await _context.Roles.AsNoTracking()
            .Where(x => roleIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name, x.IsActive })
            .ToListAsync(cancellationToken);

        var foundIds = roles.Select(x => x.Id).ToHashSet();
        var missingIds = roleIds.Except(foundIds).OrderBy(x => x).ToList();
        if (missingIds.Count > 0)
            throw new ArgumentException($"Roles not found: {string.Join(", ", missingIds)}.");

        var inactiveRoles = roles.Where(x => !x.IsActive).Select(x => x.Name).OrderBy(x => x).ToList();
        if (inactiveRoles.Count > 0)
            throw new ArgumentException(
                $"Inactive roles cannot be assigned: {string.Join(", ", inactiveRoles)}.");

        return roleIds;
    }

    private async Task<HashSet<int>> ValidateMenuIdsAsync(
        int moduleId, IReadOnlyList<int>? requestedMenuIds,
        CancellationToken cancellationToken)
    {
        if (requestedMenuIds is null)
            throw new ArgumentException("Menu IDs are required.");
        if (requestedMenuIds.Any(x => x <= 0))
            throw new ArgumentException("Every menu ID must be greater than zero.");

        var menuIds = requestedMenuIds.ToHashSet();
        if (menuIds.Count != requestedMenuIds.Count)
            throw new ArgumentException("Duplicate menu IDs are not allowed.");
        if (menuIds.Count == 0) return menuIds;

        var moduleMenus = await _context.ModuleMenus.AsNoTracking()
            .Where(x => x.ApplicationModuleId == moduleId)
            .Select(x => new { x.Id, x.Name, x.ParentMenuId, x.IsActive })
            .ToListAsync(cancellationToken);
        var menusById = moduleMenus.ToDictionary(x => x.Id);

        var missingIds = menuIds.Except(menusById.Keys).OrderBy(x => x).ToList();
        if (missingIds.Count > 0)
            throw new ArgumentException(
                $"Menus do not exist in this module: {string.Join(", ", missingIds)}.");

        var inactiveMenus = moduleMenus
            .Where(x => menuIds.Contains(x.Id) && !x.IsActive)
            .Select(x => x.Name).OrderBy(x => x).ToList();
        if (inactiveMenus.Count > 0)
            throw new ArgumentException(
                $"Inactive menus cannot be assigned: {string.Join(", ", inactiveMenus)}.");

        foreach (var menuId in menuIds)
        {
            var parentId = menusById[menuId].ParentMenuId;
            while (parentId.HasValue)
            {
                if (!menusById.TryGetValue(parentId.Value, out var parentMenu))
                    throw new ArgumentException(
                        $"Menu '{menusById[menuId].Name}' has an invalid parent menu.");
                if (!menuIds.Contains(parentId.Value))
                    throw new ArgumentException(
                        $"Menu '{menusById[menuId].Name}' requires its parent menu " +
                        $"'{parentMenu.Name}' to be assigned.");
                parentId = parentMenu.ParentMenuId;
            }
        }

        return menuIds;
    }

    private async Task ApplyRoleAssignmentsAsync(
        int moduleId, HashSet<int> roleIds, CancellationToken cancellationToken)
    {
        var assignments = await _context.RoleModules
            .Where(x => x.ApplicationModuleId == moduleId)
            .ToListAsync(cancellationToken);
        var assignedRoleIds = assignments.Select(x => x.RoleId).ToHashSet();
        var assignedAtUtc = DateTime.UtcNow;

        foreach (var assignment in assignments)
        {
            var shouldBeActive = roleIds.Contains(assignment.RoleId);
            if (shouldBeActive && !assignment.IsActive)
                assignment.AssignedAtUtc = assignedAtUtc;
            assignment.IsActive = shouldBeActive;
        }

        foreach (var roleId in roleIds.Except(assignedRoleIds))
        {
            _context.RoleModules.Add(new RoleModule
            {
                RoleId = roleId,
                ApplicationModuleId = moduleId,
                IsActive = true,
                AssignedAtUtc = assignedAtUtc
            });
        }
    }

    private async Task<IReadOnlyList<RoleModuleDto>> GetAssignedRolesAsync(
        int moduleId, CancellationToken cancellationToken) =>
        await _context.RoleModules.AsNoTracking()
            .Where(x => x.ApplicationModuleId == moduleId && x.IsActive)
            .OrderBy(x => x.Role.Name)
            .Select(x => new RoleModuleDto(x.RoleId, x.Role.Name, x.IsActive))
            .ToListAsync(cancellationToken);

    private async Task<RoleModuleHeader?> GetActiveRoleModuleAsync(
        int moduleId, int roleId, CancellationToken cancellationToken) =>
        await _context.RoleModules.AsNoTracking()
            .Where(x => x.ApplicationModuleId == moduleId && x.RoleId == roleId &&
                x.IsActive && x.Role.IsActive)
            .Select(x => new RoleModuleHeader(
                x.RoleId, x.Role.Name, x.ApplicationModuleId, x.ApplicationModule.Name))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<RoleModuleMenuConfigurationDto> BuildRoleModuleMenuConfigurationAsync(
        RoleModuleHeader assignment, CancellationToken cancellationToken)
    {
        var menus = await _context.RoleModuleMenus.AsNoTracking()
            .Where(x => x.RoleId == assignment.RoleId &&
                x.ApplicationModuleId == assignment.ApplicationModuleId &&
                x.IsActive && x.ModuleMenu.IsActive)
            .OrderBy(x => x.ModuleMenu.DisplayOrder).ThenBy(x => x.ModuleMenu.Name)
            .Select(x => new ModuleMenuDto(
                x.ModuleMenu.Id, x.ModuleMenu.ApplicationModuleId,
                x.ModuleMenu.ParentMenuId, x.ModuleMenu.Name,
                x.ModuleMenu.ControllerName, x.ModuleMenu.ActionName,
                x.ModuleMenu.QueryUrl, x.ModuleMenu.Icon,
                x.ModuleMenu.DisplayOrder, x.ModuleMenu.IsActive))
            .ToListAsync(cancellationToken);

        return new RoleModuleMenuConfigurationDto(
            assignment.RoleId, assignment.RoleName,
            assignment.ApplicationModuleId, assignment.ModuleName,
            BuildMenuHierarchy(menus));
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
    private static string? NormalizeQueryUrl(string? value)
    {
        var result = NormalizeOptional(value);
        if (result is null) return null;
        return result.StartsWith('/') ? result : $"/{result}";
    }
    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
    private static ApplicationModuleDto MapModule(ApplicationModule x) =>
        new(x.Id, x.Code, x.Name, x.Description, x.Icon, x.DisplayOrder, x.IsActive);
    private static ModuleMenuDto MapMenu(ModuleMenu x) =>
        new(x.Id, x.ApplicationModuleId, x.ParentMenuId, x.Name, x.ControllerName,
            x.ActionName, x.QueryUrl, x.Icon, x.DisplayOrder, x.IsActive);

    private static IReadOnlyList<ModuleMenuDto> BuildMenuHierarchy(
        IReadOnlyList<ModuleMenuDto> menus)
    {
        var menuIds = menus.Select(x => x.Id).ToHashSet();
        var children = menus
            .Where(x => x.ParentMenuId.HasValue && menuIds.Contains(x.ParentMenuId.Value))
            .GroupBy(x => x.ParentMenuId!.Value)
            .ToDictionary(x => x.Key, x => x.ToList());

        ModuleMenuDto Build(ModuleMenuDto menu) => menu with
        {
            Children = children.TryGetValue(menu.Id, out var childMenus)
                ? childMenus.Select(Build).ToList()
                : []
        };

        return menus
            .Where(x => !x.ParentMenuId.HasValue || !menuIds.Contains(x.ParentMenuId.Value))
            .Select(Build)
            .ToList();
    }

    private sealed record MenuProjection(int Id, int? ParentMenuId, string Name,
        string? ControllerName, string? ActionName, string? QueryUrl, string? Icon, int DisplayOrder);
    private sealed record RoleModuleHeader(
        int RoleId, string RoleName, int ApplicationModuleId, string ModuleName);
}
