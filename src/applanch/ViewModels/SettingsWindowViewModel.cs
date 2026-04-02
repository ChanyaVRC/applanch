using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;

namespace applanch;

internal sealed class SettingsWindowViewModel : ObservableObject
{
    private readonly AppEvent _appEvent;
    private readonly Action<AppSettings> _save;
    private readonly Func<IReadOnlyList<ThemeOption>> _themeOptionsProvider;
    private IReadOnlyList<ThemeOption> _themeOptions;
    private AppSettings _current;
    private AppTheme _themeMode;
    private string? _themeId;
    private PostLaunchBehavior _postLaunchBehavior;
    private bool _closeOnLaunch;
    private bool _checkForUpdatesOnStartup;
    private bool _debugUpdate;
    private bool _startMinimizedOnLaunch;
    private bool _launchAtWindowsStartup;
    private bool _confirmBeforeLaunch;
    private bool _confirmBeforeDelete;
    private CategorySortMode _categorySortMode;
    private AppListSortMode _appListSortMode;
    private bool _runAsAdministrator;
    private LanguageOption _language;

    internal SettingsWindowViewModel(
        AppSettings settings,
        AppEvent appEvent,
        Action<AppSettings>? save = null,
        Func<IReadOnlyList<ThemeOption>>? themeOptionsProvider = null)
    {
        _appEvent = appEvent;
        _save = save ?? (static s => s.Save());
        _themeOptionsProvider = themeOptionsProvider ?? ThemeOptionsProvider.Load;
        _themeOptions = _themeOptionsProvider();
        _current = settings;
        LoadFields(settings);
    }

    public IReadOnlyList<ThemeOption> ThemeOptions => _themeOptions;

    public bool IsThemeSelectionVisible => _themeOptions.Count > 0;

    public int ThemeIndex
    {
        get => ResolveThemeIndex();
        set
        {
            if (!IsThemeSelectionVisible || value < 0 || value >= _themeOptions.Count)
            {
                return;
            }

            var selected = _themeOptions[value];
            var selectedThemeMode = selected.IsSystemOption
                ? AppTheme.System
                : string.Equals(selected.ThemeId, ThemePaletteConfigurationLoader.DarkThemeId, StringComparison.OrdinalIgnoreCase)
                    ? AppTheme.Dark
                    : AppTheme.Light;
            var selectedThemeId = selected.IsSystemOption ||
                string.Equals(selected.ThemeId, ThemePaletteConfigurationLoader.LightThemeId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(selected.ThemeId, ThemePaletteConfigurationLoader.DarkThemeId, StringComparison.OrdinalIgnoreCase)
                ? null
                : selected.ThemeId;

            if (_themeMode == selectedThemeMode &&
                string.Equals(_themeId, selectedThemeId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _themeMode = selectedThemeMode;
            _themeId = selectedThemeId;
            OnPropertyChanged();
            Commit();
        }
    }

    public bool CloseOnLaunch
    {
        get => _closeOnLaunch;
        set
        {
            if (_closeOnLaunch == value)
            {
                return;
            }

            _closeOnLaunch = value;
            _postLaunchBehavior = value ? PostLaunchBehavior.CloseApp : PostLaunchBehavior.KeepOpen;
            OnPropertyChanged();
            Commit();
        }
    }

    public int PostLaunchBehaviorIndex
    {
        get => (int)_postLaunchBehavior;
        set
        {
            if ((int)_postLaunchBehavior == value)
            {
                return;
            }

            _postLaunchBehavior = (PostLaunchBehavior)value;
            _closeOnLaunch = _postLaunchBehavior == PostLaunchBehavior.CloseApp;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CloseOnLaunch));
            Commit();
        }
    }

    public bool CheckForUpdatesOnStartup
    {
        get => _checkForUpdatesOnStartup;
        set => SetFieldAndCommit(ref _checkForUpdatesOnStartup, value);
    }

    public bool DebugUpdate
    {
        get => _debugUpdate;
        set => SetFieldAndCommit(ref _debugUpdate, value);
    }

    public bool StartMinimizedOnLaunch
    {
        get => _startMinimizedOnLaunch;
        set => SetFieldAndCommit(ref _startMinimizedOnLaunch, value);
    }

    public bool LaunchAtWindowsStartup
    {
        get => _launchAtWindowsStartup;
        set => SetFieldAndCommit(ref _launchAtWindowsStartup, value);
    }

    public bool ConfirmBeforeLaunch
    {
        get => _confirmBeforeLaunch;
        set => SetFieldAndCommit(ref _confirmBeforeLaunch, value);
    }

    public bool ConfirmBeforeDelete
    {
        get => _confirmBeforeDelete;
        set => SetFieldAndCommit(ref _confirmBeforeDelete, value);
    }

    public int CategorySortModeIndex
    {
        get => (int)_categorySortMode;
        set => SetFieldAndCommit(ref _categorySortMode, (CategorySortMode)value);
    }

    public int AppListSortModeIndex
    {
        get => (int)_appListSortMode;
        set => SetFieldAndCommit(ref _appListSortMode, (AppListSortMode)value);
    }

    public bool RunAsAdministrator
    {
        get => _runAsAdministrator;
        set => SetFieldAndCommit(ref _runAsAdministrator, value);
    }

    public int LanguageIndex
    {
        get => (int)_language;
        set => SetFieldAndCommit(ref _language, (LanguageOption)value);
    }

    public bool SettingsChanged { get; private set; }

    internal void ApplyExternalSettings(AppSettings settings)
    {
        var languageChanged = _language != settings.Language;
        _current = settings;
        LoadFields(settings);

        if (languageChanged)
        {
            ReloadThemeOptions();
        }

        NotifyAllProperties();
    }

    internal void ResetToDefaults()
    {
        var defaults = new AppSettings();
        var languageChanged = _language != defaults.Language;
        LoadFields(defaults);

        if (languageChanged)
        {
            ReloadThemeOptions();
        }

        NotifyAllProperties();
        Commit();
    }

    private void LoadFields(AppSettings settings)
    {
        _themeMode = settings.Theme;
        _themeId = settings.ThemeId;
        _postLaunchBehavior = settings.ResolvePostLaunchBehavior();
        _closeOnLaunch = settings.CloseOnLaunch;
        _checkForUpdatesOnStartup = settings.CheckForUpdatesOnStartup;
        _debugUpdate = settings.DebugUpdate;
        _startMinimizedOnLaunch = settings.StartMinimizedOnLaunch;
        _launchAtWindowsStartup = settings.LaunchAtWindowsStartup;
        _confirmBeforeLaunch = settings.ConfirmBeforeLaunch;
        _confirmBeforeDelete = settings.ConfirmBeforeDelete;
        _categorySortMode = settings.CategorySortMode;
        _appListSortMode = settings.AppListSortMode;
        _runAsAdministrator = settings.RunAsAdministrator;
        _language = settings.Language;
    }

    private void NotifyAllProperties()
    {
        OnPropertyChanged(nameof(ThemeIndex));
        OnPropertyChanged(nameof(PostLaunchBehaviorIndex));
        OnPropertyChanged(nameof(CloseOnLaunch));
        OnPropertyChanged(nameof(CheckForUpdatesOnStartup));
        OnPropertyChanged(nameof(DebugUpdate));
        OnPropertyChanged(nameof(StartMinimizedOnLaunch));
        OnPropertyChanged(nameof(LaunchAtWindowsStartup));
        OnPropertyChanged(nameof(ConfirmBeforeLaunch));
        OnPropertyChanged(nameof(ConfirmBeforeDelete));
        OnPropertyChanged(nameof(CategorySortModeIndex));
        OnPropertyChanged(nameof(AppListSortModeIndex));
        OnPropertyChanged(nameof(RunAsAdministrator));
        OnPropertyChanged(nameof(LanguageIndex));
    }

    private bool SetFieldAndCommit<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (!SetField(ref field, value, propertyName))
        {
            return false;
        }

        Commit();
        return true;
    }

