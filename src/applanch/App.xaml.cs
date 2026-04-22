using Microsoft.Win32;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.Windows;
using applanch.Events;
using applanch.Infrastructure.Dialogs;
using applanch.Infrastructure.Integration;
using applanch.Infrastructure.Launch;
using applanch.Resolution;
using applanch.Infrastructure.Storage;
using applanch.Settings;
using applanch.Theming;
using applanch.Infrastructure.Updates;
using applanch.Updates;
using applanch.ViewModels;
using applanch.Utilities;
using applanch.Localization;
using applanch.Infrastructure.Wpf;

namespace applanch;

public sealed partial class App : Application
{
    internal AppEvent Events { get; } = AppEvent.Instance;
    private readonly ThemeApplier _themeApplier;
    private readonly ContextMenuRegistrar _contextMenuRegistrar = new();
    private readonly SparsePackageRegistrar _sparsePackageRegistrar = new();
    private readonly StartupRegistrationService _startupRegistrationService = new();
    private readonly DataBindingTraceListener _dataBindingTraceListener = new(AppLogger.Instance);
    private string? _startupThemeOverrideId;
    private string? _startupConfiguredThemeId;

    public App()
    {
        _themeApplier = new ThemeApplier();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        RegisterLifecycleEventHandlers();
        RegisterDataBindingTraceLogging();
        RegisterGlobalExceptionHandlers();

        AppLogger.Instance.Info("Application starting");
        var configuredSettings = AppSettingsProvider.Current;
        var startupArguments = AppStartupArguments.Parse(e.Args);
        var overrideThemeId = startupArguments.ThemeOverrideId;
        _startupThemeOverrideId = overrideThemeId;
        _startupConfiguredThemeId = configuredSettings.ThemeId;

        var settings = CreateStartupSettings(configuredSettings, overrideThemeId);
        InitializeEnvironment(settings.ThemeId);
        ApplyLanguage(settings.Language);
        ApplyStartupRegistration(settings);

        if (TryHandleStartupArgument(startupArguments))
        {
            Shutdown();
            return;
        }

        ApplyContextMenuRegistration(settings);

        ShowMainWindow(settings);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLogger.Instance.Info("Application exiting");
        UnregisterLifecycleEventHandlers();
        UnregisterDataBindingTraceLogging();
        AppLogger.Instance.Dispose();
        base.OnExit(e);
    }

    private void RegisterDataBindingTraceLogging()
    {
        var source = PresentationTraceSources.DataBindingSource;
        if (!source.Listeners.Contains(_dataBindingTraceListener))
        {
            source.Listeners.Add(_dataBindingTraceListener);
        }

        if (source.Switch.Level < SourceLevels.Warning)
        {
            source.Switch.Level = SourceLevels.Warning;
        }
    }

    private void UnregisterDataBindingTraceLogging()
    {
        PresentationTraceSources.DataBindingSource.Listeners.Remove(_dataBindingTraceListener);
    }

