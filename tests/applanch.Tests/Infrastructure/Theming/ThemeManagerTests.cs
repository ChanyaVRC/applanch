using System.Windows;
using System.Windows.Media;
using Xunit;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;

namespace applanch.Tests.Infrastructure.Theming;

public class ThemeManagerTests
{
    [Fact]
    public void ApplyTheme_LightTheme_SetsExpectedPrimaryBrush()
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeManager(
            () => new AppSettings { ThemeId = ThemePaletteConfigurationLoader.LightThemeId },
            BuildConfiguration());

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#0F172A")!, brush.Color);

        var notificationBrush = Assert.IsType<SolidColorBrush>(resources["Brush.NotificationInfoBackground"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#FFFFFF")!, notificationBrush.Color);
    }

    [Fact]
    public void ApplyTheme_DarkTheme_SetsExpectedPrimaryBrush()
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeManager(
            () => new AppSettings { ThemeId = ThemePaletteConfigurationLoader.DarkThemeId },
            BuildConfiguration());

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#E2E8F0")!, brush.Color);

        var notificationBrush = Assert.IsType<SolidColorBrush>(resources["Brush.NotificationInfoBackground"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#131D31")!, notificationBrush.Color);
    }

    [Fact]
    public void ApplyTheme_CustomThemeId_SetsExpectedPrimaryBrush()
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeManager(
            () => new AppSettings { ThemeId = "monochrome" },
            BuildConfiguration());

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#1A1A1A")!, brush.Color);

        var notificationBrush = Assert.IsType<SolidColorBrush>(resources["Brush.NotificationInfoBackground"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#FFFFFF")!, notificationBrush.Color);
    }

    [Fact]
    public void ApplyTheme_UnknownThemeId_FallsBackToLight()
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeManager(
            () => new AppSettings { ThemeId = "unknown-theme" },
            BuildConfiguration());

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#0F172A")!, brush.Color);

        var notificationBrush = Assert.IsType<SolidColorBrush>(resources["Brush.NotificationInfoBackground"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#FFFFFF")!, notificationBrush.Color);
    }

    [Fact]
    public void ApplyTheme_WithWindows_StillUpdatesBrushes()
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeManager(
            () => new AppSettings { ThemeId = ThemePaletteConfigurationLoader.LightThemeId },
            BuildConfiguration());

        manager.ApplyTheme(resources, []);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#0F172A")!, brush.Color);
    }

    [Fact]
    public void ApplyTheme_UsesProvidedPalette()
    {
        var resources = new ResourceDictionary();
        var configuration = new ThemePaletteConfiguration(
            [new ThemeDefinition("sunset", new LocalizedText("Sunset"))],
            [new ThemePaletteEntry("Brush.Custom", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["sunset"] = "#AABBCC" })],
            LoadedFromConfig: true);
        var manager = new ThemeManager(
            () => new AppSettings { ThemeId = "sunset" },
            configuration);

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.Custom"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#AABBCC")!, brush.Color);
    }

    private static ThemePaletteConfiguration BuildConfiguration() =>
        new(
            [
                new ThemeDefinition("light", new LocalizedText("Light")),
                new ThemeDefinition("dark", new LocalizedText("Dark")),
                new ThemeDefinition("monochrome", new LocalizedText("Monochrome"))
            ],
            [
                new ThemePaletteEntry(
                    "Brush.TextPrimary",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["light"] = "#0F172A",
                        ["dark"] = "#E2E8F0",
                        ["monochrome"] = "#1A1A1A",
                    }),
                new ThemePaletteEntry(
                    "Brush.NotificationInfoBackground",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["light"] = "#FFFFFF",
                        ["dark"] = "#131D31",
                        ["monochrome"] = "#FFFFFF",
                    })
            ],
            LoadedFromConfig: true);
}


