using System.Reflection;
using System.Windows;
using applanch.Infrastructure.Storage;
using applanch.Tests.TestSupport;
using applanch.Tests.ViewModels.TestDoubles;
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
        method.Invoke(window, [window, new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)]);
    }

    private static SettingsWindow? GetSettingsWindow(MainWindow window)
    {
        var field = typeof(MainWindow).GetField("_settingsWindow", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field.GetValue(window) as SettingsWindow;
    }
}
