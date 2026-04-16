using applanch.Updates;
using Xunit;

namespace applanch.Tests.Infrastructure.Updates;

public class SemanticVersionTests
{
    [Theory]
    [InlineData("2.11.3", 2, 11, 3, "")]
    [InlineData("1.0.0-beta.1", 1, 0, 0, "beta.1")]
    [InlineData("1.0.0-alpha-2", 1, 0, 0, "alpha-2")]
    public void Parse_ValidVersion_ReturnsExpectedComponents(
        string input,
        int expectedMajor,
        int expectedMinor,
        int expectedPatch,
        string expectedPrerelease)
    {
        var version = SemanticVersion.Parse(input);

        Assert.Equal(expectedMajor, version.Major);
        Assert.Equal(expectedMinor, version.Minor);
        Assert.Equal(expectedPatch, version.Patch);
        Assert.Equal(expectedPrerelease, version.Prerelease);
    }

    [Theory]
    [InlineData("1.2.3.4")]
    [InlineData("1.2.3-")]
    [InlineData("1.2")]
    [InlineData("1.a.3")]
    [InlineData("")]
    public void Parse_InvalidFormat_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => SemanticVersion.Parse(input));
    }

    [Fact]
    public void TryParse_InvalidInput_ReturnsFalse()
    {
        Assert.False(SemanticVersion.TryParse("1.2", out _));
    }

    [Theory]
    [InlineData("1.0.0", false)]
    [InlineData("1.0.0-rc.1", true)]
    public void IsPrerelease_ReturnsExpectedValue(string input, bool expected)
    {
        Assert.Equal(expected, SemanticVersion.Parse(input).IsPrerelease);
    }

    [Theory]
    [InlineData("2.0.0", "1.9.9", 1)]
    [InlineData("1.1.0", "1.0.9", 1)]
    [InlineData("1.0.1", "1.0.0", 1)]
    [InlineData("1.0.0", "1.0.0-beta", 1)]
    [InlineData("1.0.0-alpha", "1.0.0-beta", -1)]
    [InlineData("1.0.0-1", "1.0.0-alpha", -1)]
    [InlineData("1.0.0-alpha", "1.0.0-alpha.1", -1)]
    public void CompareTo_ReturnsExpectedOrdering(string left, string right, int expectedSign)
    {
        var lhs = SemanticVersion.Parse(left);
        var rhs = SemanticVersion.Parse(right);

        Assert.Equal(expectedSign, Math.Sign(lhs.CompareTo(rhs)));
        Assert.Equal(-expectedSign, Math.Sign(rhs.CompareTo(lhs)));
    }

    [Fact]
    public void CompareTo_EqualStableVersions_ReturnsZero()
    {
        Assert.Equal(0, SemanticVersion.Parse("1.2.3").CompareTo(SemanticVersion.Parse("1.2.3")));
    }

}
