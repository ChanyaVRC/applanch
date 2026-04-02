using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;
using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Theming;

internal sealed class ThemeManager
{
    private const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightTheme = "AppsUseLightTheme";

    private readonly ThemePaletteConfiguration _configuration;
    private readonly Dictionary<string, ThemeDefinition> _themesById;
    private readonly Dictionary<string, Dictionary<string, SolidColorBrush>> _brushesByThemeId;
    private readonly Func<AppSettings> _settingsProvider;

    public ThemeManager(
        Func<AppSettings>? settingsProvider = null,
        ThemePaletteConfiguration? configuration = null)
    {
        _settingsProvider = settingsProvider ?? AppSettings.Load;
        _configuration = configuration ?? ThemePaletteConfigurationLoader.LoadForRuntime();
        _themesById = _configuration.Themes.ToDictionary(static x => x.Id, StringComparer.OrdinalIgnoreCase);
        _brushesByThemeId = BuildBrushMaps(_configuration, _themesById);
    }

    public void ApplyTheme(ResourceDictionary resources)
    {
        var selectedThemeId = ResolveThemeId(_settingsProvider());
        if (!_brushesByThemeId.TryGetValue(selectedThemeId, out var brushMap))
        {
            brushMap = _brushesByThemeId.Values.First();
        }

        foreach (var entry in _configuration.Entries)
        {
            resources[entry.Key] = brushMap[entry.Key];
        }
    }

    public void ApplyTheme(ResourceDictionary resources, IEnumerable<Window> windows)
    {
        ApplyTheme(resources);

        foreach (var window in windows)
        {
            WindowCaptionThemeHelper.Apply(window);
        }
    }

    private static Dictionary<string, Dictionary<string, SolidColorBrush>> BuildBrushMaps(
        ThemePaletteConfiguration configuration,
        IReadOnlyDictionary<string, ThemeDefinition> themesById)
    {
        var brushMaps = new Dictionary<string, Dictionary<string, SolidColorBrush>>(StringComparer.OrdinalIgnoreCase);
        var availableThemeIds = configuration.Themes
            .Select(static x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var themeId in availableThemeIds)
        {
            brushMaps[themeId] = new Dictionary<string, SolidColorBrush>(configuration.Entries.Count, StringComparer.Ordinal);
        }

        foreach (var entry in configuration.Entries)
        {
            foreach (var themeId in availableThemeIds)
            {
                var hex = ResolveHex(entry, themeId, themesById);
                var color = ColorFromHex(hex);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                brushMaps[themeId][entry.Key] = brush;
            }
        }

        return brushMaps;
    }

    private static string ResolveHex(
        ThemePaletteEntry entry,
        string themeId,
        IReadOnlyDictionary<string, ThemeDefinition> themesById)
    {
        if (entry.ColorsByThemeId.TryGetValue(themeId, out var hex))
        {
            return hex;
        }

        var inheritedHex = ResolveInheritedHex(entry, themeId, themesById);
        if (!string.IsNullOrWhiteSpace(inheritedHex))
        {
            return inheritedHex;
        }

        if (entry.ColorsByThemeId.TryGetValue(ThemePaletteConfigurationLoader.LightThemeId, out var lightHex))
        {
            return lightHex;
        }

        return entry.ColorsByThemeId.Values.First(static x => !string.IsNullOrWhiteSpace(x));
    }

    private static string? ResolveInheritedHex(
        ThemePaletteEntry entry,
        string themeId,
        IReadOnlyDictionary<string, ThemeDefinition> themesById)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { themeId };
        var currentThemeId = themeId;

        while (themesById.TryGetValue(currentThemeId, out var theme) &&
               theme.TryGetInheritedThemeId(out var parentThemeId) &&
               visited.Add(parentThemeId))
        {
            if (entry.ColorsByThemeId.TryGetValue(parentThemeId, out var inheritedHex))
            {
                return inheritedHex;
            }

            currentThemeId = parentThemeId;
        }

        return null;
    }

    private string ResolveThemeId(AppSettings settings)
    {
        var selectedThemeId = settings.ThemeId.Trim();
        var preferredMode = ReadWindowsThemePreference()
            ? SystemThemeMode.Light
            : SystemThemeMode.Dark;
        var isSystemDependentTheme = false;

        if (_themesById.TryGetValue(selectedThemeId, out var selectedTheme) &&
            selectedTheme is SystemDependentThemeDefinition systemDependentTheme &&
            systemDependentTheme.TryResolveSystemSource(preferredMode, out var preferredThemeId))
        {
            isSystemDependentTheme = true;
            if (_brushesByThemeId.ContainsKey(preferredThemeId))
            {
                return preferredThemeId;
            }
        }

        if (_brushesByThemeId.ContainsKey(selectedThemeId))
        {
            return selectedThemeId;
        }

        if (isSystemDependentTheme && _brushesByThemeId.ContainsKey(ThemeIdFor(preferredMode)))
        {
            return ThemeIdFor(preferredMode);
        }

        return _brushesByThemeId.ContainsKey(ThemePaletteConfigurationLoader.LightThemeId)
            ? ThemePaletteConfigurationLoader.LightThemeId
            : _brushesByThemeId.Keys.First();
    }

    private static Color ColorFromHex(string hex)
    {
        return (Color)ColorConverter.ConvertFromString(hex)!;
    }

    private static string ThemeIdFor(SystemThemeMode mode) =>
        mode == SystemThemeMode.Light
            ? ThemePaletteConfigurationLoader.LightThemeId
            : ThemePaletteConfigurationLoader.DarkThemeId;

    private static bool ReadWindowsThemePreference()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeRegistryPath);
        var value = key?.GetValue(AppsUseLightTheme);
        return value is not int intValue || intValue != 0;
    }
}

