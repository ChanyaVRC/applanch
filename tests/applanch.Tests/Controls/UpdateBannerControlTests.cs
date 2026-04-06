using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using applanch.Controls;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Controls;

[Collection("WpfTests")]
public class UpdateBannerControlTests
{
    [Fact]
    public void MessageProperty_CanSetAndGet()
    {
        WpfTestHost.RunInSta(() =>
        {
            var control = new UpdateBannerControl();

            control.Message = "Update available";

            Assert.Equal("Update available", control.Message);
        });
    }

    [Fact]
    public void UpdateButtonClick_RaisesUpdateRequestedEvent()
    {
        WpfTestHost.RunInSta(() =>
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
        WpfTestHost.RunInSta(() =>
        {
            var control = new UpdateBannerControl();
            var raised = false;
            control.DismissRequested += (_, _) => raised = true;

            InvokePrivateClick(control, "DismissButton_Click");

            Assert.True(raised);
        });
    }

    [Fact]
    public void UpdateActionButtonVisibilityProperty_UpdatesUpdateActionButtonVisibility()
    {
        WpfTestHost.RunInSta(() =>
        {
            var control = new UpdateBannerControl();
            var updateActionButton = Assert.IsType<Button>(control.FindName("UpdateActionButton"));

            Assert.Equal(Visibility.Visible, updateActionButton.Visibility);

            control.UpdateActionButtonVisibility = Visibility.Collapsed;
            WpfTestHost.DoEvents();

            Assert.Equal(Visibility.Collapsed, updateActionButton.Visibility);
        });
    }

    private static void InvokePrivateClick(UpdateBannerControl control, string methodName)
    {
        var method = typeof(UpdateBannerControl).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(control, [new Button(), new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)]);
    }

}
