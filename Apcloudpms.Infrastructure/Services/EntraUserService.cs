using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Domain.Entities;
using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Apcloudpms.Infrastructure.Services;

public sealed class EntraUserService : IEntraUserService
{
    private readonly AppDbContext _context;
    private readonly EntraProvisioningOptions _options;

    public EntraUserService(AppDbContext context, IOptions<EntraProvisioningOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<AuthenticatedUserDto?> EnsureUserAsync(
        EntraUserProfileDto profile, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(profile.TenantId, profile.ObjectId, cancellationToken);
        if (user is null)
        {
            if (!_options.AutoProvisionUsers) return null;
            user = await CreateUserAsync(profile, cancellationToken);
        }
        else
        {
            if (!user.IsActive) return null;
            var displayName = NormalizeOptional(profile.DisplayName, 200);
            var email = NormalizeOptional(profile.Email, 320);
            if (user.DisplayName != displayName || user.Email != email)
            {
                user.DisplayName = displayName;
                user.Email = email;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        var roles = await _context.UserRoles.AsNoTracking()
            .Where(x => x.UserId == user.Id && x.IsActive && x.Role.IsActive)
            .Select(x => x.Role.Name)
            .ToListAsync(cancellationToken);
        return new AuthenticatedUserDto(user.Id, roles);
    }

    private async Task<User> CreateUserAsync(
        EntraUserProfileDto profile, CancellationToken cancellationToken)
    {
        var defaultRole = await _context.Roles.SingleOrDefaultAsync(
            x => x.NormalizedName == "USER" && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("The active default User role is not configured.");

        var userName = await CreateUniqueUserNameAsync(profile, cancellationToken);
        var user = new User
        {
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            PasswordHash = null,
            EntraTenantId = profile.TenantId,
            EntraObjectId = profile.ObjectId,
            DisplayName = NormalizeOptional(profile.DisplayName, 200),
            Email = NormalizeOptional(profile.Email, 320),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.UserRoles.Add(new UserRole
        {
            RoleId = defaultRole.Id,
            IsActive = true,
            AssignedAtUtc = DateTime.UtcNow
        });

        var moduleCodes = _options.DefaultModuleCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct()
            .ToArray();
        var modules = await _context.ApplicationModules
            .Where(x => x.IsActive && moduleCodes.Contains(x.Code))
            .ToListAsync(cancellationToken);
        foreach (var module in modules)
        {
            user.UserModules.Add(new UserModule
            {
                ApplicationModuleId = module.Id,
                IsActive = true,
                AssignedAtUtc = DateTime.UtcNow
            });
        }

        _context.Users.Add(user);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
            var concurrentlyCreated = await FindUserAsync(
                profile.TenantId, profile.ObjectId, cancellationToken);
            if (concurrentlyCreated is null) throw;
            return concurrentlyCreated;
        }
    }

    private async Task<User?> FindUserAsync(
        Guid tenantId, Guid objectId, CancellationToken cancellationToken) =>
        await _context.Users.SingleOrDefaultAsync(
            x => x.EntraTenantId == tenantId && x.EntraObjectId == objectId,
            cancellationToken);

    private async Task<string> CreateUniqueUserNameAsync(
        EntraUserProfileDto profile, CancellationToken cancellationToken)
    {
        var baseName = string.IsNullOrWhiteSpace(profile.UserName)
            ? profile.ObjectId.ToString("N")
            : profile.UserName.Trim();
        baseName = baseName[..Math.Min(baseName.Length, 100)];
        if (!await _context.Users.AnyAsync(
                x => x.NormalizedUserName == baseName.ToUpper(), cancellationToken))
            return baseName;

        var suffix = $"_{profile.ObjectId:N}"[..9];
        return $"{baseName[..Math.Min(baseName.Length, 100 - suffix.Length)]}{suffix}";
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized[..Math.Min(normalized.Length, maxLength)];
    }
}
