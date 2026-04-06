using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using applanch.Controls;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Controls;

public class FloatingNotificationControlTests
{
    [Fact]
    public void ShowNotification_SetsVisibilityVisible()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();
            var control = new FloatingNotificationControl();

            control.ShowNotification();

            Assert.Equal(Visibility.Visible, control.Visibility);
        });
    }

    [Fact]
    public void HideNotification_WhenAlreadyHidden_RaisesHiddenEventAndStaysCollapsed()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();
            var control = new FloatingNotificationControl
            {
                Visibility = Visibility.Collapsed,
            };
            var hiddenRaised = false;
            control.Hidden += (_, _) => hiddenRaised = true;

            control.HideNotification();

            Assert.True(hiddenRaised);
            Assert.Equal(Visibility.Collapsed, control.Visibility);
        });
    }

    [Fact]
    public void ActionButtonClick_RaisesActionRequested()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();
            var control = new FloatingNotificationControl();
            var requested = false;
            control.ActionRequested += (_, _) => requested = true;

            var method = typeof(FloatingNotificationControl).GetMethod("ActionButton_Click", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method!.Invoke(control, [new Button(), new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)]);

            Assert.True(requested);
        });
    }
}
