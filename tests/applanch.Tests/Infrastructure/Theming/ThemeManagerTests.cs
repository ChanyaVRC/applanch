using System.Windows;
using System.Windows.Media;
using Xunit;
using applanch.Infrastructure.Theming;

namespace applanch.Tests.Infrastructure.Theming;

public class ThemeManagerTests
{
    [Fact]
    public void ApplyTheme_LightTheme_SetsExpectedPrimaryBrush()
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeManager(() => AppTheme.Light);

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#0F172A")!, brush.Color);
    }

    [Fact]
    public void ApplyTheme_DarkTheme_SetsExpectedPrimaryBrush()
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeManager(() => AppTheme.Dark);

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#E2E8F0")!, brush.Color);
    }
}


