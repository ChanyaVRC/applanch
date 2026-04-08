using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using applanch.Events;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using applanch.Infrastructure.Utilities;

namespace applanch.ViewModels;

internal sealed class SettingsWindowViewModel : ObservableObject
{
    private static readonly int[] QuickAddSuggestionLimitOptionsValues = [10, 20, 30, 50, 100];

    private readonly AppEvent _appEvent;
    private readonly Func<IReadOnlyList<ThemeOption>> _themeOptionsProvider;
    private IReadOnlyList<ThemeOption> _themeOptions;
    private AppSettings _current;
    private AppSettings _draft;

    internal SettingsWindowViewModel(
        AppSettings settings,
        AppEvent appEvent,
        Func<IReadOnlyList<ThemeOption>>? themeOptionsProvider = null)
    {
        _appEvent = appEvent;
        _themeOptionsProvider = themeOptionsProvider ?? ThemeOptionsProvider.Load;
        _themeOptions = _themeOptionsProvider();
        _current = settings;
        _draft = settings;
    }

    public IReadOnlyList<ThemeOption> ThemeOptions => _themeOptions;

    public IReadOnlyList<int> QuickAddSuggestionLimitOptions => QuickAddSuggestionLimitOptionsValues;

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
        get => _draft.ThemeId;
        set
        {
            if (!IsThemeSelectionVisible)
            {
                return;
            }

            var selectedIndex = ResolveThemeIndex(value);
            if (selectedIndex < 0)
            {
                return;
            }

            var selectedThemeId = _themeOptions[selectedIndex].ThemeId;
            if (string.Equals(_draft.ThemeId, selectedThemeId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _draft = _draft with { ThemeId = selectedThemeId };
            OnPropertyChanged();
            OnPropertyChanged(nameof(ThemeIndex));
            OnPropertyChanged(nameof(SelectedThemeDisplayName));
            Commit();
        }
    }

    public string SelectedThemeDisplayName
    {
        get
        {
            var themeIndex = ResolveThemeIndex(_draft.ThemeId);
            return themeIndex >= 0 ? _themeOptions[themeIndex].DisplayName : string.Empty;
        }
    }

    public PostLaunchBehavior SelectedPostLaunchBehavior
    {
        get => _draft.PostLaunchBehavior;
        set => SetDraftValue(value, static settings => settings.PostLaunchBehavior, static (settings, v) => settings with { PostLaunchBehavior = v });
    }

    public bool CheckForUpdatesOnStartup
    {
        get => _draft.CheckForUpdatesOnStartup;
        set => SetDraftValue(value, static settings => settings.CheckForUpdatesOnStartup, static (settings, v) => settings with { CheckForUpdatesOnStartup = v });
    }

    public UpdateInstallBehavior SelectedUpdateInstallBehavior
    {
        get => _draft.UpdateInstallBehavior;
        set => SetDraftValue(value, static settings => settings.UpdateInstallBehavior, static (settings, v) => settings with { UpdateInstallBehavior = v });
    }

    public bool DebugUpdate
    {
        get => _draft.DebugUpdate;
        set => SetDraftValue(value, static settings => settings.DebugUpdate, static (settings, v) => settings with { DebugUpdate = v });
    }

    public bool StartMinimizedOnLaunch
    {
        get => _draft.StartMinimizedOnLaunch;
        set => SetDraftValue(value, static settings => settings.StartMinimizedOnLaunch, static (settings, v) => settings with { StartMinimizedOnLaunch = v });
    }

    public bool LaunchAtWindowsStartup
    {
        get => _draft.LaunchAtWindowsStartup;
        set => SetDraftValue(value, static settings => settings.LaunchAtWindowsStartup, static (settings, v) => settings with { LaunchAtWindowsStartup = v });
    }

    public bool RegisterContextMenuOnStartup
    {
        get => _draft.RegisterContextMenuOnStartup;
        set => SetDraftValue(value, static settings => settings.RegisterContextMenuOnStartup, static (settings, v) => settings with { RegisterContextMenuOnStartup = v });
    }

    public bool FetchHttpIcons
    {
        get => _draft.FetchHttpIcons;
        set => SetDraftValue(value, static settings => settings.FetchHttpIcons, static (settings, v) => settings with { FetchHttpIcons = v });
    }

    public bool AllowPrivateNetworkHttpIconRequests
    {
        get => _draft.AllowPrivateNetworkHttpIconRequests;
        set => SetDraftValue(value, static settings => settings.AllowPrivateNetworkHttpIconRequests, static (settings, v) => settings with { AllowPrivateNetworkHttpIconRequests = v });
    }

    public bool ConfirmBeforeLaunch
    {
        get => _draft.ConfirmBeforeLaunch;
        set => SetDraftValue(value, static settings => settings.ConfirmBeforeLaunch, static (settings, v) => settings with { ConfirmBeforeLaunch = v });
    }

    public bool ConfirmBeforeDelete
    {
        get => _draft.ConfirmBeforeDelete;
        set => SetDraftValue(value, static settings => settings.ConfirmBeforeDelete, static (settings, v) => settings with { ConfirmBeforeDelete = v });
    }

    public int QuickAddSuggestionLimit
    {
        get => _draft.QuickAddSuggestionLimit;
        set
        {
            if (_draft.QuickAddSuggestionLimit == value || !QuickAddSuggestionLimitOptionsValues.Contains(value))
            {
                return;
            }

            _draft = _draft with { QuickAddSuggestionLimit = value };
            OnPropertyChanged();
            Commit();
        }
    }

    public CategorySortMode SelectedCategorySortMode
    {
        get => _draft.CategorySortMode;
        set => SetDraftValue(value, static settings => settings.CategorySortMode, static (settings, v) => settings with { CategorySortMode = v });
    }

    public AppListSortMode SelectedAppListSortMode
    {
        get => _draft.AppListSortMode;
        set => SetDraftValue(value, static settings => settings.AppListSortMode, static (settings, v) => settings with { AppListSortMode = v });
    }

    public bool LaunchItemIconOnlyMode
    {
        get => _draft.LaunchItemIconOnlyMode;
        set => SetDraftValue(value, static settings => settings.LaunchItemIconOnlyMode, static (settings, v) => settings with { LaunchItemIconOnlyMode = v });
    }

    public bool RunAsAdministrator
    {
        get => _draft.RunAsAdministrator;
        set => SetDraftValue(value, static settings => settings.RunAsAdministrator, static (settings, v) => settings with { RunAsAdministrator = v });
    }

    public LanguageOption SelectedLanguage
    {
        get => _draft.Language;
        set => SetDraftValue(value, static settings => settings.Language, static (settings, v) => settings with { Language = v });
    }

    public bool SettingsChanged { get; private set; }

    public string AppVersion => AppVersionProvider.GetDisplayVersion();

    internal void ApplyExternalSettings(AppSettings settings)
    {
        var previousLanguage = _draft.Language;
        _current = settings;
        _draft = settings;

        RefreshThemeOptionsIfLanguageChanged(previousLanguage, _draft.Language);

        OnPropertyChanged(string.Empty);
    }

    internal void ResetToDefaults()
    {
        var defaults = new AppSettings();
        var previousLanguage = _draft.Language;
        _draft = defaults;

        RefreshThemeOptionsIfLanguageChanged(previousLanguage, _draft.Language);

        OnPropertyChanged(string.Empty);
        Commit();
    }

    internal string CreateDiagnosticsText()
    {
        var builder = new StringBuilder(256);
        builder.AppendLine($"App version: {AppVersionProvider.GetDisplayVersion()}");
        builder.AppendLine($"OS: {RuntimeInformation.OSDescription.Trim()}");
        builder.AppendLine($".NET: {RuntimeInformation.FrameworkDescription}");
        builder.AppendLine($"UI culture: {CultureInfo.CurrentUICulture.Name}");
        builder.AppendLine($"Culture: {CultureInfo.CurrentCulture.Name}");
        builder.AppendLine($"Log folder: {AppLogger.LogDirectoryPath}");
        builder.AppendLine($"Update check on startup: {CheckForUpdatesOnStartup}");
        builder.AppendLine($"Update install behavior: {SelectedUpdateInstallBehavior}");
        builder.AppendLine($"Debug update mode: {DebugUpdate}");
        return builder.ToString();
    }

    private bool SetDraftValue<T>(
        T value,
        Func<AppSettings, T> getter,
        Func<AppSettings, T, AppSettings> updater,
        [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(getter(_draft), value))
        {
            return false;
        }

        _draft = updater(_draft, value);
        OnPropertyChanged(propertyName);

        Commit();
        return true;
    }

    private void Commit()
    {
        var previousLanguage = _current.Language;

        _current = _draft;

        SettingsChanged = true;
        _appEvent.Invoke(AppEvents.Commit, _current);

        RefreshThemeOptionsIfLanguageChanged(previousLanguage, _current.Language);
    }

    private void RefreshThemeOptionsIfLanguageChanged(LanguageOption previousLanguage, LanguageOption nextLanguage)
    {
        if (previousLanguage != nextLanguage)
        {
            RefreshThemeOptionsForCurrentCulture();
        }
    }

    private void RefreshThemeOptionsForCurrentCulture()
    {
        foreach (var option in _themeOptions)
        {
            option.NotifyDisplayNameChanged();
        }

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

        var themeIndex = ResolveThemeIndex(_draft.ThemeId);
        if (themeIndex >= 0)
        {
            return themeIndex;
        }

        return 0;
    }

    private int ResolveThemeIndex(string themeId)
    {
        for (var i = 0; i < _themeOptions.Count; i++)
        {
            if (string.Equals(_themeOptions[i].ThemeId, themeId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
