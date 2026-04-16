using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using applanch.Settings;
using applanch.Tests.TestSupport;
using applanch.Tests.ViewModels.TestDoubles;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.Application;

[Collection("WpfTests")]
public sealed class MainWindowIconModeRenderTests
{
    [Fact]
    public void Startup_WhenIconModeEnabled_RendersAtLeastOneLaunchItem()
    {
        WpfTestHost.RunInSta((Action)(() =>
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

            WpfTestHost.ShowOffscreen(window);
            window.UpdateLayout();
            window.UpdateLayout();

            try
            {
                var launchListBox = (ListBox)window.FindName("LaunchListBox");
                Assert.NotNull(launchListBox);
                Assert.NotEmpty(launchListBox.Items);

                launchListBox.ScrollIntoView(launchListBox.Items[0]);
                WaitUntil(
                    () => launchListBox.ItemContainerGenerator.ContainerFromIndex(0) is not null,
                    TimeSpan.FromSeconds(2),
                    "first ListBoxItem container to be realized",
                    () =>
                    {
                        launchListBox.UpdateLayout();
                        WpfTestHost.DoEvents();
                    });

                var realized = launchListBox.ItemContainerGenerator.ContainerFromIndex(0);
                var realizedListBoxItems = CountVisualChildren<ListBoxItem>(launchListBox);
                var panel = FindVisualChild<applanch.Controls.VirtualizingWrapPanel>(launchListBox);
                var panelVisualChildren = panel is null ? -1 : VisualTreeHelper.GetChildrenCount(panel);

                Assert.True(realized is not null,
                    $"Expected container for index 0. Realized ListBoxItem visuals={realizedListBoxItems}, panel={panel?.GetType().Name ?? "null"}, panel visual children={panelVisualChildren}");
                Assert.Equal(Visibility.Visible, ((ListBoxItem)realized!).Visibility);
            }
            finally
            {
                window.Close();
                WpfTestHost.DoEvents();
            }
        }));
    }

    private static int CountVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        var count = 0;
        var children = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < children; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T)
            {
                count++;
            }

            count += CountVisualChildren<T>(child);
        }

        return count;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        var children = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < children; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T target)
            {
                return target;
            }

            var result = FindVisualChild<T>(child);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private static void WaitUntil(Func<bool> condition, TimeSpan timeout, string conditionDescription, Action onPoll)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Timed out after {timeout} while waiting for {conditionDescription}.");
            }

            onPoll();
            Thread.Sleep(1);
        }
    }
}
