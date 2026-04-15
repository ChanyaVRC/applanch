using applanch.Infrastructure.Launch;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.Infrastructure.Launch;

public class LaunchExecutionResultTests
{
    [Fact]
    public void Success_ReturnsExpectedValues()
    {
        var result = LaunchExecutionResult.Success();

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Message);
        Assert.Equal(NotificationIconType.None, result.Icon);
    }

    [Fact]
    public void Failed_ReturnsExpectedValues()
    {
        var result = LaunchExecutionResult.Failed("failed", NotificationIconType.Error);

        Assert.False(result.IsSuccess);
        Assert.Equal("failed", result.Message);
        Assert.Equal(NotificationIconType.Error, result.Icon);
    }
}

