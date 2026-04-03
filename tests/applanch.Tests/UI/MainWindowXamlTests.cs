using Xunit;

namespace applanch.Tests.UI;

public class MainWindowXamlTests
{
    [Fact]
    public void FloatingNotificationBanner_UsesCrispTextRenderingSettings()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var xamlPath = Path.Combine(projectRoot, "src", "applanch", "MainWindow.xaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("controls:FloatingNotificationControl", xaml);
        Assert.Contains("x:Name=\"FloatingNotification\"", xaml);
        Assert.Contains("ActionRequested=\"FloatingNotification_ActionRequested\"", xaml);
        Assert.Contains("Hidden=\"FloatingNotification_Hidden\"", xaml);
    }

    [Fact]
    public void FloatingNotificationControl_UsesCrispTextRenderingSettings()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var xamlPath = Path.Combine(projectRoot, "src", "applanch", "Controls", "FloatingNotificationControl.xaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("UseLayoutRounding=\"True\"", xaml);
        Assert.Contains("SnapsToDevicePixels=\"True\"", xaml);
        Assert.Contains("TextOptions.TextFormattingMode=\"Display\"", xaml);
        Assert.Contains("TextOptions.TextRenderingMode=\"Auto\"", xaml);
        Assert.Contains("Visibility=\"{Binding ActionVisibility, ElementName=Root}\"", xaml);
    }

    [Fact]
    public void LaunchItemTemplate_ShowsMissingPathWarningBadge()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var xamlPath = Path.Combine(projectRoot, "src", "applanch", "MainWindow.xaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("ToolTip_MissingPath", xaml);
        Assert.Contains("Binding=\"{Binding IsPathMissing}\"", xaml);
        Assert.Contains("&#xE7BA;", xaml);
    }

    [Fact]
    public void LaunchItemTemplate_ContextMenu_IncludesOpenFileLocationAction()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var xamlPath = Path.Combine(projectRoot, "src", "applanch", "MainWindow.xaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("Menu_OpenFileLocation", xaml);
        Assert.Contains("Tag=\"{x:Static local:LaunchItemContextMenuAction.OpenLocation}\"", xaml);
    }
}
