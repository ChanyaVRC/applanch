using Microsoft.Win32;
using System.Windows;
using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Theming;

internal sealed class ThemeApplier
{
    private const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightTheme = "AppsUseLightTheme";

    private readonly ThemePaletteConfiguration _configuration;
    private readonly Dictionary<string, ThemeDefinition> _themesById;
    private readonly AppSettings? _settings;

    public ThemeApplier()
        : this(null, ThemePaletteConfigurationLoader.Load())
    {
    }

    internal ThemeApplier(
        AppSettings? settings = null,
        ThemePaletteConfiguration? configuration = null)
    {
        _settings = settings;
        _configuration = configuration ?? ThemePaletteConfigurationLoader.Load();
        _themesById = _configuration.Themes.ToDictionary(static x => x.Id);
    }

    public void ApplyTheme(ResourceDictionary resources)
    {
        var preferredMode = ReadWindowsThemePreference();
        var selectedTheme = ResolveTheme(_settings ?? AppSettingsProvider.Current);
        var brushMap = selectedTheme.CreateBrushMap(_themesById, preferredMode);

        foreach (var (key, brush) in brushMap)
        {
            resources[key] = brush;
        }
    }

    public void ApplyTheme(ResourceDictionary resources, IEnumerable<Window> windows)
    {
        ApplyTheme(resources);

        foreach (var window in windows)
        {
            WindowCaptionThemeHelper.Apply(window);
            WindowIconThemeHelper.Apply(window, resources);
        }
    }

    private ThemeDefinition ResolveTheme(AppSettings settings)
    {
        var selectedThemeId = settings.ThemeId.Trim();

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

