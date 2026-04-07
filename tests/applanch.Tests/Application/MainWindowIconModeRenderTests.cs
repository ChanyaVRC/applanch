using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
