using System.Windows;
using applanch.Infrastructure.Registry;

namespace applanch.Theming;

public sealed class ThemeApplier
{
    private const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightTheme = "AppsUseLightTheme";

    private readonly ThemePaletteConfiguration _configuration;
    private readonly Dictionary<string, ThemeDefinition> _themesById;
    private readonly IRegistryRuntime _registryRuntime;

    public ThemeApplier()
        : this(ThemePaletteConfigurationLoader.Load(), new RegistryRuntime())
    {
    }

    internal ThemeApplier(ThemePaletteConfiguration? configuration = null, IRegistryRuntime? registryRuntime = null)
    {
        _configuration = configuration ?? ThemePaletteConfigurationLoader.Load();
        _themesById = _configuration.Themes.ToDictionary(static x => x.Id);
        _registryRuntime = registryRuntime ?? new RegistryRuntime();
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

    private SystemThemeMode ReadWindowsThemePreference()
    {
        using var key = _registryRuntime.OpenSubKey(Microsoft.Win32.Registry.CurrentUser, PersonalizeRegistryPath, writable: false);
        var value = key?.GetValue(AppsUseLightTheme);
        return value is int intValue && intValue == 0 ? SystemThemeMode.Dark : SystemThemeMode.Light;
    }
}
