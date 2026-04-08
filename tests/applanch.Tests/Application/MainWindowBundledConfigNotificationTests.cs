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
using Strings = applanch.Properties.Resources;

namespace applanch.Tests.Application;

[Collection("WpfTests")]
public sealed class MainWindowBundledConfigNotificationTests
{
    [Fact]
    public void LoadedWindow_ShowsPendingBundledConfigNotification()
    {
        WpfTestHost.RunInSta(() =>
        {
            using var scope = BundledConfigLoadNotificationTestScope.Enter();
            WpfTestHost.EnsureAppResources();
            BundledConfigLoadNotificationCenter.ReportMissing("launch-fallbacks.json");

            var window = CreateMainWindow();
            WpfTestHost.ShowOffscreen(window);

            try
            {
                WpfTestHost.DoEvents();

                var viewModel = Assert.IsType<MainWindowViewModel>(window.DataContext);
                Assert.Equal(
                    string.Format(Strings.Notification_BundledConfigMissing, "launch-fallbacks.json"),
                    viewModel.FloatingNotification.Message);
                Assert.Equal(NotificationIconType.Warning, viewModel.FloatingNotification.IconType);
            }
            finally
            {
                window.Close();
                WpfTestHost.DoEvents();
            }
        });
    }

    [Fact]
    public void ReportedBundledConfigNotification_ShowsWhileWindowIsOpen()
    {
        WpfTestHost.RunInSta(() =>
        {
            using var scope = BundledConfigLoadNotificationTestScope.Enter();
            WpfTestHost.EnsureAppResources();

            var window = CreateMainWindow();
            WpfTestHost.ShowOffscreen(window);

            try
            {
                BundledConfigLoadNotificationCenter.ReportInvalidFormat("theme-palette.json");
                WpfTestHost.DoEvents();

                var viewModel = Assert.IsType<MainWindowViewModel>(window.DataContext);
                Assert.Equal(
                    string.Format(Strings.Notification_BundledConfigInvalidFormat, "theme-palette.json"),
                    viewModel.FloatingNotification.Message);
                Assert.Equal(NotificationIconType.Warning, viewModel.FloatingNotification.IconType);
            }
            finally
            {
                window.Close();
                WpfTestHost.DoEvents();
            }
        });
    }

    private static MainWindow CreateMainWindow()
    {
        var settings = new AppSettings
        {
            CheckForUpdatesOnStartup = false,
        };

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