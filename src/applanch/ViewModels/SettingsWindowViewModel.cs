using System.ComponentModel;
using System.Runtime.CompilerServices;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;

namespace applanch;

internal sealed class SettingsWindowViewModel : INotifyPropertyChanged
{
    private readonly Action<AppSettings> _onCommit;
    private readonly Action<AppSettings> _save;
    private AppSettings _current;
    private AppTheme _theme;
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

    internal SettingsWindowViewModel(AppSettings settings, Action<AppSettings> onCommit, Action<AppSettings>? save = null)
    {
        _onCommit = onCommit;
        _save = save ?? (static s => s.Save());
        _current = settings;
        _theme = settings.Theme;
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

    public int ThemeIndex
    {
        get => (int)_theme;
        set
        {
            if ((int)_theme == value)
            {
                return;
            }

            _theme = (AppTheme)value;
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
        set
        {
            if (_checkForUpdatesOnStartup == value)
            {
                return;
            }

            _checkForUpdatesOnStartup = value;
            OnPropertyChanged();
            Commit();
        }
    }

    public bool DebugUpdate
    {
        get => _debugUpdate;
        set
        {
            if (_debugUpdate == value)
            {
                return;
            }

            _debugUpdate = value;
            OnPropertyChanged();
            Commit();
        }
    }

    public bool StartMinimizedOnLaunch
    {
        get => _startMinimizedOnLaunch;
        set
        {
            if (_startMinimizedOnLaunch == value)
            {
                return;
            }

            _startMinimizedOnLaunch = value;
            OnPropertyChanged();
            Commit();
        }
    }

    public bool LaunchAtWindowsStartup
    {
        get => _launchAtWindowsStartup;
        set
        {
            if (_launchAtWindowsStartup == value)
            {
                return;
            }

            _launchAtWindowsStartup = value;
            OnPropertyChanged();
            Commit();
        }
    }

    public bool ConfirmBeforeLaunch
    {
        get => _confirmBeforeLaunch;
        set
        {
            if (_confirmBeforeLaunch == value)
            {
                return;
            }

            _confirmBeforeLaunch = value;
            OnPropertyChanged();
            Commit();
        }
    }

    public bool ConfirmBeforeDelete
    {
        get => _confirmBeforeDelete;
        set
        {
            if (_confirmBeforeDelete == value)
            {
                return;
            }

            _confirmBeforeDelete = value;
            OnPropertyChanged();
            Commit();
        }
    }

    public int CategorySortModeIndex
    {
        get => (int)_categorySortMode;
        set
        {
            if ((int)_categorySortMode == value)
            {
                return;
            }

            _categorySortMode = (CategorySortMode)value;
            OnPropertyChanged();
            Commit();
        }
    }

    public int AppListSortModeIndex
    {
        get => (int)_appListSortMode;
        set
        {
            if ((int)_appListSortMode == value)
            {
                return;
            }

            _appListSortMode = (AppListSortMode)value;
            OnPropertyChanged();
            Commit();
        }
    }

    public bool RunAsAdministrator
    {
        get => _runAsAdministrator;
        set
        {
            if (_runAsAdministrator == value)
            {
                return;
            }

            _runAsAdministrator = value;
            OnPropertyChanged();
            Commit();
        }
    }

    public int LanguageIndex
    {
        get => (int)_language;
        set
        {
            if ((int)_language == value)
            {
                return;
            }

            _language = (LanguageOption)value;
            OnPropertyChanged();
            Commit();
        }
    }

    public bool SettingsChanged { get; private set; }

    public AppSettings? SavedSettings { get; private set; }

    private void Commit()
    {
        _current = _current with
        {
            Theme = _theme,
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
        SavedSettings = _current;
        SettingsChanged = true;
        _onCommit(_current);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
