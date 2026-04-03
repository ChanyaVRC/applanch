using System.Windows;
using applanch.Infrastructure.Storage;
using Xunit;

namespace applanch.Tests.Application;

public class MainWindowUpdatePresentationTests
{
    [Fact]
    public void ResolveUpdatePresentation_Manual_ShowsAllUpdateActions()
    {
        var presentation = MainWindow.ResolveUpdatePresentation(UpdateInstallBehavior.Manual);

        Assert.Equal(Visibility.Visible, presentation.BannerVisibility);
        Assert.Equal(Visibility.Visible, presentation.HeaderButtonVisibility);
        Assert.Equal(Visibility.Visible, presentation.ActionButtonVisibility);
    }

    [Fact]
    public void ResolveUpdatePresentation_NotifyOnly_HidesActionButtons()
    {
        var presentation = MainWindow.ResolveUpdatePresentation(UpdateInstallBehavior.NotifyOnly);

        Assert.Equal(Visibility.Visible, presentation.BannerVisibility);
        Assert.Equal(Visibility.Collapsed, presentation.HeaderButtonVisibility);
        Assert.Equal(Visibility.Collapsed, presentation.ActionButtonVisibility);
    }

    [Fact]
    public void ResolveUpdatePresentation_AutomaticallyApply_HidesAllUpdateUi()
    {
        var presentation = MainWindow.ResolveUpdatePresentation(UpdateInstallBehavior.AutomaticallyApply);

        Assert.Equal(Visibility.Collapsed, presentation.BannerVisibility);
        Assert.Equal(Visibility.Collapsed, presentation.HeaderButtonVisibility);
        Assert.Equal(Visibility.Collapsed, presentation.ActionButtonVisibility);
    }
}
