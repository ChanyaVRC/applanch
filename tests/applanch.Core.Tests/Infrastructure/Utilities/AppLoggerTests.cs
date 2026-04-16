using applanch.Core.Infrastructure.Utilities;
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

    [Fact]
    public void Warn_WithException_LogsExceptionDetails()
    {
        var marker = $"warn-test-{Guid.NewGuid():N}";
        var ex = new InvalidOperationException("outer", new Exception("inner"));

        AppLogger.Instance.Warn(ex, marker);

        using var stream = new FileStream(
            AppLogger.LogFilePathValue,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        var log = reader.ReadToEnd();
        Assert.Contains(marker, log, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException: outer", log, StringComparison.Ordinal);
        Assert.Contains("Inner: Exception: inner", log, StringComparison.Ordinal);
    }
}
