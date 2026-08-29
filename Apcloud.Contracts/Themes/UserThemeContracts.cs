using System.ComponentModel.DataAnnotations;

namespace Apcloud.Contracts.Themes;

public sealed record UserThemeSettingsDto(
    string Mode,
    string Color,
    int Radius,
    bool IsDefault)
{
    public static UserThemeSettingsDto Default { get; } = new(
        UserThemeSettingDefaults.Mode,
        UserThemeSettingDefaults.Color,
        UserThemeSettingDefaults.Radius,
        true);
}

public sealed class UpdateUserThemeSettingsDto
{
    [Required]
    [RegularExpression("^(light|dark|system)$", ErrorMessage = "Mode must be light, dark, or system.")]
    public string Mode { get; set; } = UserThemeSettingDefaults.Mode;

    [Required]
    [RegularExpression("^(blue|azure|indigo|purple|pink|red|orange|green)$",
        ErrorMessage = "Color is not supported.")]
    public string Color { get; set; } = UserThemeSettingDefaults.Color;

    public int Radius { get; set; } = UserThemeSettingDefaults.Radius;
}

public static class UserThemeSettingDefaults
{
    public const string Mode = "system";
    public const string Color = "blue";
    public const int Radius = 6;

    public static UserThemeSettingsDto CreateDto() => UserThemeSettingsDto.Default;
}
