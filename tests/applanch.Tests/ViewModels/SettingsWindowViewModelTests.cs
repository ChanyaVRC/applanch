using Xunit;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;

namespace applanch.Tests;

public class SettingsWindowViewModelTests
{
    private static readonly IReadOnlyList<ThemeOption> ThemeOptions =
    [
        new ThemeOption(ThemePaletteConfigurationLoader.SystemThemeId, "System", IsSystemOption: true),
        new ThemeOption(ThemePaletteConfigurationLoader.LightThemeId, "Light"),
        new ThemeOption(ThemePaletteConfigurationLoader.DarkThemeId, "Dark"),
        new ThemeOption("monochrome", "Monochrome")
    ];

    private static SettingsWindowViewModel Make(
        AppSettings? settings = null,
        Action<AppSettings>? onCommit = null,
        Action<AppSettings>? onRefresh = null,
        List<AppSettings>? saved = null)
    {
        var captures = saved ?? [];
        var appEvent = new AppEvent();
        if (onCommit is not null)
        {
            appEvent.Register(AppEvents.Commit, onCommit);
        }

        if (onRefresh is not null)
        {
            appEvent.Register(AppEvents.Refresh, onRefresh);
        }

        return new SettingsWindowViewModel(
            settings ?? new AppSettings(),
            appEvent,
            s => captures.Add(s),
            () => ThemeOptions);
    }

    // ── Initial state ──────────────────────────────────────

    [Fact]
    public void InitialValues_ReflectSettingsPassedIn()
    {
        var settings = new AppSettings
        {
            Theme = AppTheme.Dark,
            CloseOnLaunch = false,
            CheckForUpdatesOnStartup = false,
            DebugUpdate = true,
            Language = LanguageOption.Japanese,
            CategorySortMode = CategorySortMode.AsAdded,
        };

        var vm = Make(settings);

        Assert.Equal((int)AppTheme.Dark, vm.ThemeIndex);
        Assert.False(vm.CloseOnLaunch);
        Assert.False(vm.CheckForUpdatesOnStartup);
        Assert.True(vm.DebugUpdate);
        Assert.Equal((int)LanguageOption.Japanese, vm.LanguageIndex);
        Assert.Equal((int)CategorySortMode.AsAdded, vm.CategorySortModeIndex);
        Assert.False(vm.SettingsChanged);
    }

    // ── ThemeIndex ─────────────────────────────────────────

    [Fact]
    public void ThemeIndex_Change_FiresPropertyChanged()
    {
        var vm = Make();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.ThemeIndex = (int)AppTheme.Dark;

        Assert.Contains(nameof(vm.ThemeIndex), raised);
    }

    [Fact]
    public void ThemeIndex_Change_CallsOnCommitWithNewTheme()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.ThemeIndex = (int)AppTheme.Light;

