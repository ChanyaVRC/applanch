using applanch.Infrastructure.Storage;
using applanch.Tests.TestSupport;
using applanch.Tests.ViewModels.TestDoubles;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.Application;

[Collection("WpfTests")]
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

                WpfTestHost.ShowOffscreen(window);

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
}