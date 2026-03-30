using Microsoft.Win32;
using System.IO;
using System.Windows;
using applanch.Infrastructure.Integration;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using applanch.Infrastructure.Utilities;

namespace applanch;

public partial class App : Application
{
    internal const string RegisterArgument = "--register";
    private readonly ThemeManager _themeManager = new(() => AppSettings.Load().Theme);
    private readonly ContextMenuRegistrar _contextMenuRegistrar = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        AppLogger.Instance.Dispose();
        base.OnExit(e);
    }

    private void InitializeEnvironment()
    {
        _themeManager.ApplyTheme(Resources);
        ApplyCaptionThemeToOpenWindows();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        LauncherStore.EnsureStorageDirectory();
        _contextMenuRegistrar.EnsureRegistered();
    }

    internal void ReapplyTheme()
    {
        _themeManager.ApplyTheme(Resources);
        ApplyCaptionThemeToOpenWindows();
    }

    private void ShowMainWindow()
    {
        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color)
        {
            Dispatcher.Invoke(() =>
            {
                _themeManager.ApplyTheme(Resources);
                ApplyCaptionThemeToOpenWindows();
            });
        }
    }

    private void ApplyCaptionThemeToOpenWindows()
    {
        foreach (Window window in Windows)
        {
            WindowCaptionThemeHelper.Apply(window);
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
