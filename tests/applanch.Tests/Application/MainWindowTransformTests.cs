using System.Windows.Controls;
using System.Windows.Media;
using Xunit;

namespace applanch.Tests.Application;

public class MainWindowTransformTests
{
    [Fact]
    public void EnsureTranslateTransform_WhenRenderTransformIsIdentity_WrapsAndAppendsTranslate()
    {
        RunInSta(() =>
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
        RunInSta(() =>
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
        RunInSta(() =>
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
        RunInSta(() =>
        {
            var original = new ScaleTransform(1.2, 1.2);
            var element = new Border { RenderTransform = original };

            var translate = MainWindow.EnsureTranslateTransform(element);

            var group = Assert.IsType<TransformGroup>(element.RenderTransform);
            Assert.Same(original, group.Children[0]);
            Assert.Same(translate, group.Children[1]);
        });
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
            throw new Xunit.Sdk.XunitException($"STA test failed: {captured}");
        }
    }
}
