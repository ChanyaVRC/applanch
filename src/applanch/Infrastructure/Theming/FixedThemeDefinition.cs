namespace applanch.Infrastructure.Theming;

internal sealed class FixedThemeDefinition(
    string id,
    LocalizedText displayName,
    string? inheritedThemeId = null,
    IReadOnlyDictionary<string, string>? colorsByKey = null,
    bool isVisibleInThemeList = true) : ThemeDefinition(id, displayName, isVisibleInThemeList)
{
    internal string? InheritedThemeId { get; } = inheritedThemeId;

    internal override IReadOnlyDictionary<string, string> ColorsByKey { get; } =
        colorsByKey ?? new Dictionary<string, string>();

    protected override IEnumerable<string> GetRelatedThemeIds(SystemThemeMode preferredSystemMode)
    {
        if (!string.IsNullOrWhiteSpace(InheritedThemeId))
        {
            yield return InheritedThemeId;
        }
    }
}
