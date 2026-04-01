using applanch.Infrastructure.Utilities;
using Xunit;

namespace applanch.Tests.Infrastructure.Utilities;

public class PathNormalizationTests
{
    [Fact]
    public void NormalizeForComparison_TrimsAndRemovesTrailingDirectorySeparator()
    {
        var tempDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var input = $"  {tempDir}{Path.DirectorySeparatorChar}  ";

        var normalized = PathNormalization.NormalizeForComparison(input);

        Assert.Equal(tempDir, normalized);
    }

    [Fact]
    public void NormalizeDirectoryPath_DriveSpecifier_AddsTrailingSeparator()
    {
        var driveRoot = Path.GetPathRoot(Path.GetTempPath());
        Assert.False(string.IsNullOrWhiteSpace(driveRoot));
        var driveSpecifier = driveRoot!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var normalized = PathNormalization.NormalizeDirectoryPath(driveSpecifier);

        Assert.Equal(driveRoot, normalized);
    }

    [Fact]
    public void TryNormalizePersistablePath_RelativePath_ReturnsFalse()
    {
        var success = PathNormalization.TryNormalizePersistablePath(@"tools\app.exe", out var normalized);

        Assert.False(success);
        Assert.Equal(string.Empty, normalized);
    }
}
