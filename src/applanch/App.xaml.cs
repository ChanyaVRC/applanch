using Microsoft.Win32;
using System.IO;
using System.Globalization;
using System.Windows;
using applanch.Infrastructure.Integration;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using applanch.Infrastructure.Utilities;

namespace applanch;

public partial class App : Application
{
    internal const string RegisterArgument = "--register";
    internal AppEvent Events { get; } = new();
    private AppSettings _settings = new();
    private readonly ThemeManager _themeManager = new(() => AppSettings.Load().Theme);
    private readonly ContextMenuRegistrar _contextMenuRegistrar = new();
    private readonly StartupRegistrationService _startupRegistrationService = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Events.Register(AppEvents.Refresh, Refresh);

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

        AppLogger.Instance.Info("Application starting");
        InitializeEnvironment();

        _settings = AppSettings.Load();
        ApplyLanguage(_settings.Language);
        ApplyStartupRegistration(_settings);

        if (TryHandleRegisterArgument(e.Args))
        {
            Shutdown();
            return;
        }

        ShowMainWindow();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLogger.Instance.Info("Application exiting");
        Events.Unregister(AppEvents.Refresh, Refresh);
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        AppLogger.Instance.Dispose();
        base.OnExit(e);
    }

    private void InitializeEnvironment()
    {
        _themeManager.ApplyTheme(Resources, Windows.Cast<Window>());
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        LauncherStore.EnsureStorageDirectory();
        _contextMenuRegistrar.EnsureRegistered();
    }

    internal void Refresh(AppSettings settings)
    {
        _settings = settings;
        ApplyLanguage(settings.Language);
        ApplyStartupRegistration(settings);
        LocalizedStrings.Instance.NotifyLanguageChanged();
        _themeManager.ApplyTheme(Resources, Windows.Cast<Window>());
    }

    private void ShowMainWindow()
    {
        MainWindow = new MainWindow();
        if (_settings.StartMinimizedOnLaunch)
        {
            MainWindow.WindowState = WindowState.Minimized;
        }

        MainWindow.Show();
    }

    private static void ApplyLanguage(LanguageOption language)
    {
        var cultureName = language switch
        {
            LanguageOption.English => "en",
            LanguageOption.Japanese => "ja",
            _ => CultureInfo.InstalledUICulture.Name,
        };

        var culture = new CultureInfo(cultureName);
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
                _themeManager.ApplyTheme(Resources, Windows.Cast<Window>());
            });
        }
    }

    private static bool TryHandleRegisterArgument(string[] args)
    {
        if (args.Length < 2 || !string.Equals(args[0], RegisterArgument, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = args[1];
        if (File.Exists(path) || Directory.Exists(path))
        {
            LauncherStore.Add(path);
        }

        return true;
    }
}
