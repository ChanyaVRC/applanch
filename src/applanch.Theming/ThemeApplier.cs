using Microsoft.Win32;
using System.Windows;

namespace applanch.Theming;

public sealed class ThemeApplier
{
    private const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightTheme = "AppsUseLightTheme";

    private readonly ThemePaletteConfiguration _configuration;
    private readonly Dictionary<string, ThemeDefinition> _themesById;

    public ThemeApplier()
        : this(ThemePaletteConfigurationLoader.Load())
    {
    }

    internal ThemeApplier(ThemePaletteConfiguration? configuration = null)
    {
        _configuration = configuration ?? ThemePaletteConfigurationLoader.Load();
        _themesById = _configuration.Themes.ToDictionary(static x => x.Id);
    }

    public void ApplyTheme(ResourceDictionary resources, string selectedThemeId)
    {
        var preferredMode = ReadWindowsThemePreference();
        var selectedTheme = ResolveTheme(selectedThemeId);
        var brushMap = selectedTheme.CreateBrushMap(_themesById, preferredMode);

        foreach (var (key, brush) in brushMap)
        {
            resources[key] = brush;
        }
    }

    public void ApplyTheme(ResourceDictionary resources, string selectedThemeId, IEnumerable<Window> windows)
    {
        ApplyTheme(resources, selectedThemeId);

        foreach (var window in windows)
        {
            WindowCaptionThemeHelper.Apply(window);
            WindowIconThemeHelper.Apply(window, resources);
        }
    }

    private ThemeDefinition ResolveTheme(string selectedThemeId)
    {
        selectedThemeId = selectedThemeId.Trim();

        if (_themesById.TryGetValue(selectedThemeId, out var selectedTheme))
        {
            return selectedTheme;
        }

        return _themesById.TryGetValue(ThemePaletteConfigurationLoader.LightThemeId, out var lightTheme)
            ? lightTheme
            : _themesById.Values.First();
    }

    private static SystemThemeMode ReadWindowsThemePreference()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeRegistryPath);
        var value = key?.GetValue(AppsUseLightTheme);
        return value is int intValue && intValue == 0 ? SystemThemeMode.Dark : SystemThemeMode.Light;
    }
}
