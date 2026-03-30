using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
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

    private static void InvokePrivateClick(HeaderBarControl control, string methodName)
    {
        var method = typeof(HeaderBarControl).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
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
