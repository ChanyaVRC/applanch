using System.ComponentModel;
using System.Runtime.CompilerServices;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;

namespace applanch;

internal sealed class SettingsWindowViewModel : INotifyPropertyChanged
{
    private readonly Action _reapplyTheme;
    private readonly Action<AppSettings> _save;
    private AppSettings _current;
    private AppTheme _theme;
    private bool _closeOnLaunch;
    private bool _checkForUpdatesOnStartup;
    private bool _debugUpdate;

    internal SettingsWindowViewModel(AppSettings settings, Action reapplyTheme, Action<AppSettings>? save = null)
    {
        _reapplyTheme = reapplyTheme;
        _save = save ?? (static s => s.Save());
        _current = settings;
        _theme = settings.Theme;
        _closeOnLaunch = settings.CloseOnLaunch;
        _checkForUpdatesOnStartup = settings.CheckForUpdatesOnStartup;
        _debugUpdate = settings.DebugUpdate;
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
            _reapplyTheme();
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
            OnPropertyChanged();
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

    public bool SettingsChanged { get; private set; }

    public AppSettings? SavedSettings { get; private set; }

    private void Commit()
    {
        _current = _current with
        {
            Theme = _theme,
            CloseOnLaunch = _closeOnLaunch,
            CheckForUpdatesOnStartup = _checkForUpdatesOnStartup,
            DebugUpdate = _debugUpdate,
        };
        _save(_current);
        SavedSettings = _current;
        SettingsChanged = true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
