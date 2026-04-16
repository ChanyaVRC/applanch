using applanch.Infrastructure.Storage;
using applanch.Core.Configuration;
using applanch.Tests.TestSupport;
using applanch.Tests.ViewModels.TestDoubles;
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
}