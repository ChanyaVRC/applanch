using System.IO;
using System.Text.Json;
using applanch.Theming;
using applanch.Core.Utilities;
using applanch.Core.Localization;

namespace applanch.Infrastructure.Storage;

internal sealed record AppSettings
{
    internal const int DefaultQuickAddSuggestionLimit = 50;
    internal const int MinQuickAddSuggestionLimit = 1;
    internal const int MaxQuickAddSuggestionLimit = 200;

    public bool DebugUpdate { get; init; } = false;
    public string ThemeId { get; init; } = ThemePaletteConfigurationLoader.SystemThemeId;
    public int QuickAddSuggestionLimit { get; init; } = DefaultQuickAddSuggestionLimit;
    public bool CheckForUpdatesOnStartup { get; init; } = true;
    public UpdateInstallBehavior UpdateInstallBehavior { get; init; } = UpdateInstallBehavior.Manual;
    public bool AllowPrereleaseUpdates { get; init; } = false;
    public bool StartMinimizedOnLaunch { get; init; } = false;
    public bool LaunchAtWindowsStartup { get; init; } = false;
    public bool RegisterContextMenuOnStartup { get; init; } = true;
    public bool CategorySidebarPinned { get; init; } = true;
    public bool FetchHttpIcons { get; init; } = true;
    public bool AllowPrivateNetworkHttpIconRequests { get; init; } = false;
    public bool ConfirmBeforeLaunch { get; init; } = false;
    public bool ConfirmBeforeDelete { get; init; } = false;
    public CategorySortMode CategorySortMode { get; init; } = CategorySortMode.Alphabetical;
    public AppListSortMode AppListSortMode { get; init; } = AppListSortMode.Manual;
    public bool LaunchItemIconOnlyMode { get; init; } = false;
    public bool RunAsAdministrator { get; init; } = false;
    public LanguageOption Language { get; init; } = LanguageOption.System;
    public PostLaunchBehavior PostLaunchBehavior { get; init; } = PostLaunchBehavior.CloseApp;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly string FilePath = AppDataPaths.GetUnderLocalApplicationData("settings.json");
    private static readonly string DirectoryPath = AppDataPaths.LocalApplicationDataDirectory;

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
            var normalized = loaded.Normalize();

            if (loaded != normalized)
            {
                try
                {
                    normalized.SaveCore();
                }
                catch (Exception ex)
                {
                    AppLogger.Instance.Warn(ex, "Failed to rewrite normalized settings");
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
        Normalize().SaveCore();
    }

    private void SaveCore()
    {
        Directory.CreateDirectory(DirectoryPath);
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(FilePath, json);
    }

    internal AppSettings Normalize()
    {
        var themeId = NormalizeThemeId(ThemeId);
        var quickAddSuggestionLimit = Math.Clamp(
            QuickAddSuggestionLimit,
            MinQuickAddSuggestionLimit,
            MaxQuickAddSuggestionLimit);

        return this with
        {
            ThemeId = themeId,
            Language = NormalizeLanguage(Language),
            QuickAddSuggestionLimit = quickAddSuggestionLimit,
        };
    }

    private static LanguageOption NormalizeLanguage(LanguageOption? language) =>
        language ?? LanguageOption.System;

    private static string NormalizeThemeId(string? themeId)
    {
        if (!string.IsNullOrWhiteSpace(themeId))
        {
            return themeId.Trim();
        }

        return ThemePaletteConfigurationLoader.SystemThemeId;
    }
}