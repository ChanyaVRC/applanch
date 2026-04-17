using System.Windows.Media;
using applanch.Localization;

namespace applanch.Theming;

/// <summary>
/// Base class for theme definitions.
/// Handles color resolution with inheritance and fallback mechanisms.
/// </summary>
internal abstract class ThemeDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeDefinition"/> class.
    /// </summary>
    protected ThemeDefinition(
        string id,
        LocalizedText displayName,
        bool isVisibleInThemeList = true)
    {
        Id = id.ToLowerInvariant();
        DisplayName = displayName;
        IsVisibleInThemeList = isVisibleInThemeList;
    }

    /// <summary>
    /// Gets the unique identifier for this theme.
    /// </summary>
    internal string Id { get; }

    /// <summary>
    /// Gets the localized display name.
    /// </summary>
    internal LocalizedText DisplayName { get; }

    /// <summary>
    /// Gets a value indicating whether this theme should appear in theme selection lists.
    /// </summary>
    internal bool IsVisibleInThemeList { get; }

    /// <summary>
    /// Gets the color definitions by key.
    /// </summary>
    internal abstract IReadOnlyDictionary<string, ThemeColor> ColorsByKey { get; }

    /// <summary>
    /// Creates a map of brush keys to SolidColorBrush instances.
    /// </summary>
    internal Dictionary<string, SolidColorBrush> CreateBrushMap(
        IReadOnlyDictionary<string, ThemeDefinition> themesById,
        SystemThemeMode preferredSystemMode)
    {
        var allKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (_, theme) in themesById)
        {
            foreach (var (key, _) in theme.ColorsByKey)
            {
                allKeys.Add(key);
            }
        }

        var brushMap = new Dictionary<string, SolidColorBrush>(allKeys.Count);

        foreach (var key in allKeys)
        {
            var color = ResolveColor(key, themesById, preferredSystemMode);
            var brush = new SolidColorBrush(color.ToMediaColor());
            brush.Freeze();
            brushMap[key] = brush;
        }

        return brushMap;
    }

    /// <summary>
    /// Gets the related theme IDs that this theme depends on for color resolution.
    /// </summary>
    protected abstract IEnumerable<string> GetRelatedThemeIds(SystemThemeMode preferredSystemMode);

    private ThemeColor ResolveColor(
        string key,
        IReadOnlyDictionary<string, ThemeDefinition> themesById,
        SystemThemeMode preferredSystemMode)
    {
        var visited = new HashSet<string>();

        if (TryResolveColorInGraph(key, themesById, preferredSystemMode, visited, out var color))
        {
            return color;
        }

        if (Id != ThemePaletteConfigurationLoader.LightThemeId &&
            themesById.TryGetValue(ThemePaletteConfigurationLoader.LightThemeId, out var lightTheme) &&
            lightTheme.TryResolveColorInGraph(key, themesById, preferredSystemMode, visited, out var lightColor))
        {
            return lightColor;
        }

        foreach (var (_, theme) in themesById)
        {
            if (theme.ColorsByKey.TryGetValue(key, out var candidateColor))
            {
                return candidateColor;
            }
        }

        throw new InvalidOperationException($"No color value found for key '{key}'.");
    }

    private bool TryResolveColorInGraph(
        string key,
        IReadOnlyDictionary<string, ThemeDefinition> themesById,
        SystemThemeMode preferredSystemMode,
        HashSet<string> visited,
        out ThemeColor color)
    {
        if (!visited.Add(Id))
        {
            color = default;
            return false;
        }

        try
        {
            if (ColorsByKey.TryGetValue(key, out color))
            {
                return true;
            }

            foreach (var relatedId in GetRelatedThemeIds(preferredSystemMode))
            {
                if (themesById.TryGetValue(relatedId, out var relatedTheme) &&
                    relatedTheme.TryResolveColorInGraph(key, themesById, preferredSystemMode, visited, out color))
                {
                    return true;
                }
            }

            color = default;
            return false;
        }
        finally
        {
            visited.Remove(Id);
        }
    }
}

