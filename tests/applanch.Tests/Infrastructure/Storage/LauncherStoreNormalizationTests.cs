using System.Reflection;
using System.Globalization;
using Xunit;
using applanch.Infrastructure.Storage;

namespace applanch.Tests.Infrastructure.Storage;

public class LauncherStoreNormalizationTests
{
    [Fact]
    public void TryNormalizePersistablePath_NullOrWhitespace_ReturnsFalse()
    {
        Assert.False(InvokeTryNormalizePersistablePath("", out var normalizedEmpty));
        Assert.Equal(string.Empty, normalizedEmpty);

        Assert.False(InvokeTryNormalizePersistablePath("   ", out var normalizedWhitespace));
        Assert.Equal(string.Empty, normalizedWhitespace);
    }

    [Fact]
    public void TryNormalizePersistablePath_PreservesTrailingSeparator_ForDirectoryPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "applanch-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var withTrailing = tempDir + Path.DirectorySeparatorChar;

            var ok = InvokeTryNormalizePersistablePath(withTrailing, out var normalized);

            Assert.True(ok);
            Assert.Equal(withTrailing, normalized);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryNormalizePersistablePath_InvalidPath_ReturnsFalse()
    {
        var invalid = "  invalid\0path  ";

        var ok = InvokeTryNormalizePersistablePath(invalid, out var normalized);

        Assert.False(ok);
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void NormalizeEntries_DriveLetterSpecifier_PromotesToDriveRoot()
    {
        var driveRoot = Path.GetPathRoot(Path.GetTempPath());
        Assert.False(string.IsNullOrWhiteSpace(driveRoot));
        var driveSpecifier = driveRoot!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var entries = new[]
        {
            new LauncherEntry(driveSpecifier, "Misc", string.Empty, "Drive")
        };

        var normalized = InvokeNormalizeEntries(entries).ToList();

        var item = Assert.Single(normalized);
        Assert.Equal(driveRoot, item.Path.Value);
    }

    [Fact]
    public void NormalizeEntries_RootedButNotFullyQualifiedPath_IsDropped()
    {
        var entries = new[]
        {
            new LauncherEntry("\\Temp\\Tool.exe", "Misc", string.Empty, "Tool")
        };

        var normalized = InvokeNormalizeEntries(entries);

        Assert.Empty(normalized);
    }

    [Fact]
    public void TryNormalizePersistablePath_ReservedDeviceLikePath_ReturnsFalse()
    {
        var reserved = "  CON:  ";

        var ok = InvokeTryNormalizePersistablePath(reserved, out var normalized);

        Assert.False(ok);
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void NormalizeEntries_RemovesDuplicates_AndNormalizesFields()
    {
        var entries = new[]
        {
            new LauncherEntry("  C:\\Apps\\Tool.exe  ", "  Dev  ", "  -a  ", "  Tool Name  "),
            new LauncherEntry("c:\\apps\\tool.exe", "", "", ""),
            new LauncherEntry("", "Ops", "-x", "X")
        };

        var normalized = InvokeNormalizeEntries(entries).ToList();

        Assert.Single(normalized);
        Assert.Equal(@"C:\Apps\Tool.exe", normalized[0].Path.Value);
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

        var placeholderPath = AppResources.EmptyMessage;
        Assert.False(string.IsNullOrWhiteSpace(placeholderPath));

        try
        {
            var entries = new[]
            {
                new LauncherEntry(placeholderPath, "Misc", string.Empty, "Placeholder"),
                new LauncherEntry(@"C:\Apps\Tool.exe", "Misc", string.Empty, "Tool"),
            };

            var normalized = InvokeNormalizeEntries(entries).ToList();

            var item = Assert.Single(normalized);
            Assert.Equal(Path.GetFullPath(@"C:\Apps\Tool.exe"), item.Path.Value);
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

        var placeholderPath = AppResources.EmptyMessage;
        Assert.False(string.IsNullOrWhiteSpace(placeholderPath));

        try
        {
            var entries = new[]
            {
                new LauncherEntry(placeholderPath, "Misc", string.Empty, "Placeholder"),
                new LauncherEntry(@"C:\Apps\Tool.exe", "Misc", string.Empty, "Tool"),
            };

            var normalized = InvokeNormalizeEntries(entries).ToList();

            var item = Assert.Single(normalized);
            Assert.Equal(Path.GetFullPath(@"C:\Apps\Tool.exe"), item.Path.Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUi;
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.DefaultThreadCurrentUICulture = previousUi;
            CultureInfo.DefaultThreadCurrentCulture = previousCulture;
        }
    }

    private static bool InvokeTryNormalizePersistablePath(string value, out string normalized)
    {
        var method = typeof(LauncherStore).GetMethod("TryNormalizePersistablePath", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        object?[] args = [value, null];
        var success = (bool)method!.Invoke(null, args)!;
        normalized = args[1] as string ?? string.Empty;
        return success;
    }

    private static IReadOnlyList<LauncherEntry> InvokeNormalizeEntries(IEnumerable<LauncherEntry> value)
    {
        var method = typeof(LauncherStore).GetMethod("NormalizeEntries", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (IReadOnlyList<LauncherEntry>)method!.Invoke(null, [value])!;
    }
}

