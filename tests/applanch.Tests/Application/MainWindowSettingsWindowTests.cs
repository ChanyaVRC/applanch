using System.Reflection;
using System.Windows;
using applanch.Infrastructure.Dialogs;
using applanch.Infrastructure.Launch;
using applanch.Infrastructure.Resolution;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Updates;
using applanch.Infrastructure.Utilities;
using applanch.Tests.TestSupport;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.Application;

[Collection("WpfTests")]
public sealed class MainWindowSettingsWindowTests
{
    [Fact]
    public void SettingsButtonClick_ShowsSettingsWindow()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            var settings = new AppSettings
            {
                CheckForUpdatesOnStartup = false,
            };

            var window = CreateMainWindow(settings);
            WpfTestHost.ShowOffscreen(window);

            try
            {
                InvokePrivateClick(window, "SettingsButton_Click");
                WpfTestHost.DoEvents();

                var settingsWindow = GetSettingsWindow(window);

                Assert.NotNull(settingsWindow);
                Assert.True(settingsWindow!.IsVisible);
                Assert.Equal(WindowState.Normal, settingsWindow.WindowState);
            }
            finally
            {
                window.Close();
                WpfTestHost.DoEvents();
            }
        });
    }

    [Fact]
    public void SettingsButtonClick_RestoresHiddenSettingsWindow()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            var settings = new AppSettings
            {
                CheckForUpdatesOnStartup = false,
            };

            var window = CreateMainWindow(settings);
            WpfTestHost.ShowOffscreen(window);

            try
            {
                InvokePrivateClick(window, "SettingsButton_Click");
                WpfTestHost.DoEvents();

                var settingsWindow = Assert.IsType<SettingsWindow>(GetSettingsWindow(window));
                settingsWindow.WindowState = WindowState.Minimized;
                settingsWindow.Hide();
                settingsWindow.Left = -10000;
                settingsWindow.Top = -10000;

                InvokePrivateClick(window, "SettingsButton_Click");
                WpfTestHost.DoEvents();

                Assert.True(settingsWindow.IsVisible);
                Assert.Equal(WindowState.Normal, settingsWindow.WindowState);
            }
            finally
            {
                window.Close();
                WpfTestHost.DoEvents();
            }
        });
    }

    [Fact]
    public void ClosingMainWindow_ClosesSettingsWindow()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            var settings = new AppSettings
            {
                CheckForUpdatesOnStartup = false,
            };

            var window = CreateMainWindow(settings);
            WpfTestHost.ShowOffscreen(window);

            InvokePrivateClick(window, "SettingsButton_Click");
            WpfTestHost.DoEvents();

            var settingsWindow = Assert.IsType<SettingsWindow>(GetSettingsWindow(window));
            Assert.True(settingsWindow.IsVisible);

            window.Close();
            WpfTestHost.DoEvents();

            Assert.False(settingsWindow.IsVisible);
        });
    }

    private static MainWindow CreateMainWindow(AppSettings settings)
    {
        var viewModel = new MainWindowViewModel(
            new FakeResolver(),
            new FakeStore(),
            settings);

        return new MainWindow(
            viewModel,
            new FakeLaunchService(),
            new FakeInteractionService(),
            static _ => new FakeUpdateService(),
            settings);
    }

    private static void InvokePrivateClick(MainWindow window, string methodName)
    {
        var method = typeof(MainWindow).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, [window, new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)]);
    }

    private static SettingsWindow? GetSettingsWindow(MainWindow window)
    {
        var field = typeof(MainWindow).GetField("_settingsWindow", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field!.GetValue(window) as SettingsWindow;
    }

    private sealed class FakeStore : ILauncherStore
    {
        public IReadOnlyList<LauncherEntry> LoadAll()
        {
            return
            [
                new LauncherEntry(new LaunchPath(@"C:\Tools\App.exe"), LauncherEntry.DefaultCategory, string.Empty, "App")
            ];
        }

        public void SaveAll(IEnumerable<LauncherEntry> entries)
        {
        }
    }

    private sealed class FakeResolver : IAppResolver
    {
        public bool TryResolve(string input, out ResolvedApp resolved)
        {
            resolved = default!;
            return false;
        }

        public IReadOnlyList<string> GetSuggestions(string input, int maxResults = 8)
        {
            return [];
        }
    }

    private sealed class FakeLaunchService : IItemLaunchService
    {
        public LaunchExecutionResult TryLaunch(LaunchPath launchPath, string arguments, bool runAsAdministrator = false)
        {
            return LaunchExecutionResult.Success();
        }
    }

    private sealed class FakeInteractionService : IUserInteractionService
    {
        public void Show(string message, string caption, MessageBoxImage icon)
        {
        }

        public bool Confirm(string message, string caption, Window owner)
        {
            return true;
        }

        public string? Prompt(string title, string initialValue, Window owner)
        {
            return initialValue;
        }

        public string? PromptWithSuggestions(string title, string initialValue, IEnumerable<string> suggestions, Window owner)
        {
            return initialValue;
        }
    }

    private sealed class FakeUpdateService : IAppUpdateService
    {
        public Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AppUpdateInfo?>(null);
        }

        public Task ApplyUpdateAsync(AppUpdateInfo update, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
