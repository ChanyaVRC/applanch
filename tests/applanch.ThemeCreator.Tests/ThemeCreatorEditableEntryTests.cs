using System.Windows.Media;
using applanch.ThemeCreator;
using applanch.Theming;
using Xunit;

namespace applanch.Tests.ThemeCreator;

public sealed class ThemeCreatorEditableEntryTests
{
    [Fact]
    public void PreviewBrush_WithSameHex_ReusesCachedBrush()
    {
        var entry = new ThemeCreatorEditableEntry("Brush.AppBackground", "Background", ThemeColor.Parse("#112233"));

        var first = entry.PreviewBrush;
        var second = entry.PreviewBrush;

        Assert.Same(first, second);
        Assert.True(((SolidColorBrush)first).IsFrozen);
    }

    [Fact]
    public void PreviewBrush_WhenHexChanges_RebuildsBrush()
    {
        var entry = new ThemeCreatorEditableEntry("Brush.AppBackground", "Background", ThemeColor.Parse("#112233"));
        var original = entry.PreviewBrush;

        entry.Hex = "#445566";
        var updated = entry.PreviewBrush;

        Assert.NotSame(original, updated);
        Assert.True(((SolidColorBrush)updated).IsFrozen);
    }
}

