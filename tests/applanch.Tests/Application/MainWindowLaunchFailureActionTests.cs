using System.Windows;
using applanch.Infrastructure.Launch;
using Xunit;

namespace applanch.Tests.Application;

public class MainWindowLaunchFailureActionTests
{
    [Fact]
    public void ShouldOfferDeleteActionForLaunchFailure_MissingTargetFailure_ReturnsTrue()
    {
        var execution = LaunchExecutionResult.Failed("missing", MessageBoxImage.Warning, LaunchFailureKind.MissingTarget);

        var shouldOfferDelete = MainWindow.ShouldOfferDeleteActionForLaunchFailure(execution);

        Assert.True(shouldOfferDelete);
    }

    [Fact]
    public void ShouldOfferDeleteActionForLaunchFailure_OtherFailure_ReturnsFalse()
    {
        var execution = LaunchExecutionResult.Failed("launch failed", MessageBoxImage.Error, LaunchFailureKind.Other);

        var shouldOfferDelete = MainWindow.ShouldOfferDeleteActionForLaunchFailure(execution);

        Assert.False(shouldOfferDelete);
    }

    [Fact]
    public void ShouldOfferDeleteActionForLaunchFailure_Success_ReturnsFalse()
    {
        var shouldOfferDelete = MainWindow.ShouldOfferDeleteActionForLaunchFailure(LaunchExecutionResult.Success());

        Assert.False(shouldOfferDelete);
    }
}