    private void RegisterLifecycleEventHandlers()
    {
        Events.Subscribe(AppEvents.Commit, OnSettingsCommitted);
        Events.Subscribe(AppEvents.Refresh, Refresh);
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    private void UnregisterLifecycleEventHandlers()
    {
        Events.Unsubscribe(AppEvents.Commit, OnSettingsCommitted);
        Events.Unsubscribe(AppEvents.Refresh, Refresh);
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            AppLogger.Instance.Error(args.Exception, "Unhandled UI exception");
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                AppLogger.Instance.Error(ex, "Unhandled domain exception");
            }
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppLogger.Instance.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };
    }

    private bool TryHandleStartupArgument(AppStartupArguments startupArguments)
    {
        var registerPath = startupArguments.RegisterPath;
        if (!string.IsNullOrWhiteSpace(registerPath) && (File.Exists(registerPath) || Directory.Exists(registerPath)))
        {
            LauncherStore.Add(registerPath);
            return true;
        }

        if (startupArguments.IsContextMenuUnregisterRequested)
        {
            _contextMenuRegistrar.Unregister();
            return true;
        }

        return false;
    }

    private static AppSettings CreateStartupSettings(AppSettings settings, string? overrideThemeId)
    {
        if (string.IsNullOrWhiteSpace(overrideThemeId))
        {
            return settings;
        }

        return settings with { ThemeId = overrideThemeId };
    }

    private void OnSettingsCommitted(AppSettings settings)
    {
        var refreshedSettings = CreatePersistedSettings(settings, _startupThemeOverrideId, _startupConfiguredThemeId);
        refreshedSettings.Save();
    }

    private static AppSettings CreatePersistedSettings(
        AppSettings settings,
        string? startupThemeOverrideId,
        string? startupConfiguredThemeId)
    {
        var normalized = settings.Normalize();

        if (string.IsNullOrWhiteSpace(startupThemeOverrideId) || string.IsNullOrWhiteSpace(startupConfiguredThemeId))
        {
            return normalized;
        }

        return string.Equals(normalized.ThemeId, startupThemeOverrideId, StringComparison.OrdinalIgnoreCase)
            ? normalized with { ThemeId = startupConfiguredThemeId }
            : normalized;
    }

    private void InitializeEnvironment(string themeId)
    {
        _themeApplier.ApplyTheme(Resources, themeId, Windows.Cast<Window>());

        LauncherStore.EnsureStorageDirectory();
        if (!_sparsePackageRegistrar.IsAlreadyRegistered())
        {
            _ = _sparsePackageRegistrar.TryEnsureRegisteredAsync();
        }
    }

    internal void Refresh(AppRefreshPayload payload)
    {
        var currentSettings = payload.CurrentSettings;
        ApplyLanguage(currentSettings.Language);
        ApplyStartupRegistration(currentSettings);
        ApplyContextMenuRegistration(currentSettings);
        LocalizedStrings.Instance.NotifyLanguageChanged();
        _themeApplier.ApplyTheme(Resources, currentSettings.ThemeId, Windows.Cast<Window>());
    }

    private void ApplyContextMenuRegistration(AppSettings settings)
    {
        try
        {
            if (settings.RegisterContextMenuOnStartup)
            {
                _contextMenuRegistrar.EnsureRegistered();
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, "Failed to apply context menu registration settings");
        }
    }

    private void ShowMainWindow(AppSettings settings)
    {
        MainWindow = CreateMainWindow(settings);
        _themeApplier.ApplyTheme(Resources, settings.ThemeId, [MainWindow]);

        if (settings.StartMinimizedOnLaunch)
        {
            MainWindow.WindowState = WindowState.Minimized;
        }

        MainWindow.Show();
    }

    private static MainWindow CreateMainWindow(AppSettings settings)
    {
        var interactionService = CreateInteractionService();
        var updateServiceFactory = CreateUpdateServiceFactory();

        return new MainWindow(
            CreateMainWindowViewModel(settings),
            CreateItemLaunchService(),
            interactionService,
            updateServiceFactory,
            settings);
    }

    private static MainWindowViewModel CreateMainWindowViewModel(AppSettings settings)
    {
        return new MainWindowViewModel(new AppResolverAdapter(), new LauncherStoreAdapter(), settings);
    }

    private static ItemLaunchService CreateItemLaunchService()
    {
        return new ItemLaunchService();
    }

    private static UserInteractionService CreateInteractionService()
    {
        return new UserInteractionService();
    }

    private static Func<AppSettings, IAppUpdateService> CreateUpdateServiceFactory()
    {
        return static settings => new GitHubAppUpdateService(settings.DebugUpdate, settings.AllowPrereleaseUpdates);
    }

    private static void ApplyLanguage(LanguageOption language)
    {
        var culture = language.GetCultureInfo();
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
    }

    private void ApplyStartupRegistration(AppSettings settings)
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return;
            }

            _startupRegistrationService.Apply(settings.LaunchAtWindowsStartup, executablePath);
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, "Failed to apply startup registration setting");
        }
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color)
        {
            Dispatcher.Invoke(() =>
            {
                _themeApplier.ApplyTheme(Resources, AppSettingsProvider.Current.ThemeId, Windows.Cast<Window>());
            });
        }
    }

}
