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

public sealed class MainWindowStartupTests
{
    [Fact]
    public void Show_WithLaunchItemIconOnlyModeEnabled_DoesNotHang()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            var settings = new AppSettings
            {
                LaunchItemIconOnlyMode = true,
                CheckForUpdatesOnStartup = false,
            };

            var viewModel = new MainWindowViewModel(
                new FakeResolver(),
                new FakeStore(),
                settings);

            var window = new MainWindow(
                viewModel,
                new FakeLaunchService(),
                new FakeInteractionService(),
                static _ => new FakeUpdateService(),
                settings);

            try
            {
                var loaded = false;
                window.Loaded += (_, _) => loaded = true;

                window.Show();
                WpfTestHost.DoEvents();

                Assert.True(loaded);
                Assert.True(window.IsVisible);
            }
            finally
            {
                if (window.IsVisible)
                {
                    window.Close();
                    WpfTestHost.DoEvents();
                }
            }
        }, TimeSpan.FromSeconds(5));
    }

    private sealed class FakeStore : ILauncherStore
    {
        public IReadOnlyList<LauncherEntry> LoadAll()
        {
            return
            [
                new LauncherEntry(new LaunchPath(@"C:\\Tools\\App.exe"), LauncherEntry.DefaultCategory, string.Empty, "App")
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