    private void Commit()
    {
        _current = _current with
        {
            Theme = _themeMode,
            ThemeId = _themeId,
            PostLaunchBehavior = _postLaunchBehavior,
            CloseOnLaunch = _closeOnLaunch,
            CheckForUpdatesOnStartup = _checkForUpdatesOnStartup,
            DebugUpdate = _debugUpdate,
            StartMinimizedOnLaunch = _startMinimizedOnLaunch,
            LaunchAtWindowsStartup = _launchAtWindowsStartup,
            ConfirmBeforeLaunch = _confirmBeforeLaunch,
            ConfirmBeforeDelete = _confirmBeforeDelete,
            CategorySortMode = _categorySortMode,
            AppListSortMode = _appListSortMode,
            RunAsAdministrator = _runAsAdministrator,
            Language = _language,
        };
        _save(_current);
        SettingsChanged = true;
        _appEvent.Invoke(AppEvents.Commit, _current);
        _appEvent.Invoke(AppEvents.Refresh, _current);
    }

    private void ReloadThemeOptions()
    {
        _themeOptions = _themeOptionsProvider();
        OnPropertyChanged(nameof(ThemeOptions));
        OnPropertyChanged(nameof(IsThemeSelectionVisible));
    }

    private int ResolveThemeIndex()
    {
        if (!IsThemeSelectionVisible)
        {
            return -1;
        }

        if (_themeMode == AppTheme.System)
        {
            return _themeOptions
                .Select(static (option, index) => (option, index))
                .FirstOrDefault(static x => x.Item1.IsSystemOption, (_themeOptions[0], 0))
                .Item2;
        }

        var targetThemeId = !string.IsNullOrWhiteSpace(_themeId)
            ? _themeId
            : _themeMode == AppTheme.Dark
                ? ThemePaletteConfigurationLoader.DarkThemeId
                : ThemePaletteConfigurationLoader.LightThemeId;

        return _themeOptions
            .Select(static (option, index) => (option, index))
            .FirstOrDefault(x => string.Equals(x.Item1.ThemeId, targetThemeId, StringComparison.OrdinalIgnoreCase), (_themeOptions[0], 0))
            .Item2;
    }
}
