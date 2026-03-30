using Xunit;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;

namespace applanch.Tests;

public class SettingsWindowViewModelTests
{
    private static SettingsWindowViewModel Make(
        AppSettings? settings = null,
        Action? reapplyTheme = null,
        List<AppSettings>? saved = null)
    {
        var captures = saved ?? [];
        return new SettingsWindowViewModel(
            settings ?? new AppSettings(),
            reapplyTheme ?? (() => { }),
            s => captures.Add(s));
    }

    // ── Initial state ──────────────────────────────────────

    [Fact]
    public void InitialValues_ReflectSettingsPassedIn()
    {
        var settings = new AppSettings(
            Theme: AppTheme.Dark,
            CloseOnLaunch: false,
            CheckForUpdatesOnStartup: false,
            DebugUpdate: true);

        var vm = Make(settings);

        Assert.Equal((int)AppTheme.Dark, vm.ThemeIndex);
        Assert.False(vm.CloseOnLaunch);
        Assert.False(vm.CheckForUpdatesOnStartup);
        Assert.True(vm.DebugUpdate);
        Assert.False(vm.SettingsChanged);
        Assert.Null(vm.SavedSettings);
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
    public void ThemeIndex_Change_CallsReapplyTheme()
    {
        var called = false;
        var vm = Make(reapplyTheme: () => called = true);

        vm.ThemeIndex = (int)AppTheme.Light;

        Assert.True(called);
    }

    [Fact]
    public void ThemeIndex_SameValue_DoesNotSave()
    {
        var saved = new List<AppSettings>();
        var vm = Make(settings: new AppSettings(Theme: AppTheme.Light), saved: saved);

        vm.ThemeIndex = (int)AppTheme.Light;

        Assert.Empty(saved);
    }

    // ── CloseOnLaunch ──────────────────────────────────────

    [Fact]
    public void CloseOnLaunch_Change_FiresPropertyChanged()
    {
        var vm = Make(settings: new AppSettings(CloseOnLaunch: true));
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.CloseOnLaunch = false;

        Assert.Contains(nameof(vm.CloseOnLaunch), raised);
    }

    [Fact]
    public void CloseOnLaunch_SameValue_DoesNotSave()
    {
        var saved = new List<AppSettings>();
        var vm = Make(settings: new AppSettings(CloseOnLaunch: true), saved: saved);

        vm.CloseOnLaunch = true;

        Assert.Empty(saved);
    }

    // ── Commit ─────────────────────────────────────────────

    [Fact]
    public void Commit_SetsSettingsChangedAndSavedSettings()
    {
        var saved = new List<AppSettings>();
        var vm = Make(saved: saved);

        vm.DebugUpdate = true;

        Assert.True(vm.SettingsChanged);
        Assert.NotNull(vm.SavedSettings);
        Assert.True(vm.SavedSettings!.DebugUpdate);
        Assert.Single(saved);
    }

    [Fact]
    public void Commit_SavesAllCurrentValues()
    {
        var saved = new List<AppSettings>();
        var vm = Make(
            settings: new AppSettings(Theme: AppTheme.System, CloseOnLaunch: true, CheckForUpdatesOnStartup: true, DebugUpdate: false),
            saved: saved);

        vm.ThemeIndex = (int)AppTheme.Dark;
        vm.CloseOnLaunch = false;

        var last = saved.Last();
        Assert.Equal(AppTheme.Dark, last.Theme);
        Assert.False(last.CloseOnLaunch);
    }
}
