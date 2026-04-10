using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using applanch.Events;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using applanch.Infrastructure.Updates;
using applanch.Infrastructure.Utilities;

namespace applanch.ViewModels;

internal sealed class SettingsWindowViewModel : ObservableObject
{
    private static readonly int[] QuickAddSuggestionLimitOptionsValues = [10, 20, 30, 50, 100];

    private readonly AppEvent _appEvent;
    private readonly Func<IReadOnlyDictionary<string, ThemeOption>> _themeOptionsProvider;
    private readonly Func<AppSettings, IAppUpdateService> _updateServiceFactory;
    private IReadOnlyDictionary<string, ThemeOption> _themeOptionsMap;
    private readonly UpdateWorkflow _updateWorkflow;
    private AppSettings _current;
    private AppSettings _draft;
    private IAppUpdateService _updateService;
    private IDisposable? _ownedUpdateService;
    private AppUpdateInfo? _selectedAvailableUpdate;
    private bool _isRefreshingAvailableUpdates;
    private bool _isApplyingSelectedUpdate;
    private string _availableUpdatesStatusMessage = string.Empty;

    internal SettingsWindowViewModel(
        AppSettings settings,
        AppEvent appEvent,
        Func<IReadOnlyDictionary<string, ThemeOption>>? themeOptionsProvider = null,
        Func<AppSettings, IAppUpdateService>? updateServiceFactory = null)
    {
        _appEvent = appEvent;
        _themeOptionsProvider = themeOptionsProvider ?? ThemeOptionsProvider.Load;
        _updateServiceFactory = updateServiceFactory ?? (static settings => new GitHubAppUpdateService(settings.DebugUpdate, settings.AllowPrereleaseUpdates));
        _themeOptionsMap = _themeOptionsProvider();
        _current = settings;
        _draft = settings;
        _updateService = _updateServiceFactory(settings);
        _ownedUpdateService = _updateService as IDisposable;
        _updateWorkflow = new UpdateWorkflow(_updateService);
        AvailableUpdates = [];
    }

    public IReadOnlyList<ThemeOption> ThemeOptions => _themeOptionsMap.Values.ToList();

    public IReadOnlyList<int> QuickAddSuggestionLimitOptions => QuickAddSuggestionLimitOptionsValues;

    public ObservableCollection<AppUpdateInfo> AvailableUpdates { get; }

    public bool IsThemeSelectionVisible => _themeOptionsMap.Count > 0;

    public int ThemeIndex
    {
        get => ResolveThemeIndex();
        set
        {
            if (!IsThemeSelectionVisible || value < 0 || value >= _themeOptionsMap.Count)
            {
                return;
            }

            SelectedThemeId = _themeOptionsMap.Values.ElementAt(value).ThemeId;
        }
    }

    public string SelectedThemeId
    {
        get => _draft.ThemeId;
        set
        {
            if (!IsThemeSelectionVisible || !_themeOptionsMap.ContainsKey(value))
            {
                return;
            }

            if (string.Equals(_draft.ThemeId, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _draft = _draft with { ThemeId = value };
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
            return _themeOptionsMap.TryGetValue(_draft.ThemeId, out var theme)
                ? theme.DisplayName
                : string.Empty;
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

    public bool AllowPrereleaseUpdates
    {
        get => _draft.AllowPrereleaseUpdates;
        set => UpdateDraft(_draft with { AllowPrereleaseUpdates = value });
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

    public AppUpdateInfo? SelectedAvailableUpdate
    {
        get => _selectedAvailableUpdate;
        set
        {
            if (SetField(ref _selectedAvailableUpdate, value))
            {
                OnPropertyChanged(nameof(CanApplySelectedUpdate));
            }
        }
    }

    public bool CanApplySelectedUpdate =>
        SelectedAvailableUpdate is not null &&
        !_isRefreshingAvailableUpdates &&
        !_isApplyingSelectedUpdate;

    public string AvailableUpdatesStatusMessage
    {
        get => _availableUpdatesStatusMessage;
        private set => SetField(ref _availableUpdatesStatusMessage, value);
    }

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

    internal async Task RefreshAvailableUpdatesAsync(CancellationToken cancellationToken = default)
    {
        ReplaceUpdateService(_updateServiceFactory(_draft));
        _isRefreshingAvailableUpdates = true;
        AvailableUpdatesStatusMessage = AppResources.UpdateVersions_Loading;
        OnPropertyChanged(nameof(CanApplySelectedUpdate));

        try
        {
            var previousSelection = SelectedAvailableUpdate?.NewVersion;
            var updates = await _updateWorkflow.GetAvailableUpdatesSafeAsync(cancellationToken);
            ReplaceCollection(AvailableUpdates, updates);

            SelectedAvailableUpdate = AvailableUpdates
                .FirstOrDefault(update => string.Equals(update.NewVersion, previousSelection, StringComparison.Ordinal))
                ?? AvailableUpdates.FirstOrDefault();

            AvailableUpdatesStatusMessage = AvailableUpdates.Count == 0
                ? AppResources.UpdateVersions_NoneAvailable
                : string.Empty;
        }
        finally
        {
            _isRefreshingAvailableUpdates = false;
            OnPropertyChanged(nameof(CanApplySelectedUpdate));
        }
    }

    internal async Task<UpdateApplyResult?> ApplySelectedUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedAvailableUpdate is null)
        {
            return null;
        }

        ReplaceUpdateService(_updateServiceFactory(_draft));
        _isApplyingSelectedUpdate = true;
        OnPropertyChanged(nameof(CanApplySelectedUpdate));

        try
        {
            return await _updateWorkflow.ApplyUpdateSafeAsync(SelectedAvailableUpdate, cancellationToken);
        }
        finally
        {
            _isApplyingSelectedUpdate = false;
            OnPropertyChanged(nameof(CanApplySelectedUpdate));
        }
    }

    internal void Dispose()
    {
        _ownedUpdateService?.Dispose();
        _ownedUpdateService = null;
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
        builder.AppendLine($"Allow prerelease updates: {AllowPrereleaseUpdates}");
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

    private void ReplaceUpdateService(IAppUpdateService updateService)
    {
        ArgumentNullException.ThrowIfNull(updateService);

        _ownedUpdateService?.Dispose();
        _updateService = updateService;
        _ownedUpdateService = updateService as IDisposable;
        _updateWorkflow.SetUpdateService(updateService);
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
        foreach (var option in _themeOptionsMap.Values)
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

        if (_themeOptionsMap.ContainsKey(_draft.ThemeId))
        {
            var values = _themeOptionsMap.Values.ToList();
            for (var i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i].ThemeId, _draft.ThemeId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return 0;
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        var snapshot = values.ToList();
        if (target.SequenceEqual(snapshot))
        {
            return;
        }

        target.Clear();
        foreach (var value in snapshot)
        {
            target.Add(value);
        }
    }
}
