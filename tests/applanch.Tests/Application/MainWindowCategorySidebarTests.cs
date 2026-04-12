using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Utilities;
using applanch.Tests.TestSupport;
using applanch.Tests.ViewModels.TestDoubles;
using applanch.ViewModels;
using Xunit;
using Strings = applanch.Properties.Resources;

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
                CategorySidebarPinned = false,
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

    [Fact]
    public void CategorySidebar_DragToCategory_ExpandsAndUpdatesItemOutsideManualSort()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            var settings = new AppSettings
            {
                AppListSortMode = AppListSortMode.Name,
                CategorySidebarPinned = false,
                CheckForUpdatesOnStartup = false,
            };

            var store = new FakeStore(
            [
                new LauncherEntry(new LaunchPath(@"C:\Tools\App.exe"), Category.FromInput("Dev"), string.Empty, "App"),
                new LauncherEntry(new LaunchPath(@"C:\Tools\Ops.exe"), Category.FromInput("Ops"), string.Empty, "OpsApp")
            ]);

            var viewModel = new MainWindowViewModel(
                new FakeResolver(),
                store,
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
                var createDropTarget = Assert.IsType<Border>(window.FindName("CategorySidebarCreateDropTarget"));

                WaitUntil(
                    () => !window.IsCategorySidebarExpanded && sidebar.Visibility == Visibility.Collapsed,
                    TimeSpan.FromSeconds(2),
                    window,
                    sidebar);
                Assert.Equal(Visibility.Collapsed, createDropTarget.Visibility);

                var data = new DataObject(typeof(LaunchItemViewModel), viewModel.LaunchItems[0]);

                Assert.Equal(DragDropEffects.None, window.GetCategorySidebarDropEffect(data, sidebar));
                WaitUntil(
                    () => window.IsCategorySidebarExpanded &&
                          sidebar.Visibility == Visibility.Visible &&
                          sidebar.ActualWidth > 120,
                    TimeSpan.FromSeconds(2),
                    window,
                    sidebar);
                Assert.Equal(Visibility.Visible, createDropTarget.Visibility);

                var categoryListBox = Assert.IsType<ListBox>(VisualTreeUtilities.FindVisualChild<ListBox>(sidebar));
                categoryListBox.UpdateLayout();
                var opsItem = Assert.IsType<ListBoxItem>(categoryListBox.ItemContainerGenerator.ContainerFromItem(Category.FromInput("Ops")));

                Assert.Equal(DragDropEffects.Move, window.GetCategorySidebarDropEffect(data, opsItem));
                Assert.True(window.IsCategoryDropTargetHighlighted(opsItem));
                window.ApplyCategoryDrop(data, opsItem);
                Assert.Equal("Ops", viewModel.LaunchItems[0].Category.Value);
                Assert.Equal(1, store.SaveCallCount);
                Assert.Equal(
                    string.Format(Strings.Notification_ItemCategoryChanged, "App", "Dev", "Ops"),
                    viewModel.FloatingNotification.Message);
                Assert.Equal(NotificationIconType.Info, viewModel.FloatingNotification.IconType);

                window.HandleCategorySidebarContainerDragLeave();

                WaitUntil(
                    () => !window.IsCategoryDropTargetHighlighted(opsItem) &&
                          createDropTarget.Visibility == Visibility.Collapsed,
                    TimeSpan.FromSeconds(2),
                    window,
                    sidebar);

                WaitUntil(
                    () => !window.IsCategorySidebarExpanded && sidebar.Visibility == Visibility.Collapsed,
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

    [Fact]
    public void CategorySidebar_DropOnCreateTarget_PromptsForCategoryAndMovesItem()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            var settings = new AppSettings
            {
                AppListSortMode = AppListSortMode.Name,
                CategorySidebarPinned = false,
                CheckForUpdatesOnStartup = false,
            };

            var interaction = new FakeInteractionService
            {
                PromptWithSuggestionsResult = "Research",
            };

            var store = new FakeStore(
            [
                new LauncherEntry(new LaunchPath(@"C:\Tools\App.exe"), Category.FromInput("Dev"), string.Empty, "App"),
                new LauncherEntry(new LaunchPath(@"C:\Tools\Ops.exe"), Category.FromInput("Ops"), string.Empty, "OpsApp")
            ]);

            var viewModel = new MainWindowViewModel(
                new FakeResolver(),
                store,
                settings);

            var window = new MainWindow(
                viewModel,
                new FakeLaunchService(),
                interaction,
                static _ => new FakeUpdateService(),
                settings);

            WpfTestHost.ShowOffscreen(window);
            window.UpdateLayout();

            try
            {
                var sidebar = Assert.IsType<Border>(window.FindName("CategorySidebarContainer"));
                var createDropTarget = Assert.IsType<Border>(window.FindName("CategorySidebarCreateDropTarget"));

                WaitUntil(
                    () => !window.IsCategorySidebarExpanded && sidebar.Visibility == Visibility.Collapsed,
                    TimeSpan.FromSeconds(2),
                    window,
                    sidebar);

                var data = new DataObject(typeof(LaunchItemViewModel), viewModel.LaunchItems[0]);
                Assert.Equal(DragDropEffects.None, window.GetCategorySidebarDropEffect(data, sidebar));

                WaitUntil(
                    () => window.IsCategorySidebarExpanded && createDropTarget.Visibility == Visibility.Visible,
                    TimeSpan.FromSeconds(2),
                    window,
                    sidebar);

                Assert.Equal(DragDropEffects.Move, window.GetCategoryCreateDropEffect(data));
                Assert.True(window.IsCategoryCreateDropTargetActive);
                Assert.True(window.ApplyCategoryCreateDrop(data));

                Assert.Equal("Research", viewModel.LaunchItems[0].Category.Value);
                Assert.Equal(1, store.SaveCallCount);
                Assert.Equal(Strings.Prompt_CreateCategory, interaction.LastPromptWithSuggestionsTitle);
                Assert.Equal(["Dev", "Ops", AppResources.DefaultCategory], interaction.LastSuggestions);
                Assert.Equal(
                    string.Format(Strings.Notification_ItemCategoryChanged, "App", "Dev", "Research"),
                    viewModel.FloatingNotification.Message);
            }
            finally
            {
                window.Close();
                WpfTestHost.DoEvents();
            }
        });
    }

    [Fact]
    public void CategorySidebar_LaunchItemDragSession_DoesNotExpandImmediately()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            var settings = new AppSettings
            {
                AppListSortMode = AppListSortMode.Name,
                CategorySidebarPinned = false,
                CheckForUpdatesOnStartup = false,
            };

            var store = new FakeStore(
            [
                new LauncherEntry(new LaunchPath(@"C:\Tools\App.exe"), Category.FromInput("Dev"), string.Empty, "App")
            ]);

            var viewModel = new MainWindowViewModel(
                new FakeResolver(),
                store,
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
                var createDropTarget = Assert.IsType<Border>(window.FindName("CategorySidebarCreateDropTarget"));
                var data = new DataObject(typeof(LaunchItemViewModel), viewModel.LaunchItems[0]);

                WaitUntil(
                    () => !window.IsCategorySidebarExpanded && sidebar.Visibility == Visibility.Collapsed,
                    TimeSpan.FromSeconds(2),
                    window,
                    sidebar);
                Assert.Equal(Visibility.Collapsed, createDropTarget.Visibility);

                window.SetLaunchItemCategoryDragSession(isActive: true);

                WaitUntil(
                    () => !window.IsCategorySidebarExpanded &&
                          sidebar.Visibility == Visibility.Collapsed,
                    TimeSpan.FromSeconds(2),
                    window,
                    sidebar);

                Assert.Equal(DragDropEffects.None, window.GetCategorySidebarDropEffect(data, sidebar));

                WaitUntil(
                    () => window.IsCategorySidebarExpanded &&
                          sidebar.Visibility == Visibility.Visible &&
                          createDropTarget.Visibility == Visibility.Visible,
                    TimeSpan.FromSeconds(2),
                    window,
                    sidebar);

                window.SetLaunchItemCategoryDragSession(isActive: false);

                WaitUntil(
                    () => !window.IsCategorySidebarExpanded &&
                          sidebar.Visibility == Visibility.Collapsed &&
                          createDropTarget.Visibility == Visibility.Collapsed,
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

    [Fact]
    public void MoveItemToCategory_ShowsFloatingNotification()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            var settings = new AppSettings
            {
                CheckForUpdatesOnStartup = false,
            };

            var store = new FakeStore(
            [
                new LauncherEntry(new LaunchPath(@"C:\Tools\App.exe"), Category.FromInput("Dev"), string.Empty, "App")
            ]);

            var viewModel = new MainWindowViewModel(
                new FakeResolver(),
                store,
                settings);

            var window = new MainWindow(
                viewModel,
                new FakeLaunchService(),
                new FakeInteractionService(),
                static _ => new FakeUpdateService(),
                settings);

            WpfTestHost.ShowOffscreen(window);

            try
            {
                window.MoveItemToCategory(viewModel.LaunchItems[0], Category.FromInput("Ops"));

                Assert.Equal("Ops", viewModel.LaunchItems[0].Category.Value);
                Assert.Equal(1, store.SaveCallCount);
                Assert.Equal(
                    string.Format(Strings.Notification_ItemCategoryChanged, "App", "Dev", "Ops"),
                    viewModel.FloatingNotification.Message);
                Assert.Equal(NotificationIconType.Info, viewModel.FloatingNotification.IconType);
            }
            finally
            {
                window.Close();
                WpfTestHost.DoEvents();
            }
        });
    }

    [Fact]
    public void MoveItemToCategory_BlankCategoryMovesToDefaultCategory()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            var settings = new AppSettings
            {
                CheckForUpdatesOnStartup = false,
            };

            var store = new FakeStore(
            [
                new LauncherEntry(new LaunchPath(@"C:\Tools\App.exe"), Category.FromInput("Dev"), string.Empty, "App")
            ]);

            var viewModel = new MainWindowViewModel(
                new FakeResolver(),
                store,
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
                window.MoveItemToCategory(viewModel.LaunchItems[0], Category.FromInput(" "));

                Assert.Equal(LauncherEntry.DefaultCategory, viewModel.LaunchItems[0].Category.Value);
                Assert.Equal(1, store.SaveCallCount);
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
}