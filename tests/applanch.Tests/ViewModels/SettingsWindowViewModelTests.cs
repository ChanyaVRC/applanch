using Xunit;
using applanch.Events;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using applanch.ViewModels;

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
        Action<AppSettings>? onCommit = null)
    {
        var appEvent = new AppEvent();
        if (onCommit is not null)
        {
            appEvent.Register(AppEvents.Commit, onCommit);
        }

        return new SettingsWindowViewModel(
            settings ?? new AppSettings(),
            appEvent,
            () => ThemeOptions);
    }

    // ── Initial state ──────────────────────────────────────

    [Fact]
    public void InitialValues_ReflectSettingsPassedIn()
    {
        var settings = new AppSettings
        {
            ThemeId = ThemePaletteConfigurationLoader.DarkThemeId,
            CloseOnLaunch = false,
            CheckForUpdatesOnStartup = false,
            UpdateInstallBehavior = UpdateInstallBehavior.NotifyOnly,
            DebugUpdate = true,
            FetchHttpIcons = false,
            AllowPrivateNetworkHttpIconRequests = true,
            Language = LanguageOption.Japanese,
            CategorySortMode = CategorySortMode.AsAdded,
        };

        var vm = Make(settings);

        Assert.Equal(2, vm.ThemeIndex);
        Assert.False(vm.CloseOnLaunch);
        Assert.False(vm.CheckForUpdatesOnStartup);
        Assert.Equal((int)UpdateInstallBehavior.NotifyOnly, vm.UpdateInstallBehaviorIndex);
        Assert.True(vm.DebugUpdate);
        Assert.False(vm.FetchHttpIcons);
        Assert.True(vm.AllowPrivateNetworkHttpIconRequests);
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

        vm.ThemeIndex = 2;

        Assert.Contains(nameof(vm.ThemeIndex), raised);
    }

    [Fact]
    public void ThemeIndex_Change_CallsOnCommitWithNewTheme()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.ThemeIndex = 1;

        Assert.NotNull(committed);
        Assert.Equal(ThemePaletteConfigurationLoader.LightThemeId, committed!.ThemeId);
    }

    [Fact]
    public void ThemeIndex_Change_CallsOnCommitWithMonochromeTheme()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.ThemeIndex = 3;

        Assert.NotNull(committed);
        Assert.Equal("monochrome", committed.ThemeId);
    }

    [Fact]
    public void ThemeIndex_SameValue_DoesNotSave()
    {
        AppSettings? committed = null;
        var vm = Make(settings: new AppSettings { ThemeId = ThemePaletteConfigurationLoader.LightThemeId }, onCommit: s => committed = s);

        vm.ThemeIndex = 1;

        Assert.Null(committed);
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
        AppSettings? committed = null;
        var vm = Make(settings: new AppSettings { CloseOnLaunch = true }, onCommit: s => committed = s);

        vm.CloseOnLaunch = true;

        Assert.Null(committed);
    }

    // ── Commit ─────────────────────────────────────────────

    [Fact]
    public void Commit_SetsSettingsChanged()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.DebugUpdate = true;

        Assert.True(vm.SettingsChanged);
        Assert.NotNull(committed);
    }

    [Fact]
    public void Commit_SavesAllCurrentValues()
    {
        AppSettings? committed = null;
        var vm = Make(
            settings: new AppSettings { ThemeId = ThemePaletteConfigurationLoader.SystemThemeId, CloseOnLaunch = true, CheckForUpdatesOnStartup = true, DebugUpdate = false },
            onCommit: s => committed = s);

        vm.ThemeIndex = 2;
        vm.CloseOnLaunch = false;

        var last = Assert.IsType<AppSettings>(committed);
        Assert.Equal(ThemePaletteConfigurationLoader.DarkThemeId, last.ThemeId);
        Assert.False(last.CloseOnLaunch);
    }

    [Fact]
    public void PostLaunchBehaviorIndex_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.PostLaunchBehaviorIndex = (int)PostLaunchBehavior.MinimizeWindow;

        var last = Assert.IsType<AppSettings>(committed);
        Assert.Equal(PostLaunchBehavior.MinimizeWindow, last.PostLaunchBehavior);
        Assert.False(last.CloseOnLaunch);
    }

    [Fact]
    public void LanguageIndex_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.LanguageIndex = (int)LanguageOption.English;

        Assert.Equal(LanguageOption.English, committed!.Language);
    }

    [Fact]
    public void UpdateInstallBehaviorIndex_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.UpdateInstallBehaviorIndex = (int)UpdateInstallBehavior.AutomaticallyApply;

        Assert.Equal(UpdateInstallBehavior.AutomaticallyApply, committed!.UpdateInstallBehavior);
    }

    [Fact]
    public void LanguageIndex_Change_ReloadsThemeOptionsImmediately()
    {
        var appEvent = new AppEvent();
        var providerCallCount = 0;
        var culturePhase = "before";

        appEvent.Register(AppEvents.Commit, _ => culturePhase = "after");

        IReadOnlyList<ThemeOption> ThemeOptionsProvider()
        {
            providerCallCount++;
            return culturePhase == "before"
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
            ThemeOptionsProvider);

        vm.LanguageIndex = (int)LanguageOption.Japanese;

        Assert.Equal(2, providerCallCount);
        Assert.Equal("after", culturePhase);
        Assert.Equal("システム", vm.ThemeOptions[0].DisplayName);
    }

    [Fact]
    public void LaunchAtWindowsStartup_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.LaunchAtWindowsStartup = true;

        Assert.True(committed!.LaunchAtWindowsStartup);
    }

    [Fact]
    public void ConfirmBeforeDelete_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.ConfirmBeforeDelete = true;

        Assert.True(committed!.ConfirmBeforeDelete);
    }

    [Fact]
    public void FetchHttpIcons_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.FetchHttpIcons = false;

        Assert.False(committed!.FetchHttpIcons);
    }

    [Fact]
    public void AllowPrivateNetworkHttpIconRequests_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.AllowPrivateNetworkHttpIconRequests = true;

        Assert.True(committed!.AllowPrivateNetworkHttpIconRequests);
    }

    [Fact]
    public void AppListSortModeIndex_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.AppListSortModeIndex = (int)AppListSortMode.CategoryThenName;

        Assert.Equal(AppListSortMode.CategoryThenName, committed!.AppListSortMode);
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
            ThemeId = ThemePaletteConfigurationLoader.DarkThemeId,
            Language = LanguageOption.Japanese,
            CloseOnLaunch = false,
            CheckForUpdatesOnStartup = false,
            UpdateInstallBehavior = UpdateInstallBehavior.NotifyOnly,
            DebugUpdate = true,
            StartMinimizedOnLaunch = true,
            LaunchAtWindowsStartup = true,
            FetchHttpIcons = false,
            AllowPrivateNetworkHttpIconRequests = true,
            ConfirmBeforeLaunch = true,
            ConfirmBeforeDelete = true,
            CategorySortMode = CategorySortMode.AsAdded,
            AppListSortMode = AppListSortMode.Name,
            RunAsAdministrator = true,
        };
        var vm = Make(settings: settings);

        vm.ResetToDefaults();

        Assert.Equal(0, vm.ThemeIndex);
        Assert.Equal((int)defaults.Language, vm.LanguageIndex);
        Assert.Equal(defaults.CloseOnLaunch, vm.CloseOnLaunch);
        Assert.Equal(defaults.CheckForUpdatesOnStartup, vm.CheckForUpdatesOnStartup);
        Assert.Equal((int)defaults.UpdateInstallBehavior, vm.UpdateInstallBehaviorIndex);
        Assert.Equal(defaults.DebugUpdate, vm.DebugUpdate);
        Assert.Equal(defaults.StartMinimizedOnLaunch, vm.StartMinimizedOnLaunch);
        Assert.Equal(defaults.LaunchAtWindowsStartup, vm.LaunchAtWindowsStartup);
        Assert.Equal(defaults.FetchHttpIcons, vm.FetchHttpIcons);
        Assert.Equal(defaults.AllowPrivateNetworkHttpIconRequests, vm.AllowPrivateNetworkHttpIconRequests);
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
            settings: new AppSettings { ThemeId = ThemePaletteConfigurationLoader.DarkThemeId },
            onCommit: s => committed = s);
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.ResetToDefaults();

        Assert.NotNull(committed);
        Assert.Equal(ThemePaletteConfigurationLoader.SystemThemeId, committed!.ThemeId);
        Assert.Contains(nameof(vm.ThemeIndex), raised);
        Assert.Contains(nameof(vm.LanguageIndex), raised);
    }

    [Fact]
    public void ApplyExternalSettings_UpdatesValuesWithoutSaving()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);
        var refreshed = new AppSettings
        {
            ThemeId = ThemePaletteConfigurationLoader.DarkThemeId,
            PostLaunchBehavior = PostLaunchBehavior.MinimizeWindow,
            CloseOnLaunch = false,
            CheckForUpdatesOnStartup = false,
            FetchHttpIcons = false,
            AllowPrivateNetworkHttpIconRequests = true,
            Language = LanguageOption.Japanese,
        };

        vm.ApplyExternalSettings(refreshed);

        Assert.Equal(2, vm.ThemeIndex);
        Assert.Equal((int)PostLaunchBehavior.MinimizeWindow, vm.PostLaunchBehaviorIndex);
        Assert.False(vm.CloseOnLaunch);
        Assert.False(vm.CheckForUpdatesOnStartup);
        Assert.False(vm.FetchHttpIcons);
        Assert.True(vm.AllowPrivateNetworkHttpIconRequests);
        Assert.Equal((int)LanguageOption.Japanese, vm.LanguageIndex);
        Assert.Null(committed);
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
            ThemeOptionsProvider);

        vm.ApplyExternalSettings(new AppSettings { Language = LanguageOption.Japanese });

        Assert.Equal(2, providerCallCount);
        Assert.Equal("システム", vm.ThemeOptions[0].DisplayName);
    }
}
