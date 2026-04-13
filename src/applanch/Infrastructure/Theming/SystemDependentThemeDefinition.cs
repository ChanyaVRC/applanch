namespace applanch.Infrastructure.Theming;

internal sealed class SystemDependentThemeDefinition(
    string id,
    LocalizedText displayName,
    IReadOnlyDictionary<SystemThemeMode, string> sourcesByMode,
    bool isVisibleInThemeList = true) : ThemeDefinition(id, displayName, isVisibleInThemeList)
{
    private static readonly IReadOnlyDictionary<string, ThemeColor> EmptyColors = new Dictionary<string, ThemeColor>();

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
