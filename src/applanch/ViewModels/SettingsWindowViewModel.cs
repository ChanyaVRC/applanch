using applanch.Events;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;

namespace applanch.ViewModels;

internal sealed class SettingsWindowViewModel : ObservableObject
{
    private readonly AppEvent _appEvent;
    private readonly Func<IReadOnlyList<ThemeOption>> _themeOptionsProvider;
    private IReadOnlyList<ThemeOption> _themeOptions;
    private AppSettings _current;
    private string _themeId = ThemePaletteConfigurationLoader.SystemThemeId;
    private PostLaunchBehavior _postLaunchBehavior;
    private bool _closeOnLaunch;
    private bool _checkForUpdatesOnStartup;
    private bool _debugUpdate;
    private bool _startMinimizedOnLaunch;
    private bool _launchAtWindowsStartup;
    private bool _fetchHttpIcons;
    private bool _allowPrivateNetworkHttpIconRequests;
    private bool _confirmBeforeLaunch;
    private bool _confirmBeforeDelete;
    private CategorySortMode _categorySortMode;
    private AppListSortMode _appListSortMode;
    private bool _runAsAdministrator;
    private LanguageOption _language;

    internal SettingsWindowViewModel(
        AppSettings settings,
        AppEvent appEvent,
        Func<IReadOnlyList<ThemeOption>>? themeOptionsProvider = null)
    {
        _appEvent = appEvent;
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

            SelectedThemeId = _themeOptions[value].ThemeId;
        }
    }

    public string SelectedThemeId
    {
        get => _themeId;
        set
        {
            if (!IsThemeSelectionVisible)
            {
                return;
            }

            var selected = _themeOptions.FirstOrDefault(
                option => string.Equals(option.ThemeId, value, StringComparison.OrdinalIgnoreCase));
            if (selected is null)
            {
                return;
            }

            if (string.Equals(_themeId, selected.ThemeId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _themeId = selected.ThemeId;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ThemeIndex));
            OnPropertyChanged(nameof(SelectedThemeDisplayName));
            Commit();
        }
    }

    public string SelectedThemeDisplayName =>
        _themeOptions.FirstOrDefault(option => string.Equals(option.ThemeId, _themeId, StringComparison.OrdinalIgnoreCase))?.DisplayName
        ?? string.Empty;

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

    public bool FetchHttpIcons
    {
        get => _fetchHttpIcons;
        set => SetFieldAndCommit(ref _fetchHttpIcons, value);
    }

    public bool AllowPrivateNetworkHttpIconRequests
    {
        get => _allowPrivateNetworkHttpIconRequests;
        set => SetFieldAndCommit(ref _allowPrivateNetworkHttpIconRequests, value);
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
        _themeId = settings.ThemeId;
        _postLaunchBehavior = settings.ResolvePostLaunchBehavior();
        _closeOnLaunch = settings.CloseOnLaunch;
        _checkForUpdatesOnStartup = settings.CheckForUpdatesOnStartup;
        _debugUpdate = settings.DebugUpdate;
        _startMinimizedOnLaunch = settings.StartMinimizedOnLaunch;
        _launchAtWindowsStartup = settings.LaunchAtWindowsStartup;
        _fetchHttpIcons = settings.FetchHttpIcons;
        _allowPrivateNetworkHttpIconRequests = settings.AllowPrivateNetworkHttpIconRequests;
        _confirmBeforeLaunch = settings.ConfirmBeforeLaunch;
        _confirmBeforeDelete = settings.ConfirmBeforeDelete;
        _categorySortMode = settings.CategorySortMode;
        _appListSortMode = settings.AppListSortMode;
        _runAsAdministrator = settings.RunAsAdministrator;
        _language = settings.Language;
    }

    private void NotifyAllProperties()
    {
        OnPropertyChanged(nameof(SelectedThemeId));
        OnPropertyChanged(nameof(SelectedThemeDisplayName));
        OnPropertyChanged(nameof(ThemeIndex));
        OnPropertyChanged(nameof(PostLaunchBehaviorIndex));
        OnPropertyChanged(nameof(CloseOnLaunch));
        OnPropertyChanged(nameof(CheckForUpdatesOnStartup));
        OnPropertyChanged(nameof(DebugUpdate));
        OnPropertyChanged(nameof(StartMinimizedOnLaunch));
        OnPropertyChanged(nameof(LaunchAtWindowsStartup));
        OnPropertyChanged(nameof(FetchHttpIcons));
        OnPropertyChanged(nameof(AllowPrivateNetworkHttpIconRequests));
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
        var languageChanged = _current.Language != _language;

        _current = _current with
        {
            ThemeId = _themeId,
            PostLaunchBehavior = _postLaunchBehavior,
            CloseOnLaunch = _closeOnLaunch,
            CheckForUpdatesOnStartup = _checkForUpdatesOnStartup,
            DebugUpdate = _debugUpdate,
            StartMinimizedOnLaunch = _startMinimizedOnLaunch,
            LaunchAtWindowsStartup = _launchAtWindowsStartup,
            FetchHttpIcons = _fetchHttpIcons,
            AllowPrivateNetworkHttpIconRequests = _allowPrivateNetworkHttpIconRequests,
            ConfirmBeforeLaunch = _confirmBeforeLaunch,
            ConfirmBeforeDelete = _confirmBeforeDelete,
            CategorySortMode = _categorySortMode,
            AppListSortMode = _appListSortMode,
            RunAsAdministrator = _runAsAdministrator,
            Language = _language,
        };

        SettingsChanged = true;
        _appEvent.Invoke(AppEvents.Commit, _current);

        if (languageChanged)
        {
            ReloadThemeOptions();
        }
    }

    private void ReloadThemeOptions()
    {
        _themeOptions = _themeOptionsProvider();
        OnPropertyChanged(nameof(ThemeOptions));
        OnPropertyChanged(nameof(IsThemeSelectionVisible));
        OnPropertyChanged(nameof(SelectedThemeId));
        OnPropertyChanged(nameof(SelectedThemeDisplayName));
        OnPropertyChanged(nameof(ThemeIndex));
    }

    private int ResolveThemeIndex()
    {
        if (!IsThemeSelectionVisible)
        {
            return -1;
        }

        return _themeOptions
            .Select(static (option, index) => (option, index))
            .FirstOrDefault(x => string.Equals(x.Item1.ThemeId, _themeId, StringComparison.OrdinalIgnoreCase), (_themeOptions[0], 0))
            .Item2;
    }
}
