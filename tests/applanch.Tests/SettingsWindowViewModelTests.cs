using Xunit;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;

namespace applanch.Tests;

public class SettingsWindowViewModelTests
{
    private static SettingsWindowViewModel Make(
        AppSettings? settings = null,
        Action<AppSettings>? onCommit = null,
        List<AppSettings>? saved = null)
    {
        var captures = saved ?? [];
        return new SettingsWindowViewModel(
            settings ?? new AppSettings(),
            onCommit ?? (_ => { }),
            s => captures.Add(s));
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
    public void ThemeIndex_Change_CallsOnCommitWithNewTheme()
    {
        AppSettings? committed = null;
        var vm = Make(onCommit: s => committed = s);

        vm.ThemeIndex = (int)AppTheme.Light;

        Assert.NotNull(committed);
        Assert.Equal(AppTheme.Light, committed!.Theme);
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
}
