using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using applanch.Controls;
using Xunit;

namespace applanch.Tests.Controls;

public class UpdateBannerControlTests
{
    [Fact]
    public void MessageProperty_CanSetAndGet()
    {
        RunInSta(() =>
        {
            var control = new UpdateBannerControl();

            control.Message = "Update available";

            Assert.Equal("Update available", control.Message);
        });
    }

    [Fact]
    public void UpdateButtonClick_RaisesUpdateRequestedEvent()
    {
        RunInSta(() =>
        {
            var control = new UpdateBannerControl();
            var raised = false;
            control.UpdateRequested += (_, _) => raised = true;

            InvokePrivateClick(control, "UpdateButton_Click");

            Assert.True(raised);
        });
    }

    [Fact]
    public void DismissButtonClick_RaisesDismissRequestedEvent()
    {
        RunInSta(() =>
        {
            var control = new UpdateBannerControl();
            var raised = false;
            control.DismissRequested += (_, _) => raised = true;

            InvokePrivateClick(control, "DismissButton_Click");

            Assert.True(raised);
        });
    }

    [Fact]
    public void UpdateActionButtonStyle_HoverChangesForegroundWithoutBackgroundHighlight()
    {
        RunInSta(() =>
        {
            var control = new UpdateBannerControl();

            var style = Assert.IsType<Style>(control.Resources["BannerFlatActionButtonStyle"]);
            var templateSetter = Assert.Single(style.Setters.OfType<Setter>(), static setter => setter.Property == Control.TemplateProperty);
            var template = Assert.IsType<ControlTemplate>(templateSetter.Value);
            var hoverTrigger = Assert.Single(template.Triggers.OfType<Trigger>(),
                static trigger => trigger.Property == UIElement.IsMouseOverProperty && Equals(trigger.Value, true));

            Assert.Contains(hoverTrigger.Setters.OfType<Setter>(), static setter => setter.Property == Control.ForegroundProperty);

            var backgroundSetter = Assert.Single(
                style.Setters.OfType<Setter>(),
                static setter => setter.Property == Control.BackgroundProperty);
            var brush = Assert.IsType<SolidColorBrush>(backgroundSetter.Value);
            Assert.Equal(Colors.Transparent, brush.Color);
        });
    }

    private static void InvokePrivateClick(UpdateBannerControl control, string methodName)
    {
        var method = typeof(UpdateBannerControl).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(control, [new Button(), new RoutedEventArgs(Button.ClickEvent)]);
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
