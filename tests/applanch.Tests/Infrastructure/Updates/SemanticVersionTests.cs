using applanch.Infrastructure.Updates;
using Xunit;

namespace applanch.Tests.Infrastructure.Updates;

public class SemanticVersionTests
{
    [Fact]
    public void TryParse_StandardVersion_ReturnsComponents()
    {
        var result = SemanticVersion.TryParse("2.11.3", out var version);

        Assert.True(result);
        Assert.Equal(2, version.Major);
        Assert.Equal(11, version.Minor);
        Assert.Equal(3, version.Patch);
        Assert.Equal(string.Empty, version.Prerelease);
    }

    [Fact]
    public void TryParse_PrereleaseVersion_CapturesPrerelease()
    {
        var result = SemanticVersion.TryParse("1.0.0-beta.1", out var version);

        Assert.True(result);
        Assert.Equal(1, version.Major);
        Assert.Equal(0, version.Minor);
        Assert.Equal(0, version.Patch);
        Assert.Equal("beta.1", version.Prerelease);
    }

    [Fact]
    public void TryParse_PrereleaseContainingDash_CapturesFullString()
    {
        var result = SemanticVersion.TryParse("1.0.0-alpha-2", out var version);

        Assert.True(result);
        Assert.Equal("alpha-2", version.Prerelease);
    }

    [Fact]
    public void TryParse_ExtraNumericSegments_PreservesCurrentBehavior()
    {
        var result = SemanticVersion.TryParse("1.2.3.4", out var version);

        Assert.True(result);
        Assert.Equal(1, version.Major);
        Assert.Equal(2, version.Minor);
        Assert.Equal(3, version.Patch);
    }

    [Fact]
    public void TryParse_TrailingDash_ParsesAsStable()
    {
        var result = SemanticVersion.TryParse("1.2.3-", out var version);

        Assert.True(result);
        Assert.Equal(string.Empty, version.Prerelease);
    }

    [Fact]
    public void TryParse_TooFewNumericSegments_ReturnsFalse()
    {
        Assert.False(SemanticVersion.TryParse("1.2", out _));
    }

    [Fact]
    public void TryParse_NonIntegerSegment_ReturnsFalse()
    {
        Assert.False(SemanticVersion.TryParse("1.a.3", out _));
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        Assert.False(SemanticVersion.TryParse(string.Empty, out _));
    }

    [Fact]
    public void IsPrerelease_FalseForStableVersion()
    {
        SemanticVersion.TryParse("1.0.0", out var version);

        Assert.False(version.IsPrerelease);
    }

    [Fact]
    public void IsPrerelease_TrueForPrereleaseVersion()
    {
        SemanticVersion.TryParse("1.0.0-rc.1", out var version);

        Assert.True(version.IsPrerelease);
    }

    [Theory]
    [InlineData("2.0.0", "1.9.9")]
    [InlineData("1.1.0", "1.0.9")]
    [InlineData("1.0.1", "1.0.0")]
    public void CompareTo_HigherVersionIsGreater(string higher, string lower)
    {
        SemanticVersion.TryParse(higher, out var hi);
        SemanticVersion.TryParse(lower, out var lo);

        Assert.True(hi.CompareTo(lo) > 0);
        Assert.True(lo.CompareTo(hi) < 0);
    }

    [Fact]
    public void CompareTo_EqualStableVersions_ReturnsZero()
    {
        SemanticVersion.TryParse("1.2.3", out var a);
        SemanticVersion.TryParse("1.2.3", out var b);

        Assert.Equal(0, a.CompareTo(b));
    }

    [Fact]
    public void CompareTo_StableIsGreaterThanPrerelease_ForSameNumbers()
    {
        SemanticVersion.TryParse("1.0.0", out var stable);
        SemanticVersion.TryParse("1.0.0-beta", out var prerelease);

        Assert.True(stable.CompareTo(prerelease) > 0);
        Assert.True(prerelease.CompareTo(stable) < 0);
    }

    [Fact]
    public void CompareTo_TwoPrereleasesWithSameNumbers_ReturnsZero()
    {
        SemanticVersion.TryParse("1.0.0-alpha", out var a);
        SemanticVersion.TryParse("1.0.0-beta", out var b);

        // Only numeric parts + prerelease flag are compared; prerelease tag text is not
        Assert.Equal(0, a.CompareTo(b));
    }
}
