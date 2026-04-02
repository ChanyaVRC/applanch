using System.IO;
using System.Text.Json;
using applanch.Infrastructure.Theming;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Storage;

internal sealed record AppSettings
{
    public bool DebugUpdate { get; init; } = false;
    public bool CloseOnLaunch { get; init; } = true;
    public string ThemeId { get; init; } = ThemePaletteConfigurationLoader.SystemThemeId;
    public bool CheckForUpdatesOnStartup { get; init; } = true;
    public bool StartMinimizedOnLaunch { get; init; } = false;
    public bool LaunchAtWindowsStartup { get; init; } = false;
    public bool ConfirmBeforeLaunch { get; init; } = false;
    public bool ConfirmBeforeDelete { get; init; } = false;
    public CategorySortMode CategorySortMode { get; init; } = CategorySortMode.Alphabetical;
    public AppListSortMode AppListSortMode { get; init; } = AppListSortMode.Manual;
    public bool RunAsAdministrator { get; init; } = false;
    public LanguageOption Language { get; init; } = LanguageOption.System;
    public PostLaunchBehavior? PostLaunchBehavior { get; init; }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "applanch",
        "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, "Failed to load settings");
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(FilePath, json);
    }

    public PostLaunchBehavior ResolvePostLaunchBehavior() =>
        PostLaunchBehavior ?? (CloseOnLaunch ? Infrastructure.Storage.PostLaunchBehavior.CloseApp : Infrastructure.Storage.PostLaunchBehavior.KeepOpen);
}

