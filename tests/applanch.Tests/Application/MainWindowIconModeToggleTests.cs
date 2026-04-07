using applanch.Infrastructure.Storage;
using Xunit;

namespace applanch.Tests.Application;

public class MainWindowIconModeToggleTests
{
    [Fact]
    public void ToggleLaunchItemIconOnlyMode_WhenDisabled_EnablesIt()
    {
        var settings = new AppSettings { LaunchItemIconOnlyMode = false };

        var updated = MainWindow.ToggleLaunchItemIconOnlyMode(settings);

        Assert.True(updated.LaunchItemIconOnlyMode);
    }

    [Fact]
    public void ToggleLaunchItemIconOnlyMode_WhenEnabled_DisablesIt()
    {
        var settings = new AppSettings { LaunchItemIconOnlyMode = true };

        var updated = MainWindow.ToggleLaunchItemIconOnlyMode(settings);

        Assert.False(updated.LaunchItemIconOnlyMode);
    }

    [Fact]
    public void SetCategorySidebarPinned_WhenFalse_UpdatesSettings()
    {
        var settings = new AppSettings { CategorySidebarPinned = true };

        var updated = MainWindow.SetCategorySidebarPinned(settings, isPinned: false);

        Assert.False(updated.CategorySidebarPinned);
    }

    [Fact]
    public void SetCategorySidebarPinned_WhenTrue_UpdatesSettings()
    {
        var settings = new AppSettings { CategorySidebarPinned = false };

        var updated = MainWindow.SetCategorySidebarPinned(settings, isPinned: true);

        Assert.True(updated.CategorySidebarPinned);
    }
}