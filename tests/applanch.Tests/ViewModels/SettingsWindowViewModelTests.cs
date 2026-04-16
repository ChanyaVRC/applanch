using Xunit;
using System.Globalization;
using applanch.Events;
using applanch.Infrastructure.Storage;
using applanch.Theming;
using applanch.Infrastructure.Updates;
using applanch.Tests.TestSupport;
using applanch.ViewModels;

namespace applanch.Tests;

public class SettingsWindowViewModelTests
{
    private static readonly ThemeOption[] ThemeOptionsList =
    [
        new ThemeOption(ThemePaletteConfigurationLoader.SystemThemeId, new LocalizedText("System"), IsSystemOption: true),
        new ThemeOption(ThemePaletteConfigurationLoader.LightThemeId, new LocalizedText("Light")),
        new ThemeOption(ThemePaletteConfigurationLoader.DarkThemeId, new LocalizedText("Dark")),
        new ThemeOption("monochrome", new LocalizedText("Monochrome"))
    ];

    private static IReadOnlyDictionary<string, ThemeOption> ThemeOptions =>
        ThemeOptionsList.ToDictionary(x => x.ThemeId);

    private static SettingsWindowViewModel Make(
        AppSettings? settings = null,
        Action<AppSettings>? onCommit = null,
        Func<AppSettings, IAppUpdateService>? updateServiceFactory = null)
    {
        var appEvent = AppEventFactory.Create();
        if (onCommit is not null)
        {
            appEvent.Register(AppEvents.Commit, onCommit);
        }

        return new SettingsWindowViewModel(
            settings ?? new AppSettings(),
            appEvent,
            () => ThemeOptions,
            updateServiceFactory);
    }

    // ── Initial state ──────────────────────────────────────

    [Fact]
    public void InitialValues_ReflectSettingsPassedIn()
    {
        var settings = new AppSettings
        {
            ThemeId = ThemePaletteConfigurationLoader.DarkThemeId,
            QuickAddSuggestionLimit = 100,
            PostLaunchBehavior = PostLaunchBehavior.KeepOpen,
            CheckForUpdatesOnStartup = false,
            UpdateInstallBehavior = UpdateInstallBehavior.NotifyOnly,
            AllowPrereleaseUpdates = true,
            DebugUpdate = true,
            RegisterContextMenuOnStartup = false,
            FetchHttpIcons = false,
            AllowPrivateNetworkHttpIconRequests = true,
            Language = LanguageOption.Japanese,
            CategorySortMode = CategorySortMode.AsAdded,
            LaunchItemIconOnlyMode = true,
        };

        var vm = Make(settings);

        Assert.Equal(2, vm.ThemeIndex);
        Assert.Equal(100, vm.QuickAddSuggestionLimit);
        Assert.Equal(PostLaunchBehavior.KeepOpen, vm.SelectedPostLaunchBehavior);
        Assert.False(vm.CheckForUpdatesOnStartup);
        Assert.Equal(UpdateInstallBehavior.NotifyOnly, vm.SelectedUpdateInstallBehavior);
        Assert.True(vm.AllowPrereleaseUpdates);
        Assert.True(vm.DebugUpdate);
        Assert.False(vm.RegisterContextMenuOnStartup);
        Assert.False(vm.FetchHttpIcons);
        Assert.True(vm.AllowPrivateNetworkHttpIconRequests);
        Assert.Equal(LanguageOption.Japanese, vm.SelectedLanguage);
        Assert.Equal(CategorySortMode.AsAdded, vm.SelectedCategorySortMode);
        Assert.True(vm.LaunchItemIconOnlyMode);
        Assert.False(vm.SettingsChanged);
    }

    [Fact]
    public async Task RefreshAvailableUpdatesAsync_LoadsAvailableVersionsAndSelectsFirst()
    {
        var expected = new[]
        {
            new AppUpdateInfo(SemanticVersion.Parse("2.0.0"), SemanticVersion.Parse("1.0.0"), new Uri("https://example.com/2.zip"), new Uri("https://example.com/r2")),
            new AppUpdateInfo(SemanticVersion.Parse("1.5.0"), SemanticVersion.Parse("1.0.0"), new Uri("https://example.com/15.zip"), new Uri("https://example.com/r15")),
        };
        var vm = Make(updateServiceFactory: _ => new FakeAppUpdateService { AvailableUpdates = expected });

        await vm.RefreshAvailableUpdatesAsync();

        Assert.Equal(expected, vm.AvailableUpdates);
        Assert.Equal(expected[0], vm.SelectedAvailableUpdate);
        Assert.Equal(string.Empty, vm.AvailableUpdatesStatusMessage);
    }

