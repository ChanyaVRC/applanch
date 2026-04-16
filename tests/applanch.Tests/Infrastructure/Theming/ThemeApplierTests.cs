using System.Windows;
using System.Windows.Media;
using applanch.Theming;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Theming;

[Collection("WpfTests")]
public class ThemeApplierTests
{
    public static TheoryData<string, string, string> ApplyThemePrimaryBrushCases =>
        new()
        {
            { ThemePaletteConfigurationLoader.LightThemeId, "#0F172A", "#0F172A" },
            { ThemePaletteConfigurationLoader.DarkThemeId, "#E2E8F0", "#FFFFF0" },
            { "monochrome", "#1A1A1A", "#0F172A" },
            { "unknown-theme", "#0F172A", "#0F172A" },
        };

    [Theory]
    [MemberData(nameof(ApplyThemePrimaryBrushCases))]
    public void ApplyTheme_SetsExpectedPrimaryBrushAndIconColor(
        string themeId,
        string expectedBrushHex,
        string expectedIconColorHex)
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeApplier(BuildConfiguration());

        manager.ApplyTheme(resources, themeId);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        var expectedIconColor = (Color)ColorConverter.ConvertFromString(expectedIconColorHex)!;
        Assert.Equal((Color)ColorConverter.ConvertFromString(expectedBrushHex)!, brush.Color);
        Assert.Equal(expectedIconColor, WindowIconThemeHelper.ResolveIconColor(resources));
    }

    [Fact]
    public void ApplyTheme_WithWindow_AppliesThemedIcon()
    {
        WpfTestHost.RunInSta(() =>
        {
            var resources = new ResourceDictionary();
            var manager = new ThemeApplier(BuildConfiguration());
            var window = new Window();

            manager.ApplyTheme(resources, ThemePaletteConfigurationLoader.LightThemeId, [window]);

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
        var manager = new ThemeApplier(configuration);

        manager.ApplyTheme(resources, "sunset");

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
        var manager = new ThemeApplier(configuration);

        manager.ApplyTheme(resources, ThemePaletteConfigurationLoader.SystemThemeId);

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
        var manager = new ThemeApplier(configuration);

        manager.ApplyTheme(resources, "ocean");

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
        var manager = new ThemeApplier(configuration);

        manager.ApplyTheme(resources, "alpha");

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
