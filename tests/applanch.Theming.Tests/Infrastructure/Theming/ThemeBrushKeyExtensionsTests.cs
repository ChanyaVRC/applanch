using applanch.Theming;
using Xunit;

namespace applanch.Tests.Infrastructure.Theming;

public sealed class ThemeBrushKeyExtensionsTests
{
    [Theory]
    [InlineData("Brush.AppBackground")]
    [InlineData("brush.appbackground")]
    [InlineData("BRUSH.APPBACKGROUND")]
    public void TryParseResourceKey_IsCaseInsensitive(string resourceKey)
    {
        var parsed = ThemeBrushKeyExtensions.TryParseResourceKey(resourceKey, out var key);

        Assert.True(parsed);
        Assert.Equal(ThemeBrushKey.AppBackground, key);
    }
}
