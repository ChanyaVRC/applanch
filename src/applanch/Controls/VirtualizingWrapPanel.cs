using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace applanch.Controls;

public sealed class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
{
    private const double DefaultItemWidth = 80d;
    private const double DefaultItemHeight = 80d;

    private Size _extent = new(0, 0);
    private Size _viewport = new(0, 0);
    private Point _offset;
    private Size _itemSize = new(DefaultItemWidth, DefaultItemHeight);

    public bool CanVerticallyScroll { get; set; } = true;

    public bool CanHorizontallyScroll { get; set; }

    public double ExtentWidth => _extent.Width;

    public double ExtentHeight => _extent.Height;

    public double ViewportWidth => _viewport.Width;

    public double ViewportHeight => _viewport.Height;

    public double HorizontalOffset => _offset.X;

    public double VerticalOffset => _offset.Y;

    public ScrollViewer? ScrollOwner { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var itemsControl = ItemsControl.GetItemsOwner(this);
        if (itemsControl is null)
        {
            return availableSize;
        }

        var itemCount = itemsControl.Items.Count;
        if (itemCount == 0)
        {
            _extent = new Size(0, 0);
            _viewport = availableSize;
            ScrollOwner?.InvalidateScrollInfo();
            RemoveInternalChildRange(0, InternalChildren.Count);
            return availableSize;
        }

        var viewportWidth = double.IsInfinity(availableSize.Width) ? ActualWidth : availableSize.Width;
        var viewportHeight = double.IsInfinity(availableSize.Height) ? ActualHeight : availableSize.Height;

        if (viewportWidth <= 0)
        {
            viewportWidth = _viewport.Width > 0 ? _viewport.Width : _itemSize.Width;
        }

        if (viewportHeight <= 0)
        {
            viewportHeight = _viewport.Height > 0 ? _viewport.Height : _itemSize.Height;
        }

        _viewport = new Size(viewportWidth, viewportHeight);

        var itemsPerRow = Math.Max(1, (int)Math.Floor(viewportWidth / _itemSize.Width));
        var firstVisibleRow = Math.Max(0, (int)Math.Floor(VerticalOffset / _itemSize.Height));
        var visibleRowCount = Math.Max(1, (int)Math.Ceiling(viewportHeight / _itemSize.Height) + 1);

        var startIndex = Math.Max(0, firstVisibleRow * itemsPerRow);
        var endIndex = Math.Min(itemCount - 1, ((firstVisibleRow + visibleRowCount) * itemsPerRow) - 1);

        RealizeItems(itemsControl, startIndex, endIndex);
        CleanupItems(startIndex, endIndex);

        _itemSize = MeasureRealizedChildren();

        itemsPerRow = Math.Max(1, (int)Math.Floor(viewportWidth / _itemSize.Width));
        var rowCount = (int)Math.Ceiling((double)itemCount / itemsPerRow);
        _extent = new Size(itemsPerRow * _itemSize.Width, rowCount * _itemSize.Height);

        CoerceOffsets();
        ScrollOwner?.InvalidateScrollInfo();

        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var viewportWidth = finalSize.Width;
        var itemsPerRow = Math.Max(1, (int)Math.Floor(viewportWidth / _itemSize.Width));

        for (var childIndex = 0; childIndex < InternalChildren.Count; childIndex++)
        {
            if (InternalChildren[childIndex] is not UIElement child)
            {
                continue;
            }

            var itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(new GeneratorPosition(childIndex, 0));
            if (itemIndex < 0)
            {
                continue;
            }

            var row = itemIndex / itemsPerRow;
            var column = itemIndex % itemsPerRow;

            var x = (column * _itemSize.Width) - HorizontalOffset;
            var y = (row * _itemSize.Height) - VerticalOffset;

            child.Arrange(new Rect(new Point(x, y), _itemSize));
        }

        return finalSize;
    }

