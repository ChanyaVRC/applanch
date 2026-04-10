using System.Windows;
using applanch.Events;
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
public sealed class MainWindowUpdateApplyNotificationTests
{
    [Fact]
    public void ApplyUpdateRequested_ShowsInstallingFloatingNotification_WhileApplying()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            var update = new AppUpdateInfo(
                SemanticVersion.Parse("1.2.0"),
                SemanticVersion.Parse("1.0.0"),
                new Uri("https://example.com/a.zip"),
                new Uri("https://example.com/r"));
            var updateService = new BlockingUpdateService();
            var window = CreateMainWindow(updateService);
            WpfTestHost.ShowOffscreen(window);

            try
            {
                AppEvent.Instance.Invoke(AppEvents.ApplyUpdateRequested, update);
                WpfTestHost.DoEvents();

                var viewModel = Assert.IsType<MainWindowViewModel>(window.DataContext);
                Assert.Equal(string.Format(Strings.Notification_InstallingVersion, update.NewVersion), viewModel.FloatingNotification.Message);
                Assert.Equal(NotificationIconType.Info, viewModel.FloatingNotification.IconType);
            }
            finally
            {
                updateService.CompleteApply();
                window.Close();
                WpfTestHost.DoEvents();
            }
        });
    }

    private static MainWindow CreateMainWindow(BlockingUpdateService updateService)
    {
        var settings = new AppSettings
        {
            CheckForUpdatesOnStartup = false,
            DebugUpdate = true,
        };

        var viewModel = new MainWindowViewModel(
            new FakeResolver(),
            new FakeStore(),
            settings);

        return new MainWindow(
            viewModel,
            new FakeLaunchService(),
            new FakeInteractionService(),
            _ => updateService,
            settings);
    }

    private sealed class BlockingUpdateService : IAppUpdateService
    {
        private readonly TaskCompletionSource<bool> _applyCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<IReadOnlyList<AppUpdateInfo>> GetAvailableUpdatesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AppUpdateInfo>>([]);
        }

        public Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AppUpdateInfo?>(null);
        }

        public Task ApplyUpdateAsync(AppUpdateInfo update, CancellationToken cancellationToken = default)
        {
            return _applyCompletion.Task;
        }

        internal void CompleteApply()
        {
            _applyCompletion.TrySetResult(true);
        }
    }

    private sealed class FakeStore : ILauncherStore
    {
        public IReadOnlyList<LauncherEntry> LoadAll()
        {
            return
            [
                new LauncherEntry(new LaunchPath(@"C:\Tools\App.exe"), Category.Default, string.Empty, "App")
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

        public PromptResult<string>? PromptWithSuggestions(string title, string initialValue, IEnumerable<string> suggestions, Window owner)
        {
            return new PromptResult<string>(initialValue, initialValue);
        }

        public PromptResult<T?>? PromptWithSuggestions<T>(string title, T initialValue, IEnumerable<T> suggestions, Window owner)
        {
            return new PromptResult<T?>(initialValue?.ToString() ?? string.Empty, initialValue);
        }
    }
}
