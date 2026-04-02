using System.Windows;
using applanch.Infrastructure.Utilities;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.Infrastructure.Utilities;

public class FloatingNotificationCoordinatorTests
{
    [Fact]
    public void BeginShow_AllowsSubsequentHideCompletion()
    {
        var coordinator = new FloatingNotificationCoordinator();

        coordinator.BeginShow();
        var shouldAnimateHide = coordinator.BeginHide(isBannerVisible: true);

        Assert.True(shouldAnimateHide);
        Assert.True(coordinator.CanCompleteHide());
    }

    [Fact]
    public void BeginHide_WhenBannerInvisible_ReturnsFalse()
    {
        var coordinator = new FloatingNotificationCoordinator();

        var shouldAnimateHide = coordinator.BeginHide(isBannerVisible: false);

        Assert.False(shouldAnimateHide);
        Assert.True(coordinator.CanCompleteHide());
    }

    [Fact]
    public void BeginHide_WhenBannerVisible_ReturnsTrue()
    {
        var coordinator = new FloatingNotificationCoordinator();
        var shouldAnimateHide = coordinator.BeginHide(isBannerVisible: true);

        Assert.True(shouldAnimateHide);
        Assert.True(coordinator.CanCompleteHide());
    }

    [Fact]
    public void CanCompleteHide_ReturnsFalse_WhenShowStartsAfterHide()
    {
        var coordinator = new FloatingNotificationCoordinator();

        coordinator.BeginHide(isBannerVisible: true);
        coordinator.BeginShow();

        Assert.False(coordinator.CanCompleteHide());
    }

    [Theory]
    [InlineData(MessageBoxImage.Error, NotificationIconType.Error)]
    [InlineData(MessageBoxImage.Warning, NotificationIconType.Warning)]
    [InlineData(MessageBoxImage.Information, NotificationIconType.Info)]
    [InlineData(MessageBoxImage.None, NotificationIconType.None)]
    public void MapIcon_ReturnsExpected(MessageBoxImage icon, NotificationIconType expected)
    {
        var result = FloatingNotificationCoordinator.MapIcon(icon);

        Assert.Equal(expected, result);
    }
}
