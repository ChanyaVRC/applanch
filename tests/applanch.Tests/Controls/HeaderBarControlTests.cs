using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using applanch.Controls;
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
    public void IsUpdateButtonEnabledProperty_CanSetAndGet()
    {
        WpfTestHost.RunInSta(() =>
        {
            var control = new HeaderBarControl();

            control.IsUpdateButtonEnabled = false;

            Assert.False(control.IsUpdateButtonEnabled);
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
    public void IsUpdateButtonEnabledProperty_UpdatesUpdateButtonEnabledState()
    {
        WpfTestHost.RunInSta(() =>
        {
            var control = new HeaderBarControl();
            var updateButton = Assert.IsType<Button>(control.FindName("UpdateButton"));

            Assert.True(updateButton.IsEnabled);

            control.IsUpdateButtonEnabled = false;
            WpfTestHost.DoEvents();

            Assert.False(updateButton.IsEnabled);
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
    public void AppVersionTextProperty_ShowsPrereleaseBadge_ForPrereleaseVersion()
    {
        WpfTestHost.RunInSta(() =>
        {
            var control = new HeaderBarControl();
            var prereleaseBadge = Assert.IsType<Border>(control.FindName("PrereleaseBadge"));

            control.AppVersionText = "v2.0.0-beta.1";
            WpfTestHost.DoEvents();

            Assert.True(control.IsPrereleaseVersion);
            Assert.Equal(Visibility.Visible, prereleaseBadge.Visibility);
        });
    }

    [Fact]
    public void AppVersionTextProperty_HidesPrereleaseBadge_ForStableVersion()
    {
        WpfTestHost.RunInSta(() =>
        {
            var control = new HeaderBarControl();
            var prereleaseBadge = Assert.IsType<Border>(control.FindName("PrereleaseBadge"));

            control.AppVersionText = "v2.0.0-beta.1";
            WpfTestHost.DoEvents();
            control.AppVersionText = "v2.0.0";
            WpfTestHost.DoEvents();

            Assert.False(control.IsPrereleaseVersion);
            Assert.Equal(Visibility.Collapsed, prereleaseBadge.Visibility);
        });
    }

    [Fact]
    public void PrereleaseBadge_UsesFixedLabel()
    {
        WpfTestHost.RunInSta(() =>
        {
            var control = new HeaderBarControl();
            var prereleaseBadge = Assert.IsType<Border>(control.FindName("PrereleaseBadge"));
            var badgeText = Assert.IsType<TextBlock>(prereleaseBadge.Child);

            Assert.Equal("PRERELEASE", badgeText.Text);
        });
    }

    [Fact]
    public void PrereleaseBadge_HasSquareCorners()
    {
        WpfTestHost.RunInSta(() =>
        {
            var control = new HeaderBarControl();
            var prereleaseBadge = Assert.IsType<Border>(control.FindName("PrereleaseBadge"));

            Assert.Equal(new CornerRadius(0), prereleaseBadge.CornerRadius);
        });
    }

    private static void InvokePrivateClick(HeaderBarControl control, string methodName)
    {
        var method = typeof(HeaderBarControl).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(control, [new Button(), new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)]);
    }

}
