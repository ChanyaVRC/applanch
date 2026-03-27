using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace applanch;

public partial class App : Application
{
	internal const string RegisterArgument = "--register";
	private readonly ThemeManager _themeManager = new();
	private readonly ContextMenuRegistrar _contextMenuRegistrar = new();

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

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
		SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
		base.OnExit(e);
	}

	private void InitializeEnvironment()
	{
		_themeManager.ApplyTheme(Resources);
		SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

		LauncherStore.EnsureStorageDirectory();
		_contextMenuRegistrar.EnsureRegistered();
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
			Dispatcher.Invoke(() => _themeManager.ApplyTheme(Resources));
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
