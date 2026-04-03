using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using applanch.Controls;
using Xunit;

namespace applanch.Tests.Controls;

public class HeaderBarControlTests
{
    [Fact]
    public void UpdateButtonVisibilityProperty_CanSetAndGet()
    {
        RunInSta(() =>
        {
            var control = new HeaderBarControl();

            control.UpdateButtonVisibility = Visibility.Visible;

            Assert.Equal(Visibility.Visible, control.UpdateButtonVisibility);
        });
    }

    [Fact]
    public void UpdateButtonClick_RaisesUpdateRequestedEvent()
    {
        RunInSta(() =>
        {
            var control = new HeaderBarControl();
            var raised = false;
            control.UpdateRequested += (_, _) => raised = true;

            InvokePrivateClick(control, "UpdateButton_Click");

            Assert.True(raised);
        });
    }

    [Fact]
    public void SettingsButtonClick_RaisesSettingsRequestedEvent()
    {
        RunInSta(() =>
        {
            var control = new HeaderBarControl();
            var raised = false;
            control.SettingsRequested += (_, _) => raised = true;

            InvokePrivateClick(control, "SettingsButton_Click");

            Assert.True(raised);
        });
    }

    [Fact]
    public void SettingsButtonStyle_HoverChangesForegroundWithoutBackgroundHighlight()
    {
        RunInSta(() =>
        {
            var control = new HeaderBarControl();

            var style = Assert.IsType<Style>(control.Resources["HeaderFlatHoverTextButtonStyle"]);
            var templateSetter = Assert.Single(style.Setters.OfType<Setter>(), static setter => setter.Property == Control.TemplateProperty);
            var template = Assert.IsType<ControlTemplate>(templateSetter.Value);
            var hoverTrigger = Assert.Single(template.Triggers.OfType<Trigger>(),
                static trigger => trigger.Property == UIElement.IsMouseOverProperty && Equals(trigger.Value, true));

            Assert.Contains(hoverTrigger.Setters.OfType<Setter>(), static setter => setter.Property == Control.ForegroundProperty);

            var backgroundSetter = Assert.Single(
                hoverTrigger.Setters.OfType<Setter>(),
                static setter =>
                    setter.TargetName == "Bd" &&
                    setter.Property == Border.BackgroundProperty);
            var brush = Assert.IsType<SolidColorBrush>(backgroundSetter.Value);
            Assert.Equal(Colors.Transparent, brush.Color);
        });
    }

    private static void InvokePrivateClick(HeaderBarControl control, string methodName)
    {
        var method = typeof(HeaderBarControl).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(control, [new Button(), new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)]);
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
