using System.Windows;
using Xunit;

namespace applanch.Tests;

public class LaunchExecutionResultTests
{
    [Fact]
    public void Success_ReturnsExpectedValues()
    {
        var result = LaunchExecutionResult.Success();

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Message);
        Assert.Equal(MessageBoxImage.None, result.Icon);
    }

    [Fact]
    public void Failed_ReturnsExpectedValues()
    {
        var result = LaunchExecutionResult.Failed("failed", MessageBoxImage.Error);

        Assert.False(result.IsSuccess);
        Assert.Equal("failed", result.Message);
        Assert.Equal(MessageBoxImage.Error, result.Icon);
    }
}