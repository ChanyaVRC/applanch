using System.Windows.Media;
using applanch.Infrastructure.Theming;
using Xunit;

namespace applanch.Tests.Infrastructure.Theming;

public class ThemeColorTests
{
    [Theory]
    [InlineData("#FF0000", 255, 255, 0, 0)]
    [InlineData("#00FF00", 255, 0, 255, 0)]
    [InlineData("#0000FF", 255, 0, 0, 255)]
    [InlineData("#AABBCC", 255, 0xAA, 0xBB, 0xCC)]
    [InlineData("FF0000", 255, 255, 0, 0)]
    public void TryParse_ValidRgbHex_ReturnsExpectedComponents(string input, byte a, byte r, byte g, byte b)
    {
        Assert.True(ThemeColor.TryParse(input, out var color));
        Assert.Equal(a, color.A);
        Assert.Equal(r, color.R);
        Assert.Equal(g, color.G);
        Assert.Equal(b, color.B);
        Assert.Equal((uint)((a << 24) | (r << 16) | (g << 8) | b), color.Argb);
    }

    [Theory]
    [InlineData("#80FF0000", 0x80, 0xFF, 0x00, 0x00)]
    [InlineData("#01020304", 0x01, 0x02, 0x03, 0x04)]
    [InlineData("FF112233", 0xFF, 0x11, 0x22, 0x33)]
    public void TryParse_ValidArgbHex_ReturnsExpectedComponents(string input, byte a, byte r, byte g, byte b)
    {
        Assert.True(ThemeColor.TryParse(input, out var color));
        Assert.Equal(a, color.A);
        Assert.Equal(r, color.R);
        Assert.Equal(g, color.G);
        Assert.Equal(b, color.B);
        Assert.Equal((uint)((a << 24) | (r << 16) | (g << 8) | b), color.Argb);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("#FFF")]
    [InlineData("#FFFFF")]
    [InlineData("#FFFFFFFFF")]
    [InlineData("ZZZZZZ")]
    [InlineData("not-a-color")]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(ThemeColor.TryParse(input, out _));
    }

    [Fact]
    public void Parse_InvalidInput_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => ThemeColor.Parse("invalid"));
    }

    [Fact]
    public void Hex_OpaqueColor_OmitsAlpha()
    {
        var color = ThemeColor.Parse("#AABBCC");
        Assert.Equal("#AABBCC", color.Hex);
    }

    [Fact]
    public void Hex_TranslucentColor_IncludesAlpha()
    {
        var color = ThemeColor.Parse("#80AABBCC");
        Assert.Equal("#80AABBCC", color.Hex);
    }

    [Fact]
    public void ToString_ReturnsHex()
    {
        var color = ThemeColor.Parse("#112233");
        Assert.Equal("#112233", color.ToString());
    }

    [Fact]
    public void ToMediaColor_ReturnsMatchingMediaColor()
    {
        var color = ThemeColor.Parse("#80FF8000");
        var media = color.ToMediaColor();
        Assert.Equal(Color.FromArgb(0x80, 0xFF, 0x80, 0x00), media);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = ThemeColor.Parse("#FF8800");
        var b = ThemeColor.Parse("#FF8800");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = ThemeColor.Parse("#FF8800");
        var b = ThemeColor.Parse("#FF8801");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Argb_SameColor_IsEqual()
    {
        var a = ThemeColor.Parse("#80AABBCC");
        var b = ThemeColor.Parse("#80AABBCC");
        Assert.Equal(a.Argb, b.Argb);
    }

    [Theory]
    [InlineData("#FF0000", "#FF0001")]
    [InlineData("#FF0000", "#FF0100")]
    [InlineData("#FF0000", "#FE0000")]  // same RGB, different A would require ARGB form
    [InlineData("#80FF0000", "#81FF0000")]
    public void Argb_DifferentColors_AreNotEqual(string hex1, string hex2)
    {
        var a = ThemeColor.Parse(hex1);
        var b = ThemeColor.Parse(hex2);
        Assert.NotEqual(a.Argb, b.Argb);
    }

    [Fact]
    public void Argb_ConsistentWithComponents()
    {
        var color = ThemeColor.Parse("#80AABBCC");
        // Argb encodes all four channels; verify it distinguishes each independently
        var sameA = ThemeColor.Parse("#80AABBCC");
        var differentR = ThemeColor.Parse("#80FFBBCC");
        var differentG = ThemeColor.Parse("#80AAFFCC");
        var differentB = ThemeColor.Parse("#80AABBFF");
        var differentA = ThemeColor.Parse("#01AABBCC");
        Assert.Equal(color.Argb, sameA.Argb);
        Assert.NotEqual(color.Argb, differentR.Argb);
        Assert.NotEqual(color.Argb, differentG.Argb);
        Assert.NotEqual(color.Argb, differentB.Argb);
        Assert.NotEqual(color.Argb, differentA.Argb);
    }
}
