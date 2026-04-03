using applanch.Infrastructure.Updates;
using Xunit;

namespace applanch.Tests.Infrastructure.Updates;

public class SemanticVersionTests
{
    [Fact]
    public void Parse_StandardVersion_ReturnsComponents()
    {
        var version = SemanticVersion.Parse("2.11.3");

        Assert.Equal(2, version.Major);
        Assert.Equal(11, version.Minor);
        Assert.Equal(3, version.Patch);
        Assert.Equal(string.Empty, version.Prerelease);
    }

    [Fact]
    public void Parse_PrereleaseVersion_CapturesPrerelease()
    {
        var version = SemanticVersion.Parse("1.0.0-beta.1");

        Assert.Equal(1, version.Major);
        Assert.Equal(0, version.Minor);
        Assert.Equal(0, version.Patch);
        Assert.Equal("beta.1", version.Prerelease);
    }

    [Fact]
    public void Parse_PrereleaseContainingDash_CapturesFullString()
    {
        var version = SemanticVersion.Parse("1.0.0-alpha-2");

        Assert.Equal("alpha-2", version.Prerelease);
    }

    [Fact]
    public void Parse_ExtraNumericSegments_PreservesCurrentBehavior()
    {
        var version = SemanticVersion.Parse("1.2.3.4");

        Assert.Equal(1, version.Major);
        Assert.Equal(2, version.Minor);
        Assert.Equal(3, version.Patch);
    }

    [Fact]
    public void Parse_TrailingDash_ParsesAsStable()
    {
        var version = SemanticVersion.Parse("1.2.3-");

        Assert.Equal(string.Empty, version.Prerelease);
    }

    [Fact]
    public void Parse_TooFewNumericSegments_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => SemanticVersion.Parse("1.2"));
    }

    [Fact]
    public void Parse_NonIntegerSegment_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => SemanticVersion.Parse("1.a.3"));
    }

    [Fact]
    public void Parse_EmptyString_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => SemanticVersion.Parse(string.Empty));
    }

    [Fact]
    public void TryParse_InvalidInput_ReturnsFalse()
    {
        Assert.False(SemanticVersion.TryParse("1.2", out _));
    }

    [Fact]
    public void IsPrerelease_FalseForStableVersion()
    {
        Assert.False(SemanticVersion.Parse("1.0.0").IsPrerelease);
    }

    [Fact]
    public void IsPrerelease_TrueForPrereleaseVersion()
    {
        Assert.True(SemanticVersion.Parse("1.0.0-rc.1").IsPrerelease);
    }

    [Theory]
    [InlineData("2.0.0", "1.9.9")]
    [InlineData("1.1.0", "1.0.9")]
    [InlineData("1.0.1", "1.0.0")]
    public void CompareTo_HigherVersionIsGreater(string higher, string lower)
    {
        var hi = SemanticVersion.Parse(higher);
        var lo = SemanticVersion.Parse(lower);

        Assert.True(hi.CompareTo(lo) > 0);
        Assert.True(lo.CompareTo(hi) < 0);
    }

    [Fact]
    public void CompareTo_EqualStableVersions_ReturnsZero()
    {
        Assert.Equal(0, SemanticVersion.Parse("1.2.3").CompareTo(SemanticVersion.Parse("1.2.3")));
    }

    [Fact]
    public void CompareTo_StableIsGreaterThanPrerelease_ForSameNumbers()
    {
        var stable = SemanticVersion.Parse("1.0.0");
        var prerelease = SemanticVersion.Parse("1.0.0-beta");

        Assert.True(stable.CompareTo(prerelease) > 0);
        Assert.True(prerelease.CompareTo(stable) < 0);
    }

    [Fact]
    public void CompareTo_TwoPrereleasesWithSameNumbers_ReturnsZero()
    {
        var a = SemanticVersion.Parse("1.0.0-alpha");
        var b = SemanticVersion.Parse("1.0.0-beta");

        // Only numeric parts + prerelease flag are compared; prerelease tag text is not
        Assert.Equal(0, a.CompareTo(b));
    }
}
