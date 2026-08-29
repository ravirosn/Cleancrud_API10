using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Domain.Entities;
using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using Apcloud.Contracts.Themes;

namespace Apcloudpms.Infrastructure.Services;

public sealed class UserThemeSettingService(AppDbContext context) : IUserThemeSettingService
{
    private static readonly HashSet<string> Modes = new(StringComparer.OrdinalIgnoreCase)
    {
        "light", "dark", "system"
    };

    private static readonly HashSet<string> Colors = new(StringComparer.OrdinalIgnoreCase)
    {
        "blue", "azure", "indigo", "purple", "pink", "red", "orange", "green"
    };

    private static readonly HashSet<int> Radii = [0, 6, 12];

    public async Task<UserThemeSettingsDto> GetAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var setting = await context.UserThemeSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        return setting is null
            ? UserThemeSettingDefaults.CreateDto()
            : ToDto(setting);
    }

    public async Task<UserThemeSettingsDto> UpsertAsync(
        int userId,
        UpdateUserThemeSettingsDto request,
        CancellationToken cancellationToken = default)
    {
        var mode = request.Mode.Trim().ToLowerInvariant();
        var color = request.Color.Trim().ToLowerInvariant();
        if (!Modes.Contains(mode) || !Colors.Contains(color) || !Radii.Contains(request.Radius))
        {
            throw new ArgumentException("One or more theme settings are not supported.");
        }

        if (!await context.Users.AnyAsync(user => user.Id == userId && user.IsActive, cancellationToken))
        {
            throw new KeyNotFoundException("The current user does not exist or is inactive.");
        }

        var now = DateTime.UtcNow;
        var setting = await context.UserThemeSettings
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (setting is null)
        {
            setting = new UserThemeSetting
            {
                UserId = userId,
                Mode = mode,
                Color = color,
                Radius = request.Radius,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            context.UserThemeSettings.Add(setting);
        }
        else
        {
            setting.Mode = mode;
            setting.Color = color;
            setting.Radius = request.Radius;
            setting.UpdatedAtUtc = now;
        }

        await context.SaveChangesAsync(cancellationToken);
        return ToDto(setting);
    }

    private static UserThemeSettingsDto ToDto(UserThemeSetting setting) =>
        new(
            setting.Mode,
            setting.Color,
            setting.Radius,
            setting.Mode == UserThemeSettingDefaults.Mode &&
            setting.Color == UserThemeSettingDefaults.Color &&
            setting.Radius == UserThemeSettingDefaults.Radius);
}
