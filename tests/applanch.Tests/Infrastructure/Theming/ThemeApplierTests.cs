using System.Windows;
using System.Windows.Media;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Theming;

[Collection("WpfTests")]
public class ThemeApplierTests
{
    [Fact]
    public void ApplyTheme_LightTheme_SetsExpectedPrimaryBrush()
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeApplier(
            () => new AppSettings { ThemeId = ThemePaletteConfigurationLoader.LightThemeId },
            BuildConfiguration());

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#0F172A")!, brush.Color);
    }

    [Fact]
    public void ApplyTheme_DarkTheme_SetsExpectedPrimaryBrush()
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeApplier(
            () => new AppSettings { ThemeId = ThemePaletteConfigurationLoader.DarkThemeId },
            BuildConfiguration());

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#E2E8F0")!, brush.Color);
        Assert.Equal(WindowIconThemeHelper.DarkPaletteIconColor, WindowIconThemeHelper.ResolveIconColor(resources));
    }

    [Fact]
    public void ApplyTheme_CustomThemeId_SetsExpectedPrimaryBrush()
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeApplier(
            () => new AppSettings { ThemeId = "monochrome" },
            BuildConfiguration());

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#1A1A1A")!, brush.Color);
        Assert.Equal(WindowIconThemeHelper.LightPaletteIconColor, WindowIconThemeHelper.ResolveIconColor(resources));
    }

    [Fact]
    public void ApplyTheme_UnknownThemeId_FallsBackToLight()
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeApplier(
            () => new AppSettings { ThemeId = "unknown-theme" },
            BuildConfiguration());

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#0F172A")!, brush.Color);
    }

    [Fact]
    public void ApplyTheme_WithWindow_AppliesThemedIcon()
    {
        WpfTestHost.RunInSta(() =>
        {
            var resources = new ResourceDictionary();
            var manager = new ThemeApplier(
                () => new AppSettings { ThemeId = ThemePaletteConfigurationLoader.LightThemeId },
                BuildConfiguration());
            var window = new Window();

            manager.ApplyTheme(resources, [window]);

            Assert.NotNull(window.Icon);
            Assert.Equal(WindowIconThemeHelper.LightPaletteIconColor, WindowIconThemeHelper.ResolveIconColor(resources));
        });
    }

    [Fact]
    public void ApplyTheme_UsesProvidedPalette()
    {
        var resources = new ResourceDictionary();
        var configuration = new ThemePaletteConfiguration(
            [new FixedThemeDefinition("sunset", new LocalizedText("Sunset"))],
            [new ThemePaletteEntry("Brush.Custom", new Dictionary<string, ThemeColor>(StringComparer.OrdinalIgnoreCase) { ["sunset"] = ThemeColor.Parse("#AABBCC") })]);
        var manager = new ThemeApplier(
            () => new AppSettings { ThemeId = "sunset" },
            configuration);

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.Custom"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#AABBCC")!, brush.Color);
    }

    [Fact]
    public void ApplyTheme_SystemTheme_UsesEntriesFromMappingWhenProvided()
    {
        var resources = new ResourceDictionary();
        var configuration = new ThemePaletteConfiguration(
            [
                new SystemDependentThemeDefinition(
                    ThemePaletteConfigurationLoader.SystemThemeId,
                    new LocalizedText("System"),
                    new Dictionary<SystemThemeMode, string>
                    {
                        [SystemThemeMode.Light] = "monochrome",
                        [SystemThemeMode.Dark] = "monochrome",
                    }),
                new FixedThemeDefinition("monochrome", new LocalizedText("Monochrome")),
                new FixedThemeDefinition(ThemePaletteConfigurationLoader.LightThemeId, new LocalizedText("Light")),
                new FixedThemeDefinition(ThemePaletteConfigurationLoader.DarkThemeId, new LocalizedText("Dark"))
            ],
            [
                new ThemePaletteEntry(
                    "Brush.TextPrimary",
                    new Dictionary<string, ThemeColor>(StringComparer.OrdinalIgnoreCase)
                    {
                        [ThemePaletteConfigurationLoader.LightThemeId] = ThemeColor.Parse("#0F172A"),
                        [ThemePaletteConfigurationLoader.DarkThemeId] = ThemeColor.Parse("#E2E8F0"),
                        ["monochrome"] = ThemeColor.Parse("#1A1A1A"),
                    })
            ]);
        var manager = new ThemeApplier(
            () => new AppSettings { ThemeId = ThemePaletteConfigurationLoader.SystemThemeId },
            configuration);

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#1A1A1A")!, brush.Color);
    }

    [Fact]
    public void ApplyTheme_NonSystemTheme_UsesEntriesFromInheritanceChain()
    {
        var resources = new ResourceDictionary();
        var configuration = new ThemePaletteConfiguration(
            [
                new FixedThemeDefinition("light", new LocalizedText("Light")),
                new FixedThemeDefinition("base", new LocalizedText("Base"), "light"),
                new FixedThemeDefinition("ocean", new LocalizedText("Ocean"), "base")
            ],
            [
                new ThemePaletteEntry(
                    "Brush.TextPrimary",
                    new Dictionary<string, ThemeColor>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["light"] = ThemeColor.Parse("#0F172A"),
                        ["base"] = ThemeColor.Parse("#1A1A1A"),
                    })
            ]);
        var manager = new ThemeApplier(
            () => new AppSettings { ThemeId = "ocean" },
            configuration);

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#1A1A1A")!, brush.Color);
    }

    [Fact]
    public void ApplyTheme_NonSystemTheme_WithCircularEntriesFrom_FallsBackToLight()
    {
        var resources = new ResourceDictionary();
        var configuration = new ThemePaletteConfiguration(
            [
                new FixedThemeDefinition("light", new LocalizedText("Light")),
                new FixedThemeDefinition("alpha", new LocalizedText("Alpha"), "beta"),
                new FixedThemeDefinition("beta", new LocalizedText("Beta"), "alpha")
            ],
            [
                new ThemePaletteEntry(
                    "Brush.TextPrimary",
                    new Dictionary<string, ThemeColor>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["light"] = ThemeColor.Parse("#0F172A"),
                    })
            ]);
        var manager = new ThemeApplier(
            () => new AppSettings { ThemeId = "alpha" },
            configuration);

        manager.ApplyTheme(resources);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#0F172A")!, brush.Color);
    }

    private static ThemePaletteConfiguration BuildConfiguration() =>
        new(
            [
                new FixedThemeDefinition("light", new LocalizedText("Light")),
                new FixedThemeDefinition("dark", new LocalizedText("Dark")),
                new FixedThemeDefinition("monochrome", new LocalizedText("Monochrome"))
            ],
            [
                new ThemePaletteEntry(
                    "Brush.TextPrimary",
                    new Dictionary<string, ThemeColor>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["light"] = ThemeColor.Parse("#0F172A"),
                        ["dark"] = ThemeColor.Parse("#E2E8F0"),
                        ["monochrome"] = ThemeColor.Parse("#1A1A1A"),
                    })
            ]);
}
