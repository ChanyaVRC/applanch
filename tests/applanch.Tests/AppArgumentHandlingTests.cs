using System.Reflection;
using Xunit;

namespace applanch.Tests;

public class AppArgumentHandlingTests
{
    [Fact]
    public void TryHandleRegisterArgument_WithNoArgs_ReturnsFalse()
    {
        var result = InvokeTryHandleRegisterArgument([]);

        Assert.False(result);
    }

    [Fact]
    public void TryHandleRegisterArgument_WithDifferentOption_ReturnsFalse()
    {
        var result = InvokeTryHandleRegisterArgument(["--other", "C:\\temp\\tool.exe"]);

        Assert.False(result);
    }

    [Fact]
    public void TryHandleRegisterArgument_WithRegisterOptionAndMissingPath_ReturnsTrue()
    {
        var result = InvokeTryHandleRegisterArgument([App.RegisterArgument, "C:\\path\\that\\does\\not\\exist.exe"]);

        Assert.True(result);
    }

    private static bool InvokeTryHandleRegisterArgument(string[] args)
    {
        var method = typeof(App).GetMethod("TryHandleRegisterArgument", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (bool)method!.Invoke(null, [args])!;
    }
}
