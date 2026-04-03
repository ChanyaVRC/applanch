using System.IO;
using System.Text.Json;
using applanch.Infrastructure.Theming;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Storage;

internal sealed record AppSettings
{
    public bool DebugUpdate { get; init; } = false;
    public string ThemeId { get; init; } = ThemePaletteConfigurationLoader.SystemThemeId;
    public bool CheckForUpdatesOnStartup { get; init; } = true;
    public UpdateInstallBehavior UpdateInstallBehavior { get; init; } = UpdateInstallBehavior.Manual;
    public bool StartMinimizedOnLaunch { get; init; } = false;
    public bool LaunchAtWindowsStartup { get; init; } = false;
    public bool FetchHttpIcons { get; init; } = true;
    public bool AllowPrivateNetworkHttpIconRequests { get; init; } = false;
    public bool ConfirmBeforeLaunch { get; init; } = false;
    public bool ConfirmBeforeDelete { get; init; } = false;
    public CategorySortMode CategorySortMode { get; init; } = CategorySortMode.Alphabetical;
    public AppListSortMode AppListSortMode { get; init; } = AppListSortMode.Manual;
    public bool RunAsAdministrator { get; init; } = false;
    public LanguageOption Language { get; init; } = LanguageOption.System;
    public PostLaunchBehavior PostLaunchBehavior { get; init; } = PostLaunchBehavior.CloseApp;

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
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            var normalized = Normalize(loaded);

            if (loaded != normalized)
            {
                try
                {
                    normalized.Save();
                }
                catch (Exception ex)
                {
                    AppLogger.Instance.Warn($"Failed to rewrite normalized settings: {ex.Message}");
                }
            }

            return normalized;
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
        var json = JsonSerializer.Serialize(Normalize(this), JsonOptions);
        File.WriteAllText(FilePath, json);
    }

    internal static AppSettings Normalize(AppSettings settings)
    {
        var themeId = NormalizeThemeId(settings.ThemeId);

        return settings with
        {
            ThemeId = themeId,
        };
    }

    private static string NormalizeThemeId(string? themeId)
    {
        if (!string.IsNullOrWhiteSpace(themeId))
        {
            return themeId.Trim();
        }

        return ThemePaletteConfigurationLoader.SystemThemeId;
    }

}

