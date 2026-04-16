using applanch.Core.Localization;

namespace applanch.Theming;

/// <summary>
/// A theme definition that adapts its inherited theme based on system light/dark mode preference.
/// </summary>
internal sealed class SystemDependentThemeDefinition(
    string id,
    LocalizedText displayName,
    IReadOnlyDictionary<SystemThemeMode, string> sourcesByMode,
    bool isVisibleInThemeList = true) : ThemeDefinition(id, displayName, isVisibleInThemeList)
{
    private static readonly IReadOnlyDictionary<string, ThemeColor> EmptyColors = new Dictionary<string, ThemeColor>();

    /// <summary>
    /// Gets the mapping of system theme modes to their corresponding source theme IDs.
    /// </summary>
    internal IReadOnlyDictionary<SystemThemeMode, string> SourcesByMode { get; } = sourcesByMode;

    internal override IReadOnlyDictionary<string, ThemeColor> ColorsByKey => EmptyColors;

    protected override IEnumerable<string> GetRelatedThemeIds(SystemThemeMode preferredSystemMode)
    {
        if (SourcesByMode.TryGetValue(preferredSystemMode, out var sourceThemeId))
        {
            yield return sourceThemeId;
        }

        yield return preferredSystemMode == SystemThemeMode.Light
            ? ThemePaletteConfigurationLoader.LightThemeId
            : ThemePaletteConfigurationLoader.DarkThemeId;
    }
}
