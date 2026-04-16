using applanch.Events;
using applanch.Settings;
using applanch.Updates;
using applanch.Tests.TestSupport;
using applanch.Tests.ViewModels.TestDoubles;
using applanch.ViewModels;
using Xunit;

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
                Assert.Equal(string.Format(AppResources.Notification_InstallingVersion, update.NewVersion), viewModel.FloatingNotification.Message);
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
}