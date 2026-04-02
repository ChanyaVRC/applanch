using System.Windows;
using System.Windows.Media;
using Xunit;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;

namespace applanch.Tests.Infrastructure.Theming;

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

        var notificationBrush = Assert.IsType<SolidColorBrush>(resources["Brush.NotificationInfoBackground"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#FFFFFF")!, notificationBrush.Color);
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

        var notificationBrush = Assert.IsType<SolidColorBrush>(resources["Brush.NotificationInfoBackground"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#131D31")!, notificationBrush.Color);

        var iconColor = WindowIconThemeHelper.ResolveIconColor(resources);
        Assert.Equal(WindowIconThemeHelper.DarkPaletteIconColor, iconColor);
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

        var notificationBrush = Assert.IsType<SolidColorBrush>(resources["Brush.NotificationInfoBackground"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#FFFFFF")!, notificationBrush.Color);

        var iconColor = WindowIconThemeHelper.ResolveIconColor(resources);
        Assert.Equal(WindowIconThemeHelper.LightPaletteIconColor, iconColor);
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

        var notificationBrush = Assert.IsType<SolidColorBrush>(resources["Brush.NotificationInfoBackground"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#FFFFFF")!, notificationBrush.Color);
    }

    [Fact]
    public void ApplyTheme_WithWindows_StillUpdatesBrushes()
    {
        var resources = new ResourceDictionary();
        var manager = new ThemeApplier(
            () => new AppSettings { ThemeId = ThemePaletteConfigurationLoader.LightThemeId },
            BuildConfiguration());

        manager.ApplyTheme(resources, []);

        var brush = Assert.IsType<SolidColorBrush>(resources["Brush.TextPrimary"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#0F172A")!, brush.Color);
    }

    [Fact]
    public void ApplyTheme_WithWindow_AppliesThemedIcon()
    {
        RunInSta(() =>
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
            [new ThemePaletteEntry("Brush.Custom", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["sunset"] = "#AABBCC" })],
            LoadedFromConfig: true);
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
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [ThemePaletteConfigurationLoader.LightThemeId] = "#0F172A",
                        [ThemePaletteConfigurationLoader.DarkThemeId] = "#E2E8F0",
                        ["monochrome"] = "#1A1A1A",
                    })
            ],
            LoadedFromConfig: true);
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
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["light"] = "#0F172A",
                        ["base"] = "#1A1A1A",
                    })
            ],
            LoadedFromConfig: true);
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
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["light"] = "#0F172A",
                    })
            ],
            LoadedFromConfig: true);
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

    private static void RunInSta(Action action)
    {
        Exception? captured = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            throw new Xunit.Sdk.XunitException($"STA test failed: {captured}");
        }
    }
}



