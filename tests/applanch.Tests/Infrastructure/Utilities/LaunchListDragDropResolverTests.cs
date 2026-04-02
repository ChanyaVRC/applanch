using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using applanch.Infrastructure.Utilities;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.Infrastructure.Utilities;

public class LaunchListDragDropResolverTests
{
    [Fact]
    public void TryGetDraggedItemData_WithValidItem_ReturnsItemAndIndex()
    {
        RunInSta(() =>
        {
            var first = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath("a"), "Dev", string.Empty, "A");
            var second = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath("b"), "Dev", string.Empty, "B");
            var items = new List<LaunchItemViewModel> { first, second };
            var data = new DataObject(typeof(LaunchItemViewModel), second);
            var sut = new LaunchListDragDropResolver();

            var success = sut.TryGetDraggedItemData(data, items, out var draggedItem, out var oldIndex);

            Assert.True(success);
            Assert.Same(second, draggedItem);
            Assert.Equal(1, oldIndex);
        });
    }

    [Fact]
    public void TryGetDraggedItemData_WhenDataMissing_ReturnsFalse()
    {
        RunInSta(() =>
        {
            var items = new List<LaunchItemViewModel>
            {
                new(new LaunchPath("a"), "Dev", string.Empty, "A")
            };
            var data = new DataObject();
            var sut = new LaunchListDragDropResolver();

            var success = sut.TryGetDraggedItemData(data, items, out var draggedItem, out var oldIndex);

            Assert.False(success);
            Assert.Null(draggedItem);
            Assert.Equal(-1, oldIndex);
        });
    }

    [Fact]
    public void TryGetDraggedItemData_WhenItemNotInList_ReturnsFalse()
    {
        RunInSta(() =>
        {
            var items = new List<LaunchItemViewModel>
            {
                new(new LaunchPath("a"), "Dev", string.Empty, "A")
            };
            var outside = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath("x"), "Dev", string.Empty, "X");
            var data = new DataObject(typeof(LaunchItemViewModel), outside);
            var sut = new LaunchListDragDropResolver();

            var success = sut.TryGetDraggedItemData(data, items, out var draggedItem, out var oldIndex);

            Assert.False(success);
            Assert.Null(draggedItem);
            Assert.Equal(-1, oldIndex);
        });
    }

    [Fact]
    public void GetDropIndex_WhenDroppedAboveTop_ReturnsFirstIndex()
    {
        RunInSta(() =>
        {
            var items = new List<LaunchItemViewModel>
            {
                new(new LaunchPath("a"), "Dev", string.Empty, "A"),
                new(new LaunchPath("b"), "Dev", string.Empty, "B"),
                new(new LaunchPath("c"), "Dev", string.Empty, "C")
            };
            var listBox = CreateMeasuredListBox();
            var sut = new LaunchListDragDropResolver();

            var newIndex = sut.GetDropIndex(listBox, items, oldIndex: 2, new Point(10, -5));

            Assert.Equal(0, newIndex);
        });
    }

    [Fact]
    public void GetDropIndex_WhenDroppedBelowBottom_ReturnsLastIndex()
    {
        RunInSta(() =>
        {
            var items = new List<LaunchItemViewModel>
            {
                new(new LaunchPath("a"), "Dev", string.Empty, "A"),
                new(new LaunchPath("b"), "Dev", string.Empty, "B"),
                new(new LaunchPath("c"), "Dev", string.Empty, "C")
            };
            var listBox = CreateMeasuredListBox();
            var sut = new LaunchListDragDropResolver();

            var newIndex = sut.GetDropIndex(listBox, items, oldIndex: 0, new Point(10, listBox.ActualHeight + 1));

            Assert.Equal(2, newIndex);
        });
    }

    [Fact]
    public void GetDropIndex_WhenNoTargetContainerInMiddle_ReturnsOriginalIndex()
    {
        RunInSta(() =>
        {
            var items = new List<LaunchItemViewModel>
            {
                new(new LaunchPath("a"), "Dev", string.Empty, "A"),
                new(new LaunchPath("b"), "Dev", string.Empty, "B"),
                new(new LaunchPath("c"), "Dev", string.Empty, "C")
            };
            var listBox = CreateMeasuredListBox();
            var sut = new LaunchListDragDropResolver();

            var newIndex = sut.GetDropIndex(listBox, items, oldIndex: 1, new Point(10, 60));

            Assert.Equal(1, newIndex);
        });
    }

    private static ListBox CreateMeasuredListBox()
    {
        var listBox = new ListBox
        {
            Width = 240,
            Height = 160,
        };

        listBox.Measure(new Size(240, 160));
        listBox.Arrange(new Rect(0, 0, 240, 160));
        listBox.UpdateLayout();
        return listBox;
    }

    private static void RunInSta(Action action)
    {
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            ExceptionDispatchInfo.Capture(captured).Throw();
        }
    }
}