    [Fact]
    public async Task RefreshAvailableUpdatesAsync_WhenNoneAvailable_SetsStatusMessage()
    {
        var vm = Make(updateServiceFactory: _ => new FakeAppUpdateService());

        await vm.RefreshAvailableUpdatesAsync();

        Assert.Empty(vm.AvailableUpdates);
        Assert.Equal(AppResources.UpdateVersions_NoneAvailable, vm.AvailableUpdatesStatusMessage);
        Assert.False(vm.CanApplySelectedUpdate);
    }

    [Fact]
    public async Task RefreshAvailableUpdatesAsync_UsesLatestAllowPrereleaseSetting_WhenRecreatingService()
    {
        var createdSettings = new List<AppSettings>();
        var vm = Make(
            settings: new AppSettings { AllowPrereleaseUpdates = false },
            updateServiceFactory: settings =>
            {
                createdSettings.Add(settings);
                return new FakeAppUpdateService();
            });

        vm.AllowPrereleaseUpdates = true;
        await vm.RefreshAvailableUpdatesAsync();

        Assert.NotEmpty(createdSettings);
        Assert.True(createdSettings[^1].AllowPrereleaseUpdates);
    }

    [Fact]
    public async Task ApplySelectedUpdateAsync_UsesSelectedAvailableUpdate()
    {
        var fakeService = new FakeAppUpdateService
        {
            AvailableUpdates =
            [
                new AppUpdateInfo(SemanticVersion.Parse("2.0.0"), SemanticVersion.Parse("1.0.0"), new Uri("https://example.com/2.zip"), new Uri("https://example.com/r2")),
            ],
        };
        var vm = Make(updateServiceFactory: _ => fakeService);
        await vm.RefreshAvailableUpdatesAsync();

        var result = await vm.ApplySelectedUpdateAsync();

        Assert.NotNull(result);
        Assert.True(result is { IsSuccess: true });
        Assert.Equal(SemanticVersion.Parse("2.0.0"), fakeService.LastAppliedUpdate!.NewVersion);
    }

    [Fact]
    public async Task ApplySelectedUpdateAsync_DoesNotStartSecondApply_WhileApplyInProgress()
    {
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fakeService = new FakeAppUpdateService
        {
            AvailableUpdates =
            [
                new AppUpdateInfo(SemanticVersion.Parse("2.0.0"), SemanticVersion.Parse("1.0.0"), new Uri("https://example.com/2.zip"), new Uri("https://example.com/r2")),
            ],
            ApplyGate = gate,
        };
        var vm = Make(updateServiceFactory: _ => fakeService);
        await vm.RefreshAvailableUpdatesAsync();

        var firstApplyTask = vm.ApplySelectedUpdateAsync();
        var secondApplyResult = await vm.ApplySelectedUpdateAsync();

        Assert.Null(secondApplyResult);
        Assert.Equal(1, fakeService.ApplyCallCount);

        gate.SetResult(true);
        var firstApplyResult = await firstApplyTask;

        Assert.True(firstApplyResult is { IsSuccess: true });
    }

    [Fact]
    public async Task ApplySelectedUpdateAsync_ChangesApplyButtonText_WhileApplyInProgress()
    {
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fakeService = new FakeAppUpdateService
        {
            AvailableUpdates =
            [
                new AppUpdateInfo(SemanticVersion.Parse("2.0.0"), SemanticVersion.Parse("1.0.0"), new Uri("https://example.com/2.zip"), new Uri("https://example.com/r2")),
            ],
            ApplyGate = gate,
        };
        var vm = Make(updateServiceFactory: _ => fakeService);
        await vm.RefreshAvailableUpdatesAsync();

        var applyTask = vm.ApplySelectedUpdateAsync();

        Assert.Equal(AppResources.Button_ApplyingSelectedVersion, vm.ApplySelectedVersionButtonText);

        gate.SetResult(true);
        await applyTask;

        Assert.Equal(AppResources.Button_ApplySelectedVersion, vm.ApplySelectedVersionButtonText);
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
            settings: new AppSettings { ThemeId = ThemePaletteConfigurationLoader.SystemThemeId, PostLaunchBehavior = PostLaunchBehavior.CloseApp, CheckForUpdatesOnStartup = true, DebugUpdate = false },
            onCommit: s => committed = s);

        vm.ThemeIndex = 2;
        vm.SelectedPostLaunchBehavior = PostLaunchBehavior.KeepOpen;

