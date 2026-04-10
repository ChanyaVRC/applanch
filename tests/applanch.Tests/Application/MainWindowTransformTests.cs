using System.Windows.Controls;
using System.Windows.Media;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Application;

[Collection("WpfTests")]
public class MainWindowTransformTests
{
    [Fact]
    public void HasSignificantReorderDelta_WhenOnlyHorizontalDeltaIsLarge_ReturnsTrue()
    {
        var result = MainWindow.HasSignificantReorderDelta(deltaX: 1.0, deltaY: 0.1);

        Assert.True(result);
    }

    [Fact]
    public void HasSignificantReorderDelta_WhenOnlyVerticalDeltaIsLarge_ReturnsTrue()
    {
        var result = MainWindow.HasSignificantReorderDelta(deltaX: 0.1, deltaY: 1.0);

        Assert.True(result);
    }

    [Fact]
    public void HasSignificantReorderDelta_WhenBothDeltasAreSmall_ReturnsFalse()
    {
        var result = MainWindow.HasSignificantReorderDelta(deltaX: 0.2, deltaY: 0.4);

        Assert.False(result);
    }

    [Fact]
    public void EnsureTranslateTransform_WhenRenderTransformIsIdentity_WrapsAndAppendsTranslate()
    {
        WpfTestHost.RunInSta(() =>
        {
            var element = new Border();

            var translate = MainWindow.EnsureTranslateTransform(element);

            var group = Assert.IsType<TransformGroup>(element.RenderTransform);
            Assert.Same(Transform.Identity, group.Children[0]);
            Assert.Same(translate, group.Children[1]);
        });
    }

    [Fact]
    public void EnsureTranslateTransform_WhenRenderTransformIsTranslate_ReturnsExisting()
    {
        WpfTestHost.RunInSta(() =>
        {
            var existing = new TranslateTransform();
            var element = new Border { RenderTransform = existing };

            var translate = MainWindow.EnsureTranslateTransform(element);

            Assert.Same(existing, translate);
        });
    }

    [Fact]
    public void EnsureTranslateTransform_WhenTransformGroupContainsTranslate_ReturnsExisting()
    {
        WpfTestHost.RunInSta(() =>
        {
            var existing = new TranslateTransform();
            var group = new TransformGroup();
            group.Children.Add(new ScaleTransform(2, 2));
            group.Children.Add(existing);
            var element = new Border { RenderTransform = group };

            var translate = MainWindow.EnsureTranslateTransform(element);

            Assert.Same(existing, translate);
            Assert.Equal(2, group.Children.Count);
        });
    }

    [Fact]
    public void EnsureTranslateTransform_WhenRenderTransformIsNonGroup_WrapsAndAppendsTranslate()
    {
        WpfTestHost.RunInSta(() =>
        {
            var original = new ScaleTransform(1.2, 1.2);
            var element = new Border { RenderTransform = original };

            var translate = MainWindow.EnsureTranslateTransform(element);

            var group = Assert.IsType<TransformGroup>(element.RenderTransform);
            Assert.Same(original, group.Children[0]);
            Assert.Same(translate, group.Children[1]);
        });
    }

}
