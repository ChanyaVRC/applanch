namespace applanch.Infrastructure.Theming;

internal sealed class SystemDependentThemeDefinition(
    string id,
    LocalizedText displayName,
    IReadOnlyDictionary<SystemThemeMode, string> sourcesByMode) : ThemeDefinition(id, displayName)
{
    private static readonly IReadOnlyDictionary<string, string> EmptyColors = new Dictionary<string, string>(StringComparer.Ordinal);

    internal IReadOnlyDictionary<SystemThemeMode, string> SourcesByMode { get; } = sourcesByMode;

    internal override IReadOnlyDictionary<string, string> ColorsByKey => EmptyColors;

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
