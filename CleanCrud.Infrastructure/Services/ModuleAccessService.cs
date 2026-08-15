using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using CleanCrud.Domain.Entities;
using CleanCrud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CleanCrud.Infrastructure.Services;

public sealed class ModuleAccessService : IModuleAccessService
{
    private readonly AppDbContext _context;

    public ModuleAccessService(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<ApplicationModuleDto>> GetModulesAsync(
        bool includeInactive, CancellationToken cancellationToken) =>
        await _context.ApplicationModules.AsNoTracking()
            .Where(x => includeInactive || x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new ApplicationModuleDto(x.Id, x.Code, x.Name, x.Description,
                x.Icon, x.DisplayOrder, x.IsActive))
            .ToListAsync(cancellationToken);

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

    public async Task<IReadOnlyList<ModuleMenuDto>> GetMenusAsync(
        int moduleId, bool includeInactive, CancellationToken cancellationToken) =>
        await _context.ModuleMenus.AsNoTracking()
            .Where(x => x.ApplicationModuleId == moduleId && (includeInactive || x.IsActive))
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new ModuleMenuDto(x.Id, x.ApplicationModuleId, x.ParentMenuId,
                x.Name, x.ControllerName, x.ActionName, x.QueryUrl, x.Icon,
                x.DisplayOrder, x.IsActive))
            .ToListAsync(cancellationToken);

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
            ControllerName = dto.ControllerName.Trim(),
            ActionName = dto.ActionName.Trim(),
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
        menu.ControllerName = dto.ControllerName.Trim();
        menu.ActionName = dto.ActionName.Trim();
        menu.QueryUrl = NormalizeQueryUrl(dto.QueryUrl);
        menu.Icon = NormalizeOptional(dto.Icon);
        menu.DisplayOrder = dto.DisplayOrder;
        menu.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return MapMenu(menu);
    }

    public async Task<bool> SetUserModuleAsync(
        UserModuleAssignmentDto dto, CancellationToken cancellationToken)
    {
        if (!await _context.Users.AnyAsync(x => x.Id == dto.UserId, cancellationToken))
            return false;
        var module = await _context.ApplicationModules.SingleOrDefaultAsync(
            x => x.Id == dto.ApplicationModuleId, cancellationToken);
        if (module is null) return false;
        if (dto.IsActive && !module.IsActive)
            throw new ArgumentException("An inactive module cannot be assigned as active.");

        var assignment = await _context.UserModules.SingleOrDefaultAsync(
            x => x.UserId == dto.UserId && x.ApplicationModuleId == dto.ApplicationModuleId,
            cancellationToken);
        if (assignment is null)
        {
            _context.UserModules.Add(new UserModule
            {
                UserId = dto.UserId,
                ApplicationModuleId = dto.ApplicationModuleId,
                IsActive = dto.IsActive,
                AssignedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            assignment.IsActive = dto.IsActive;
            if (dto.IsActive) assignment.AssignedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ApplicationModuleDto>> GetAssignedModulesAsync(
        int userId, CancellationToken cancellationToken) =>
        await _context.UserModules.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && x.ApplicationModule.IsActive)
            .OrderBy(x => x.ApplicationModule.DisplayOrder).ThenBy(x => x.ApplicationModule.Name)
            .Select(x => new ApplicationModuleDto(x.ApplicationModule.Id,
                x.ApplicationModule.Code, x.ApplicationModule.Name,
                x.ApplicationModule.Description, x.ApplicationModule.Icon,
                x.ApplicationModule.DisplayOrder, x.ApplicationModule.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<ModuleNavigationDto?> SelectModuleAsync(
        int userId, int moduleId, CancellationToken cancellationToken)
    {
        var module = await _context.UserModules.AsNoTracking()
            .Where(x => x.UserId == userId && x.ApplicationModuleId == moduleId &&
                        x.IsActive && x.ApplicationModule.IsActive)
            .Select(x => new ApplicationModuleDto(x.ApplicationModule.Id,
                x.ApplicationModule.Code, x.ApplicationModule.Name,
                x.ApplicationModule.Description, x.ApplicationModule.Icon,
                x.ApplicationModule.DisplayOrder, x.ApplicationModule.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
        if (module is null) return null;

        var menus = await _context.ModuleMenus.AsNoTracking()
            .Where(x => x.ApplicationModuleId == moduleId && x.IsActive)
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
        if (await _context.ModuleMenus.AnyAsync(x => x.ApplicationModuleId == moduleId &&
                x.Id != menuId && x.QueryUrl == queryUrl, cancellationToken))
            throw new ArgumentException("This query URL already exists in the module.");

        if (!dto.ParentMenuId.HasValue) return;
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

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
    private static string NormalizeQueryUrl(string value)
    {
        var result = value.Trim();
        return result.StartsWith('/') ? result : $"/{result}";
    }
    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ApplicationModuleDto MapModule(ApplicationModule x) =>
        new(x.Id, x.Code, x.Name, x.Description, x.Icon, x.DisplayOrder, x.IsActive);
    private static ModuleMenuDto MapMenu(ModuleMenu x) =>
        new(x.Id, x.ApplicationModuleId, x.ParentMenuId, x.Name, x.ControllerName,
            x.ActionName, x.QueryUrl, x.Icon, x.DisplayOrder, x.IsActive);

    private sealed record MenuProjection(int Id, int? ParentMenuId, string Name,
        string ControllerName, string ActionName, string QueryUrl, string? Icon, int DisplayOrder);
}
