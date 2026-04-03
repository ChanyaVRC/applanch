using applanch.Infrastructure.Utilities;
using Xunit;

namespace applanch.Tests.Infrastructure.Utilities;

public class AppLoggerTests
{
    [Fact]
    public void IsLikelyTestProcess_TesthostName_ReturnsTrue()
    {
        Assert.True(AppLogger.IsLikelyTestProcess("testhost"));
        Assert.True(AppLogger.IsLikelyTestProcess("testhost.net"));
    }

    [Fact]
    public void IsLikelyTestProcess_RegularName_ReturnsFalse()
    {
        Assert.False(AppLogger.IsLikelyTestProcess("applanch"));
    }

    [Fact]
    public void ResolveLogDirectory_TestProcess_UsesTempTestLogDirectory()
    {
        var directory = AppLogger.ResolveLogDirectory(overrideDirectory: null, processName: "testhost");

        Assert.Equal(Path.Combine(Path.GetTempPath(), "applanch-test-logs"), directory);
    }

    [Fact]
    public void ResolveLogDirectory_WithOverride_UsesOverrideDirectory()
    {
        var directory = AppLogger.ResolveLogDirectory(overrideDirectory: @"C:\override-logs", processName: "testhost");

        Assert.Equal(@"C:\override-logs", directory);
    }
}
