using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using applanch.Controls;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Controls;

public sealed class VirtualizingWrapPanelTests
{
    [Fact]
    public void MeasureOverride_WithManyItemsBeyondViewport_OnlyRealizesVisibleContainers()
    {
        WpfTestHost.RunInSta((Action)(() =>
        {
            WpfTestHost.EnsureAppResources();

            // 40 items, viewport fits 2 per row, ~2 visible rows → expect well under 40 realized
            var listBox = CreateVirtualizingListBox(itemCount: 40, itemWidth: 80, itemHeight: 80);
            var window = new Window { Content = listBox, Width = 200, Height = 180, WindowStyle = WindowStyle.None };
            window.Show();
            window.UpdateLayout();

            try
            {
                var realized = CountRealizedContainers(listBox, totalItems: 40);
                Assert.True(realized < 40, $"Expected fewer than 40 realized containers, got {realized}");
            }
            finally
            {
                window.Close();
            }
        }));
    }

    [Fact]
    public void ScrollIntoView_WhenItemBeyondViewport_ChangesVerticalOffset()
    {
        WpfTestHost.RunInSta((Action)(() =>
        {
            WpfTestHost.EnsureAppResources();

            // 20 items, 1 per row (viewport width ≈ item width), viewport shows ~2 rows
            const int itemCount = 20;
            var listBox = CreateVirtualizingListBox(itemCount: itemCount, itemWidth: 90, itemHeight: 90);
            var window = new Window { Content = listBox, Width = 100, Height = 190, WindowStyle = WindowStyle.None };
            window.Show();
            window.UpdateLayout();

            try
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
                Assert.NotNull(scrollViewer);

                double initialOffset = scrollViewer.VerticalOffset;

                // Scroll to last item — panel's BringIndexIntoView should move the offset
                listBox.ScrollIntoView(listBox.Items[itemCount - 1]);
                window.UpdateLayout();

                double offsetAfterScrollToEnd = scrollViewer.VerticalOffset;
                Assert.True(offsetAfterScrollToEnd > initialOffset,
                    $"Expected offset to increase after ScrollIntoView(last), was {initialOffset} → {offsetAfterScrollToEnd}");

                // Scroll back to first item
                listBox.ScrollIntoView(listBox.Items[0]);
                window.UpdateLayout();

                double offsetAfterScrollToStart = scrollViewer.VerticalOffset;
                Assert.True(offsetAfterScrollToStart < offsetAfterScrollToEnd,
                    $"Expected offset to decrease after ScrollIntoView(first), was {offsetAfterScrollToEnd} → {offsetAfterScrollToStart}");
            }
            finally
            {
                window.Close();
            }
        }));
    }

    private static ListBox CreateVirtualizingListBox(int itemCount, double itemWidth, double itemHeight)
    {
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(FrameworkElement.WidthProperty, itemWidth);
        factory.SetValue(FrameworkElement.HeightProperty, itemHeight);

        var listBox = new ListBox
        {
            ItemTemplate = new DataTemplate { VisualTree = factory },
            ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingWrapPanel))),
        };

        ScrollViewer.SetCanContentScroll(listBox, true);
        VirtualizingPanel.SetIsVirtualizing(listBox, true);
        VirtualizingPanel.SetVirtualizationMode(listBox, VirtualizationMode.Recycling);

        for (var i = 0; i < itemCount; i++)
        {
            listBox.Items.Add($"Item {i}");
        }

        return listBox;
    }

    private static int CountRealizedContainers(ListBox listBox, int totalItems)
    {
        var count = 0;
        for (var i = 0; i < totalItems; i++)
        {
            if (listBox.ItemContainerGenerator.ContainerFromIndex(i) is not null)
            {
                count++;
            }
        }

        return count;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
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
}
