using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
public sealed class MainWindowCategorySidebarTests
{
    [Fact]
    public void CategorySidebar_UnpinnedHoverFlow_ExpandsAndCollapses()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            var settings = new AppSettings
            {
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

            try
            {
                var sidebar = Assert.IsType<Border>(window.FindName("CategorySidebarContainer"));
                var hoverZone = Assert.IsType<Border>(window.FindName("CategorySidebarHoverZone"));
                var mainContent = Assert.IsType<Grid>(window.FindName("MainContentContainer"));
                var pinToggle = Assert.IsType<ToggleButton>(window.FindName("CategorySidebarPinToggleButton"));

                Assert.True(window.IsCategorySidebarExpanded);
                Assert.True(sidebar.ActualWidth > 120);
                Assert.True(mainContent.Margin.Left > 120);

                pinToggle.IsChecked = false;
                WaitUntil(
                    () => !window.IsCategorySidebarExpanded &&
                          sidebar.Visibility == Visibility.Collapsed &&
                          Math.Abs(mainContent.Margin.Left) < 0.1,
                    TimeSpan.FromSeconds(2),
                    window,
                    sidebar);

                hoverZone.RaiseEvent(CreateMouseEventArgs(UIElement.MouseEnterEvent));
                WaitUntil(
                    () => window.IsCategorySidebarExpanded &&
                          sidebar.Visibility == Visibility.Visible &&
                          sidebar.ActualWidth > 120 &&
                          Math.Abs(mainContent.Margin.Left) < 0.1,
                    TimeSpan.FromSeconds(2),
                    window,
                    sidebar);

                hoverZone.RaiseEvent(CreateMouseEventArgs(UIElement.MouseLeaveEvent));
                sidebar.RaiseEvent(CreateMouseEventArgs(UIElement.MouseEnterEvent));

                sidebar.RaiseEvent(CreateMouseEventArgs(UIElement.MouseLeaveEvent));
                WaitUntil(
                    () => !window.IsCategorySidebarExpanded &&
                          sidebar.Visibility == Visibility.Collapsed &&
                          Math.Abs(mainContent.Margin.Left) < 0.1,
                    TimeSpan.FromSeconds(2),
                    window,
                    sidebar);
            }
            finally
            {
                window.Close();
                WpfTestHost.DoEvents();
            }
        });
    }

    private static MouseEventArgs CreateMouseEventArgs(RoutedEvent routedEvent)
    {
        return new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
        {
            RoutedEvent = routedEvent,
        };
    }

    private static void WaitUntil(Func<bool> condition, TimeSpan timeout, MainWindow window, FrameworkElement sidebar)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Timed out waiting for sidebar state transition. Expanded={window.IsCategorySidebarExpanded}, Width={sidebar.ActualWidth}.");
            }

            window.UpdateLayout();
            WpfTestHost.DoEvents();
            Thread.Sleep(10);
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