        var last = Assert.IsType<AppSettings>(committed);
        Assert.Equal(ThemePaletteConfigurationLoader.DarkThemeId, last.ThemeId);
        Assert.Equal(PostLaunchBehavior.KeepOpen, last.PostLaunchBehavior);
    }

    [Fact]
    public void SelectedPostLaunchBehavior_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.SelectedPostLaunchBehavior = PostLaunchBehavior.MinimizeWindow;

        var last = Assert.IsType<AppSettings>(committed);
        Assert.Equal(PostLaunchBehavior.MinimizeWindow, last.PostLaunchBehavior);
    }

    [Fact]
    public void QuickAddSuggestionLimit_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.QuickAddSuggestionLimit = 20;

        Assert.Equal(20, committed!.QuickAddSuggestionLimit);
    }

    [Fact]
    public void SelectedLanguage_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.SelectedLanguage = LanguageOption.English;

        Assert.Equal(LanguageOption.English, committed!.Language);
    }

    [Fact]
    public void SelectedUpdateInstallBehavior_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.SelectedUpdateInstallBehavior = UpdateInstallBehavior.AutomaticallyApply;

        Assert.Equal(UpdateInstallBehavior.AutomaticallyApply, committed!.UpdateInstallBehavior);
    }

    [Fact]
    public void AllowPrereleaseUpdates_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.AllowPrereleaseUpdates = true;

        Assert.True(committed!.AllowPrereleaseUpdates);
    }

    [Fact]
    public void SelectedLanguage_Change_UpdatesThemeOptionDisplayNamesWithoutReloadingProvider()
    {
        var appEvent = AppEventFactory.Create();
        var providerCallCount = 0;

        appEvent.Register(AppEvents.Commit, payload =>
        {
            var settings = Assert.IsType<AppSettings>(payload);
            var cultureName = settings.Language == LanguageOption.Japanese ? "ja-JP" : "en-US";
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
        });

        IReadOnlyDictionary<string, ThemeOption> ThemeOptionsProvider()
        {
            providerCallCount++;
            var options = new[]
            {
                new ThemeOption(
                    ThemePaletteConfigurationLoader.SystemThemeId,
                    new LocalizedText(
                        "System",
                        new Dictionary<LanguageOption, string>
                        {
                            [LanguageOption.Japanese] = "システム"
                        }),
                    IsSystemOption: true),
                new ThemeOption(
                    ThemePaletteConfigurationLoader.LightThemeId,
                    new LocalizedText(
                        "Light",
                        new Dictionary<LanguageOption, string>
                        {
                            [LanguageOption.Japanese] = "ライト"
                        }))
            };
            return options.ToDictionary(x => x.ThemeId);
        }

        using var cultureScope = new CultureScope("en-US");

        var vm = new SettingsWindowViewModel(new AppSettings { Language = LanguageOption.English }, appEvent, ThemeOptionsProvider);

        vm.SelectedLanguage = LanguageOption.Japanese;

        Assert.Equal(1, providerCallCount);
        Assert.Equal("システム", vm.ThemeOptions.First().DisplayName);
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
    public void RegisterContextMenuOnStartup_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.RegisterContextMenuOnStartup = false;

        Assert.False(committed!.RegisterContextMenuOnStartup);
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
    public void SelectedAppListSortMode_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.SelectedAppListSortMode = AppListSortMode.CategoryThenName;

        Assert.Equal(AppListSortMode.CategoryThenName, committed!.AppListSortMode);
    }

    [Fact]
    public void LaunchItemIconOnlyMode_Change_UpdatesSavedSettings()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.LaunchItemIconOnlyMode = true;

        Assert.True(committed!.LaunchItemIconOnlyMode);
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
            PostLaunchBehavior = PostLaunchBehavior.KeepOpen,
            CheckForUpdatesOnStartup = false,
            UpdateInstallBehavior = UpdateInstallBehavior.NotifyOnly,
            AllowPrereleaseUpdates = true,
            DebugUpdate = true,
            StartMinimizedOnLaunch = true,
            LaunchAtWindowsStartup = true,
            RegisterContextMenuOnStartup = false,
            FetchHttpIcons = false,
            AllowPrivateNetworkHttpIconRequests = true,
            ConfirmBeforeLaunch = true,
            ConfirmBeforeDelete = true,
            QuickAddSuggestionLimit = 10,
            CategorySortMode = CategorySortMode.AsAdded,
            AppListSortMode = AppListSortMode.Name,
            LaunchItemIconOnlyMode = true,
            RunAsAdministrator = true,
        };
        var vm = Make(settings: settings);

        vm.ResetToDefaults();

        Assert.Equal(0, vm.ThemeIndex);
        Assert.Equal(defaults.Language, vm.SelectedLanguage);
        Assert.Equal(PostLaunchBehavior.CloseApp, vm.SelectedPostLaunchBehavior);
        Assert.Equal(defaults.CheckForUpdatesOnStartup, vm.CheckForUpdatesOnStartup);
        Assert.Equal(defaults.UpdateInstallBehavior, vm.SelectedUpdateInstallBehavior);
        Assert.Equal(defaults.AllowPrereleaseUpdates, vm.AllowPrereleaseUpdates);
        Assert.Equal(defaults.DebugUpdate, vm.DebugUpdate);
        Assert.Equal(defaults.StartMinimizedOnLaunch, vm.StartMinimizedOnLaunch);
        Assert.Equal(defaults.LaunchAtWindowsStartup, vm.LaunchAtWindowsStartup);
        Assert.Equal(defaults.RegisterContextMenuOnStartup, vm.RegisterContextMenuOnStartup);
        Assert.Equal(defaults.FetchHttpIcons, vm.FetchHttpIcons);
        Assert.Equal(defaults.AllowPrivateNetworkHttpIconRequests, vm.AllowPrivateNetworkHttpIconRequests);
        Assert.Equal(defaults.ConfirmBeforeLaunch, vm.ConfirmBeforeLaunch);
        Assert.Equal(defaults.ConfirmBeforeDelete, vm.ConfirmBeforeDelete);
        Assert.Equal(defaults.QuickAddSuggestionLimit, vm.QuickAddSuggestionLimit);
        Assert.Equal(defaults.CategorySortMode, vm.SelectedCategorySortMode);
        Assert.Equal(defaults.AppListSortMode, vm.SelectedAppListSortMode);
        Assert.Equal(defaults.LaunchItemIconOnlyMode, vm.LaunchItemIconOnlyMode);
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
        Assert.Contains(string.Empty, raised);
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
            CheckForUpdatesOnStartup = false,
            AllowPrereleaseUpdates = true,
            RegisterContextMenuOnStartup = false,
            FetchHttpIcons = false,
            AllowPrivateNetworkHttpIconRequests = true,
            Language = LanguageOption.Japanese,
        };

        vm.ApplyExternalSettings(new AppRefreshPayload(new AppSettings(), refreshed));

        Assert.Equal(2, vm.ThemeIndex);
        Assert.Equal(PostLaunchBehavior.MinimizeWindow, vm.SelectedPostLaunchBehavior);
        Assert.False(vm.CheckForUpdatesOnStartup);
        Assert.True(vm.AllowPrereleaseUpdates);
        Assert.False(vm.RegisterContextMenuOnStartup);
        Assert.False(vm.FetchHttpIcons);
        Assert.True(vm.AllowPrivateNetworkHttpIconRequests);
        Assert.Equal(LanguageOption.Japanese, vm.SelectedLanguage);
        Assert.Null(committed);
    }

    [Fact]
    public void ApplyExternalSettings_WhenLanguageChanges_UpdatesThemeOptionDisplayNamesWithoutReloadingProvider()
    {
        var appEvent = AppEventFactory.Create();
        var providerCallCount = 0;

        IReadOnlyDictionary<string, ThemeOption> ThemeOptionsProvider()
        {
            providerCallCount++;
            var options = new[]
            {
                new ThemeOption(
                    ThemePaletteConfigurationLoader.SystemThemeId,
                    new LocalizedText(
                        "System",
                        new Dictionary<LanguageOption, string>
                        {
                            [LanguageOption.Japanese] = "システム"
                        }),
                    IsSystemOption: true),
                new ThemeOption(
                    ThemePaletteConfigurationLoader.LightThemeId,
                    new LocalizedText(
                        "Light",
                        new Dictionary<LanguageOption, string>
                        {
                            [LanguageOption.Japanese] = "ライト"
                        }))
            };
            return options.ToDictionary(x => x.ThemeId);
        }

        using var cultureScope = new CultureScope("en-US");

        var vm = new SettingsWindowViewModel(new AppSettings { Language = LanguageOption.English }, appEvent, ThemeOptionsProvider);

        var japaneseCulture = new CultureInfo("ja-JP");
        CultureInfo.CurrentUICulture = japaneseCulture;
        CultureInfo.CurrentCulture = japaneseCulture;

        vm.ApplyExternalSettings(new AppRefreshPayload(
            new AppSettings { Language = LanguageOption.English },
            new AppSettings { Language = LanguageOption.Japanese }));

        Assert.Equal(1, providerCallCount);
        Assert.Equal("システム", vm.ThemeOptions.First().DisplayName);
    }

    private sealed class FakeAppUpdateService : IAppUpdateService
    {
        internal IReadOnlyList<AppUpdateInfo> AvailableUpdates { get; init; } = [];
        internal TaskCompletionSource<bool>? ApplyGate { get; init; }
        internal int ApplyCallCount { get; private set; }
        internal AppUpdateInfo? LastAppliedUpdate { get; private set; }

        public Task<IReadOnlyList<AppUpdateInfo>> GetAvailableUpdatesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AvailableUpdates);
        }

        public Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AppUpdateInfo?>(null);
        }

        public Task ApplyUpdateAsync(AppUpdateInfo update, CancellationToken cancellationToken = default)
        {
            LastAppliedUpdate = update;
            ApplyCallCount++;
            return ApplyGate?.Task ?? Task.CompletedTask;
        }
    }
}
