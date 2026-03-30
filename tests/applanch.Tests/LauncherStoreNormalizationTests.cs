using System.Reflection;
using Xunit;
using applanch.Infrastructure.Storage;

namespace applanch.Tests;

public class LauncherStoreNormalizationTests
{
    [Fact]
    public void NormalizePath_NullOrWhitespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, InvokeNormalizePath(null));
        Assert.Equal(string.Empty, InvokeNormalizePath("   "));
    }

    [Fact]
    public void NormalizePath_TrimsTrailingSeparator_ForNonRootPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "applanch-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var withTrailing = tempDir + Path.DirectorySeparatorChar;

            var normalized = InvokeNormalizePath(withTrailing);

            Assert.Equal(tempDir, normalized);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void NormalizePath_InvalidPath_ReturnsTrimmedOriginal()
    {
        var invalid = "  invalid\0path  ";

        var normalized = InvokeNormalizePath(invalid);

        Assert.Equal("invalid\0path", normalized);
    }

    [Fact]
    public void NormalizePath_ReservedDeviceLikePath_ReturnsTrimmedOriginal()
    {
        var reserved = "  CON:  ";

        var normalized = InvokeNormalizePath(reserved);

        Assert.Equal(@"\\.\CON", normalized);
    }

    [Fact]
    public void NormalizeEntries_RemovesDuplicates_AndNormalizesFields()
    {
        var entries = new[]
        {
            new LauncherStore.LauncherEntry("  C:\\Apps\\Tool.exe  ", "  Dev  ", "  -a  ", "  Tool Name  "),
            new LauncherStore.LauncherEntry("c:\\apps\\tool.exe", "", "", ""),
            new LauncherStore.LauncherEntry("", "Ops", "-x", "X")
        };

        var normalized = InvokeNormalizeEntries(entries).ToList();

        Assert.Single(normalized);
        Assert.Equal(@"C:\Apps\Tool.exe", normalized[0].Path);
        Assert.Equal("Dev", normalized[0].Category);
        Assert.Equal("-a", normalized[0].Arguments);
        Assert.Equal("Tool Name", normalized[0].DisplayName);
    }

    private static string InvokeNormalizePath(string? value)
    {
        var method = typeof(LauncherStore).GetMethod("NormalizePath", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method!.Invoke(null, new object?[] { value })!;
    }

    private static IReadOnlyList<LauncherStore.LauncherEntry> InvokeNormalizeEntries(IEnumerable<LauncherStore.LauncherEntry> value)
    {
        var method = typeof(LauncherStore).GetMethod("NormalizeEntries", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (IReadOnlyList<LauncherStore.LauncherEntry>)method!.Invoke(null, new object?[] { value })!;
    }
}
