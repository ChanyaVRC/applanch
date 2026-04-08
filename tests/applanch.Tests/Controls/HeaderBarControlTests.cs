using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using applanch.Controls;
using applanch.Infrastructure.Utilities;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Controls;

[Collection("WpfTests")]
public class HeaderBarControlTests
{
    [Fact]
    public void UpdateButtonVisibilityProperty_CanSetAndGet()
    {
        WpfTestHost.RunInSta(() =>
        {
            var control = new HeaderBarControl();

            control.UpdateButtonVisibility = Visibility.Visible;

            Assert.Equal(Visibility.Visible, control.UpdateButtonVisibility);
        });
    }

    [Fact]
    public void UpdateButtonClick_RaisesUpdateRequestedEvent()
    {
        WpfTestHost.RunInSta(() =>
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
        WpfTestHost.RunInSta(() =>
        {
            var control = new HeaderBarControl();
            var raised = false;
            control.SettingsRequested += (_, _) => raised = true;

            InvokePrivateClick(control, "SettingsButton_Click");

            Assert.True(raised);
        });
    }

    [Fact]
    public void UpdateButtonVisibilityProperty_UpdatesUpdateButtonVisibility()
    {
        WpfTestHost.RunInSta(() =>
        {
            var control = new HeaderBarControl();
            var updateButton = Assert.IsType<Button>(control.FindName("UpdateButton"));

            Assert.Equal(Visibility.Collapsed, updateButton.Visibility);

            control.UpdateButtonVisibility = Visibility.Visible;
            WpfTestHost.DoEvents();

            Assert.Equal(Visibility.Visible, updateButton.Visibility);
        });
    }

    [Fact]
    public void AppVersionText_DefaultsToCurrentVersion()
    {
        WpfTestHost.RunInSta(() =>
        {
            var control = new HeaderBarControl();

            Assert.Equal($"v{AppVersionProvider.GetDisplayVersion()}", control.AppVersionText);
        });
    }

    [Fact]
    public void AppVersionTextProperty_UpdatesHeaderVersionText()
    {
        WpfTestHost.RunInSta(() =>
        {
            var control = new HeaderBarControl();
            var versionText = Assert.IsType<TextBlock>(control.FindName("AppVersionTextBlock"));

            control.AppVersionText = "v9.9.9";
            WpfTestHost.DoEvents();

            Assert.Equal("v9.9.9", versionText.Text);
        });
    }

    [Fact]
    public void AppVersionText_AlignsToBottomOfTitleRow()
    {
        WpfTestHost.RunInSta(() =>
        {
            var control = new HeaderBarControl();
            var versionText = Assert.IsType<TextBlock>(control.FindName("AppVersionTextBlock"));

            Assert.Equal(VerticalAlignment.Bottom, versionText.VerticalAlignment);
        });
    }

    [Fact]
    public void TitleArea_DoesNotInterceptPointerInput()
    {
        WpfTestHost.RunInSta(() =>
        {
            var control = new HeaderBarControl();
            var titleArea = Assert.IsType<StackPanel>(control.FindName("TitleArea"));

            Assert.Equal(HorizontalAlignment.Left, titleArea.HorizontalAlignment);
            Assert.False(titleArea.IsHitTestVisible);
        });
    }

    private static void InvokePrivateClick(HeaderBarControl control, string methodName)
    {
        var method = typeof(HeaderBarControl).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(control, [new Button(), new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)]);
    }

}