    public void LineUp() => SetVerticalOffset(VerticalOffset - _itemSize.Height * 0.25);

    public void LineDown() => SetVerticalOffset(VerticalOffset + _itemSize.Height * 0.25);

    public void LineLeft()
    {
    }

    public void LineRight()
    {
    }

    public void MouseWheelUp() => SetVerticalOffset(VerticalOffset - _itemSize.Height);

    public void MouseWheelDown() => SetVerticalOffset(VerticalOffset + _itemSize.Height);

    public void MouseWheelLeft()
    {
    }

    public void MouseWheelRight()
    {
    }

    public void PageUp() => SetVerticalOffset(VerticalOffset - ViewportHeight);

    public void PageDown() => SetVerticalOffset(VerticalOffset + ViewportHeight);

    public void PageLeft()
    {
    }

    public void PageRight()
    {
    }

    public Rect MakeVisible(Visual visual, Rect rectangle)
    {
        return rectangle;
    }

    public void SetHorizontalOffset(double offset)
    {
        if (!CanHorizontallyScroll)
        {
            return;
        }

        _offset.X = Math.Max(0, Math.Min(offset, ExtentWidth - ViewportWidth));
        InvalidateArrange();
        ScrollOwner?.InvalidateScrollInfo();
    }

    public void SetVerticalOffset(double offset)
    {
        if (!CanVerticallyScroll)
        {
            return;
        }

        var newOffset = Math.Max(0, Math.Min(offset, ExtentHeight - ViewportHeight));
        if (Math.Abs(newOffset - _offset.Y) < 0.1)
        {
            return;
        }

        _offset.Y = newOffset;
        InvalidateMeasure();
        ScrollOwner?.InvalidateScrollInfo();
    }

    private void RealizeItems(ItemsControl itemsControl, int startIndex, int endIndex)
    {
        var generator = ItemContainerGenerator;
        var startPosition = generator.GeneratorPositionFromIndex(startIndex);
        var childIndex = startPosition.Offset == 0 ? startPosition.Index : startPosition.Index + 1;

        using var _ = generator.StartAt(startPosition, GeneratorDirection.Forward, allowStartAtRealizedItem: true);

        for (var itemIndex = startIndex; itemIndex <= endIndex; itemIndex++, childIndex++)
        {
            var child = generator.GenerateNext(out var newlyRealized) as UIElement;
            if (child is null)
            {
                continue;
            }

            if (newlyRealized)
            {
                if (childIndex >= InternalChildren.Count)
                {
                    AddInternalChild(child);
                }
                else
                {
                    InsertInternalChild(childIndex, child);
                }

                generator.PrepareItemContainer(child);
            }
        }
    }

    private void CleanupItems(int startIndex, int endIndex)
    {
        for (var childIndex = InternalChildren.Count - 1; childIndex >= 0; childIndex--)
        {
            var itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(new GeneratorPosition(childIndex, 0));
            if (itemIndex < startIndex || itemIndex > endIndex)
            {
                ItemContainerGenerator.Remove(new GeneratorPosition(childIndex, 0), 1);
                RemoveInternalChildRange(childIndex, 1);
            }
        }
    }

    private Size MeasureRealizedChildren()
    {
        var measuredSize = _itemSize;

        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            if (child.DesiredSize.Width > 0 && child.DesiredSize.Height > 0)
            {
                measuredSize = child.DesiredSize;
            }
        }

        if (measuredSize.Width <= 0 || measuredSize.Height <= 0)
        {
            return new Size(DefaultItemWidth, DefaultItemHeight);
        }

        return measuredSize;
    }

    private void CoerceOffsets()
    {
        _offset.X = Math.Max(0, Math.Min(_offset.X, Math.Max(0, ExtentWidth - ViewportWidth)));
        _offset.Y = Math.Max(0, Math.Min(_offset.Y, Math.Max(0, ExtentHeight - ViewportHeight)));
    }
}