        Assert.NotNull(committed);
        Assert.Equal(AppTheme.Light, committed!.Theme);
    }

    [Fact]
    public void ThemeIndex_Change_CallsOnRefreshWithNewTheme()
    {
        AppSettings? refreshed = null;
        var vm = Make(onRefresh: s => refreshed = s);

        vm.ThemeIndex = (int)AppTheme.Light;

        Assert.NotNull(refreshed);
        Assert.Equal(AppTheme.Light, refreshed!.Theme);
    }

    [Fact]
    public void ThemeIndex_Change_CallsOnCommitWithMonochromeTheme()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.ThemeIndex = 3;

        Assert.NotNull(committed);
        Assert.Equal(AppTheme.Light, committed!.Theme);
        Assert.Equal("monochrome", committed.ThemeId);
    }

    [Fact]
    public void ThemeIndex_SameValue_DoesNotSave()
    {
        var saved = new List<AppSettings>();
        var vm = Make(settings: new AppSettings { Theme = AppTheme.Light }, saved: saved);

        vm.ThemeIndex = (int)AppTheme.Light;

        Assert.Empty(saved);
    }

    // ── CloseOnLaunch ──────────────────────────────────────

    [Fact]
    public void CloseOnLaunch_Change_FiresPropertyChanged()
    {
        var vm = Make(settings: new AppSettings { CloseOnLaunch = true });
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.CloseOnLaunch = false;

        Assert.Contains(nameof(vm.CloseOnLaunch), raised);
    }

    [Fact]
    public void CloseOnLaunch_SameValue_DoesNotSave()
    {
        var saved = new List<AppSettings>();
        var vm = Make(settings: new AppSettings { CloseOnLaunch = true }, saved: saved);

        vm.CloseOnLaunch = true;

        Assert.Empty(saved);
    }

    // ── Commit ─────────────────────────────────────────────

    [Fact]
    public void Commit_SetsSettingsChanged()
    {
        var saved = new List<AppSettings>();
        var vm = Make(saved: saved);

        vm.DebugUpdate = true;

        Assert.True(vm.SettingsChanged);
        Assert.Single(saved);
    }

    [Fact]
    public void Commit_SavesAllCurrentValues()
    {
        var saved = new List<AppSettings>();
        var vm = Make(
            settings: new AppSettings { Theme = AppTheme.System, CloseOnLaunch = true, CheckForUpdatesOnStartup = true, DebugUpdate = false },
            saved: saved);

        vm.ThemeIndex = (int)AppTheme.Dark;
        vm.CloseOnLaunch = false;

        var last = saved.Last();
        Assert.Equal(AppTheme.Dark, last.Theme);
        Assert.False(last.CloseOnLaunch);
    }

    [Fact]
    public void PostLaunchBehaviorIndex_Change_UpdatesSavedSettings()
    {
        var saved = new List<AppSettings>();
        var vm = Make(saved: saved);

        vm.PostLaunchBehaviorIndex = (int)PostLaunchBehavior.MinimizeWindow;

        var last = saved.Last();
        Assert.Equal(PostLaunchBehavior.MinimizeWindow, last.PostLaunchBehavior);
        Assert.False(last.CloseOnLaunch);
    }

    [Fact]
    public void LanguageIndex_Change_UpdatesSavedSettings()
    {
        var saved = new List<AppSettings>();
        var vm = Make(saved: saved);

        vm.LanguageIndex = (int)LanguageOption.English;

        Assert.Equal(LanguageOption.English, saved.Last().Language);
    }

    [Fact]
    public void LaunchAtWindowsStartup_Change_UpdatesSavedSettings()
    {
        var saved = new List<AppSettings>();
        var vm = Make(saved: saved);

        vm.LaunchAtWindowsStartup = true;

        Assert.True(saved.Last().LaunchAtWindowsStartup);
    }

    [Fact]
    public void ConfirmBeforeDelete_Change_UpdatesSavedSettings()
    {
        var saved = new List<AppSettings>();
        var vm = Make(saved: saved);

        vm.ConfirmBeforeDelete = true;

        Assert.True(saved.Last().ConfirmBeforeDelete);
    }

    [Fact]
    public void AppListSortModeIndex_Change_UpdatesSavedSettings()
    {
        var saved = new List<AppSettings>();
        var vm = Make(saved: saved);

        vm.AppListSortModeIndex = (int)AppListSortMode.CategoryThenName;

        Assert.Equal(AppListSortMode.CategoryThenName, saved.Last().AppListSortMode);
    }

    [Fact]
    public void PostLaunchBehaviorIndex_CloseApp_SetsCloseOnLaunchTrue()
    {
        var vm = Make(settings: new AppSettings { CloseOnLaunch = false });

        vm.PostLaunchBehaviorIndex = (int)PostLaunchBehavior.CloseApp;

        Assert.True(vm.CloseOnLaunch);
    }

    // ── ResetToDefaults ────────────────────────────────────

    [Fact]
    public void ResetToDefaults_RestoresAllDefaultValues()
    {
        var defaults = new AppSettings();
        var settings = new AppSettings
        {
            Theme = AppTheme.Dark,
            Language = LanguageOption.Japanese,
            CloseOnLaunch = false,
            CheckForUpdatesOnStartup = false,
            DebugUpdate = true,
            StartMinimizedOnLaunch = true,
            LaunchAtWindowsStartup = true,
            ConfirmBeforeLaunch = true,
            ConfirmBeforeDelete = true,
            CategorySortMode = CategorySortMode.AsAdded,
            AppListSortMode = AppListSortMode.Name,
            RunAsAdministrator = true,
        };
        var vm = Make(settings: settings);

        vm.ResetToDefaults();

        Assert.Equal((int)defaults.Theme, vm.ThemeIndex);
        Assert.Equal((int)defaults.Language, vm.LanguageIndex);
        Assert.Equal(defaults.CloseOnLaunch, vm.CloseOnLaunch);
        Assert.Equal(defaults.CheckForUpdatesOnStartup, vm.CheckForUpdatesOnStartup);
        Assert.Equal(defaults.DebugUpdate, vm.DebugUpdate);
        Assert.Equal(defaults.StartMinimizedOnLaunch, vm.StartMinimizedOnLaunch);
        Assert.Equal(defaults.LaunchAtWindowsStartup, vm.LaunchAtWindowsStartup);
        Assert.Equal(defaults.ConfirmBeforeLaunch, vm.ConfirmBeforeLaunch);
        Assert.Equal(defaults.ConfirmBeforeDelete, vm.ConfirmBeforeDelete);
        Assert.Equal((int)defaults.CategorySortMode, vm.CategorySortModeIndex);
        Assert.Equal((int)defaults.AppListSortMode, vm.AppListSortModeIndex);
        Assert.Equal(defaults.RunAsAdministrator, vm.RunAsAdministrator);
    }

    [Fact]
    public void ResetToDefaults_CommitsAndFiresPropertyChanged()
    {
        AppSettings? committed = null;
        var raised = new List<string?>();
        var vm = Make(
            settings: new AppSettings { Theme = AppTheme.Dark },
            onCommit: s => committed = s);
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.ResetToDefaults();

        Assert.NotNull(committed);
        Assert.Equal(AppTheme.System, committed!.Theme);
        Assert.Contains(nameof(vm.ThemeIndex), raised);
        Assert.Contains(nameof(vm.LanguageIndex), raised);
    }

    [Fact]
    public void ApplyExternalSettings_UpdatesValuesWithoutSaving()
    {
        var saved = new List<AppSettings>();
        var vm = Make(saved: saved);
        var refreshed = new AppSettings
        {
            Theme = AppTheme.Dark,
            PostLaunchBehavior = PostLaunchBehavior.MinimizeWindow,
            CloseOnLaunch = false,
            CheckForUpdatesOnStartup = false,
            Language = LanguageOption.Japanese,
        };

        vm.ApplyExternalSettings(refreshed);

        Assert.Equal((int)AppTheme.Dark, vm.ThemeIndex);
        Assert.Equal((int)PostLaunchBehavior.MinimizeWindow, vm.PostLaunchBehaviorIndex);
        Assert.False(vm.CloseOnLaunch);
        Assert.False(vm.CheckForUpdatesOnStartup);
        Assert.Equal((int)LanguageOption.Japanese, vm.LanguageIndex);
        Assert.Empty(saved);
    }

    [Fact]
    public void ApplyExternalSettings_WhenLanguageChanges_ReloadsThemeOptions()
    {
        var appEvent = new AppEvent();
        var providerCallCount = 0;
        IReadOnlyList<ThemeOption> ThemeOptionsProvider()
        {
            providerCallCount++;
            return providerCallCount == 1
                ?
                [
                    new ThemeOption(ThemePaletteConfigurationLoader.SystemThemeId, "System", IsSystemOption: true),
                    new ThemeOption(ThemePaletteConfigurationLoader.LightThemeId, "Light")
                ]
                :
                [
                    new ThemeOption(ThemePaletteConfigurationLoader.SystemThemeId, "システム", IsSystemOption: true),
                    new ThemeOption(ThemePaletteConfigurationLoader.LightThemeId, "ライト")
                ];
        }

        var vm = new SettingsWindowViewModel(
            new AppSettings { Language = LanguageOption.English },
            appEvent,
            save: null,
            ThemeOptionsProvider);

        vm.ApplyExternalSettings(new AppSettings { Language = LanguageOption.Japanese });

        Assert.Equal(2, providerCallCount);
        Assert.Equal("システム", vm.ThemeOptions[0].DisplayName);
    }
}
