using applanch.Localization;

namespace applanch.Theming;

/// <summary>
/// A theme definition with fixed color values and optional theme inheritance.
/// </summary>
internal sealed class FixedThemeDefinition(
    string id,
    LocalizedText displayName,
    string? inheritedThemeId = null,
    IReadOnlyDictionary<ThemeBrushKey, ThemeColor>? colorsByKey = null,
    bool isVisibleInThemeList = true) : ThemeDefinition(id, displayName, isVisibleInThemeList)
{
    /// <summary>
    /// Gets the ID of the theme this theme inherits colors from (if any).
    /// </summary>
    internal string? InheritedThemeId { get; } = inheritedThemeId;

    internal override IReadOnlyDictionary<ThemeBrushKey, ThemeColor> ColorsByKey { get; } =
        colorsByKey ?? new Dictionary<ThemeBrushKey, ThemeColor>();

    protected override IEnumerable<string> GetRelatedThemeIds(SystemThemeMode preferredSystemMode)
    {
        if (!string.IsNullOrWhiteSpace(InheritedThemeId))
        {
            yield return InheritedThemeId;
        }
    }
}

