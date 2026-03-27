using System.Windows;
using System.Windows.Media;
using Xunit;

namespace applanch.Tests;

public class ThemeManagerTests
{
    [Fact]
    public void ApplyTheme_LightTheme_SetsExpectedPrimaryBrush()
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeManager(() => true);

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#0F172A")!, brush.Color);
    }

    [Fact]
    public void ApplyTheme_DarkTheme_SetsExpectedPrimaryBrush()
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeManager(() => false);

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#E2E8F0")!, brush.Color);
    }
}
