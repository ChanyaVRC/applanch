using System.Reflection;
using System.Globalization;
using Xunit;
using applanch.Infrastructure.Storage;
using applanch.Properties;

namespace applanch.Tests.Infrastructure.Storage;

public class LauncherStoreNormalizationTests
{
    [Fact]
    public void NormalizePath_NullOrWhitespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, InvokeNormalizePath(null));
        Assert.Equal(string.Empty, InvokeNormalizePath("   "));
    }

    [Fact]
    public void NormalizePath_PreservesTrailingSeparator_ForDirectoryPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "applanch-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var withTrailing = tempDir + Path.DirectorySeparatorChar;

            var normalized = InvokeNormalizePath(withTrailing);

            Assert.Equal(withTrailing, normalized);
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
    public void NormalizeEntries_DriveLetterSpecifier_PromotesToDriveRoot()
    {
        var driveRoot = Path.GetPathRoot(Path.GetTempPath());
        Assert.False(string.IsNullOrWhiteSpace(driveRoot));
        var driveSpecifier = driveRoot!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var entries = new[]
        {
            new LauncherStore.LauncherEntry(driveSpecifier, "Misc", string.Empty, "Drive")
        };

        var normalized = InvokeNormalizeEntries(entries).ToList();

        var item = Assert.Single(normalized);
        Assert.Equal(driveRoot, item.Path);
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

    [Fact]
    public void NormalizeEntries_DropsJapaneseEmptyMessagePlaceholderPath()
    {
        var previousUi = CultureInfo.CurrentUICulture;
        var previousCulture = CultureInfo.CurrentCulture;

        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ja");

        var placeholderPath = Resources.EmptyMessage;
        Assert.False(string.IsNullOrWhiteSpace(placeholderPath));

        try
        {
            var entries = new[]
            {
                new LauncherStore.LauncherEntry(placeholderPath, "Misc", string.Empty, "Placeholder"),
                new LauncherStore.LauncherEntry(@"C:\Apps\Tool.exe", "Misc", string.Empty, "Tool"),
            };

            var normalized = InvokeNormalizeEntries(entries).ToList();

            var item = Assert.Single(normalized);
            Assert.Equal(Path.GetFullPath(@"C:\Apps\Tool.exe"), item.Path);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUi;
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.DefaultThreadCurrentUICulture = previousUi;
            CultureInfo.DefaultThreadCurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void NormalizeEntries_DropsEnglishEmptyMessagePlaceholderPath()
    {
        var previousUi = CultureInfo.CurrentUICulture;
        var previousCulture = CultureInfo.CurrentCulture;

        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en");

        var placeholderPath = Resources.EmptyMessage;
        Assert.False(string.IsNullOrWhiteSpace(placeholderPath));

        try
        {
            var entries = new[]
            {
                new LauncherStore.LauncherEntry(placeholderPath, "Misc", string.Empty, "Placeholder"),
                new LauncherStore.LauncherEntry(@"C:\Apps\Tool.exe", "Misc", string.Empty, "Tool"),
            };

            var normalized = InvokeNormalizeEntries(entries).ToList();

            var item = Assert.Single(normalized);
            Assert.Equal(Path.GetFullPath(@"C:\Apps\Tool.exe"), item.Path);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUi;
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.DefaultThreadCurrentUICulture = previousUi;
            CultureInfo.DefaultThreadCurrentCulture = previousCulture;
        }
    }

    private static string InvokeNormalizePath(string? value)
    {
        var method = typeof(LauncherStore).GetMethod("NormalizePath", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method!.Invoke(null, [value])!;
    }

    private static IReadOnlyList<LauncherStore.LauncherEntry> InvokeNormalizeEntries(IEnumerable<LauncherStore.LauncherEntry> value)
    {
        var method = typeof(LauncherStore).GetMethod("NormalizeEntries", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (IReadOnlyList<LauncherStore.LauncherEntry>)method!.Invoke(null, [value])!;
    }
}

