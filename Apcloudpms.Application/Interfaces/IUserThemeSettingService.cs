using Apcloudpms.Application.DTOs;

using Apcloud.Contracts.Themes;

namespace Apcloudpms.Application.Interfaces;

public interface IUserThemeSettingService
{
    Task<UserThemeSettingsDto> GetAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserThemeSettingsDto> UpsertAsync(
        int userId,
        UpdateUserThemeSettingsDto request,
        CancellationToken cancellationToken = default);
}
