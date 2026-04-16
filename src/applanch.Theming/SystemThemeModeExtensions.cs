namespace applanch.Theming;

/// <summary>
/// Extension methods for <see cref="SystemThemeMode"/>.
/// </summary>
internal static class SystemThemeModeExtensions
{
    /// <summary>
    /// Converts a <see cref="SystemThemeMode"/> to its corresponding theme ID string.
    /// </summary>
    internal static string ToThemeId(this SystemThemeMode mode) =>
        mode switch
        {
            SystemThemeMode.Light => ThemePaletteConfigurationLoader.LightThemeId,
            SystemThemeMode.Dark => ThemePaletteConfigurationLoader.DarkThemeId,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown SystemThemeMode"),
        };

    /// <summary>
    /// Attempts to convert a theme ID string (after normalization) to a <see cref="SystemThemeMode"/>.
    /// Returns null if the string does not correspond to a known system theme mode.
    /// </summary>
    internal static SystemThemeMode? TryParseFromThemeId(string normalizedThemeId) =>
        normalizedThemeId switch
        {
            ThemePaletteConfigurationLoader.LightThemeId => SystemThemeMode.Light,
            ThemePaletteConfigurationLoader.DarkThemeId => SystemThemeMode.Dark,
            _ => null,
        };
}
