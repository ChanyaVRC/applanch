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
        set => UpdateDraft(_draft with { PostLaunchBehavior = value });
    }

    public bool CheckForUpdatesOnStartup
    {
        get => _draft.CheckForUpdatesOnStartup;
        set => UpdateDraft(_draft with { CheckForUpdatesOnStartup = value });
    }

    public UpdateInstallBehavior SelectedUpdateInstallBehavior
    {
        get => _draft.UpdateInstallBehavior;
        set => UpdateDraft(_draft with { UpdateInstallBehavior = value });
    }

    public bool DebugUpdate
    {
        get => _draft.DebugUpdate;
        set => UpdateDraft(_draft with { DebugUpdate = value });
    }

    public bool StartMinimizedOnLaunch
    {
        get => _draft.StartMinimizedOnLaunch;
        set => UpdateDraft(_draft with { StartMinimizedOnLaunch = value });
    }

    public bool LaunchAtWindowsStartup
    {
        get => _draft.LaunchAtWindowsStartup;
        set => UpdateDraft(_draft with { LaunchAtWindowsStartup = value });
    }

    public bool RegisterContextMenuOnStartup
    {
        get => _draft.RegisterContextMenuOnStartup;
        set => UpdateDraft(_draft with { RegisterContextMenuOnStartup = value });
    }

    public bool FetchHttpIcons
    {
        get => _draft.FetchHttpIcons;
        set => UpdateDraft(_draft with { FetchHttpIcons = value });
    }

    public bool AllowPrivateNetworkHttpIconRequests
    {
        get => _draft.AllowPrivateNetworkHttpIconRequests;
        set => UpdateDraft(_draft with { AllowPrivateNetworkHttpIconRequests = value });
    }

    public bool ConfirmBeforeLaunch
    {
        get => _draft.ConfirmBeforeLaunch;
        set => UpdateDraft(_draft with { ConfirmBeforeLaunch = value });
    }

    public bool ConfirmBeforeDelete
    {
        get => _draft.ConfirmBeforeDelete;
        set => UpdateDraft(_draft with { ConfirmBeforeDelete = value });
    }

    public int QuickAddSuggestionLimit
    {
        get => _draft.QuickAddSuggestionLimit;
        set
        {
            if (!QuickAddSuggestionLimitOptionsValues.Contains(value))
            {
                return;
            }

            UpdateDraft(_draft with { QuickAddSuggestionLimit = value });
        }
    }

    public CategorySortMode SelectedCategorySortMode
    {
        get => _draft.CategorySortMode;
        set => UpdateDraft(_draft with { CategorySortMode = value });
    }

    public AppListSortMode SelectedAppListSortMode
    {
        get => _draft.AppListSortMode;
        set => UpdateDraft(_draft with { AppListSortMode = value });
    }

    public bool LaunchItemIconOnlyMode
    {
        get => _draft.LaunchItemIconOnlyMode;
        set => UpdateDraft(_draft with { LaunchItemIconOnlyMode = value });
    }

    public bool RunAsAdministrator
    {
        get => _draft.RunAsAdministrator;
        set => UpdateDraft(_draft with { RunAsAdministrator = value });
    }

    public LanguageOption SelectedLanguage
    {
        get => _draft.Language;
        set => UpdateDraft(_draft with { Language = value });
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

    private bool UpdateDraft(
        AppSettings nextDraft,
        [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (_draft == nextDraft)
        {
            return false;
        }

        _draft = nextDraft;
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
