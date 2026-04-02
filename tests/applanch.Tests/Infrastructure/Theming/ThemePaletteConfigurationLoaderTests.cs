using System.Globalization;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using Xunit;

namespace applanch.Tests.Infrastructure.Theming;

public sealed class ThemePaletteConfigurationLoaderTests
{
    [Fact]
    public void TryLoadFromDirectory_LoadsPaletteEntriesFromThemes()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config"));
        File.WriteAllText(
            Path.Combine(appBase, "Config", "theme-palette.json"),
            """
            {
                            "themes": [
                                {
                                    "id": "light",
                                    "displayNames": { "en": "Light", "ja": "ライト" },
                                    "entries": [
                                        { "key": "Brush.Custom", "hex": "#102030" }
                                    ]
                                },
                                {
                                    "id": "dark",
                                    "displayNames": { "en": "Dark", "ja": "ダーク" },
                                    "entries": [
                                        { "key": "Brush.Custom", "hex": "#405060" }
                                    ]
                                },
                                {
                                    "id": "monochrome",
                                    "displayNames": { "en": "Monochrome", "ja": "モノクローム" },
                                    "entries": [
                                        { "key": "Brush.Custom", "hex": "#708090" }
                                    ]
                                }
              ]
            }
            """);

        try
        {
            var loaded = ThemePaletteConfigurationLoader.TryLoadFromDirectory(appBase, out var configuration);

            Assert.True(loaded);
            Assert.True(configuration.LoadedFromConfig);

            var entry = Assert.Single(configuration.Entries);
            Assert.Equal("Brush.Custom", entry.Key);
            Assert.Equal("#102030", entry.ColorsByThemeId["light"]);
            Assert.Equal("#405060", entry.ColorsByThemeId["dark"]);
            Assert.Equal("#708090", entry.ColorsByThemeId["monochrome"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryLoadFromDirectory_WhenConfigMissing_ReturnsFalse()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config"));

        try
        {
            var loaded = ThemePaletteConfigurationLoader.TryLoadFromDirectory(appBase, out _);

            Assert.False(loaded);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryLoadFromDirectory_LoadsThemeDisplayNamesByLanguage()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config"));
        File.WriteAllText(
                Path.Combine(appBase, "Config", "theme-palette.json"),
                """
                        {
                            "themes": [
                                {
                                    "id": "ocean",
                                    "displayName": "Ocean",
                                    "displayNames": {
                                        "en": "Ocean",
                                        "ja": "オーシャン",
                                        "fr": "Ocean FR"
                                    },
                                    "entries": [
                                        {
                                            "key": "Brush.Custom",
                                            "hex": "#112233"
                                        }
                                    ]
                                }
                            ]
                        }
                        """);

        try
        {
            var loaded = ThemePaletteConfigurationLoader.TryLoadFromDirectory(appBase, out var configuration);

            Assert.True(loaded);
            var theme = Assert.Single(configuration.Themes);
            using var cultureScope = new CultureScope("fr-FR");
            Assert.Equal("Ocean", theme.DisplayName.ResolveCurrentCulture());
            Assert.Equal("オーシャン", theme.DisplayName.Resolve(LanguageOption.Japanese));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryLoadFromDirectory_WhenJsonContainsSystemTheme_LoadsSystemThemeDefinition()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config"));
        File.WriteAllText(
            Path.Combine(appBase, "Config", "theme-palette.json"),
            """
            {
                "themes": [
                    {
                        "id": "system",
                        "displayNames": { "en": "System", "ja": "システム" }
                    },
                    {
                        "id": "light",
                        "entries": [
                            { "key": "Brush.Custom", "hex": "#112233" }
                        ]
                    },
                    {
                        "id": "dark",
                        "entries": [
                            { "key": "Brush.Custom", "hex": "#445566" }
                        ]
                    }
                ]
            }
            """);

        try
        {
            var loaded = ThemePaletteConfigurationLoader.TryLoadFromDirectory(appBase, out var configuration);

            Assert.True(loaded);
            var systemTheme = Assert.Single(configuration.Themes, t => t.Id == ThemePaletteConfigurationLoader.SystemThemeId);
            Assert.Equal("システム", systemTheme.DisplayName.Resolve(LanguageOption.Japanese));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryLoadFromDirectory_WhenSystemThemeHasEntriesFrom_LoadsSystemDependentThemeDefinition()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config"));
        File.WriteAllText(
            Path.Combine(appBase, "Config", "theme-palette.json"),
            """
            {
                "themes": [
                    {
                        "id": "system",
                        "entriesFrom": {
                            "light": "sunrise",
                            "dark": "midnight"
                        }
                    },
                    {
                        "id": "sunrise",
                        "entries": [
                            { "key": "Brush.Custom", "hex": "#112233" }
                        ]
                    },
                    {
                        "id": "midnight",
                        "entries": [
                            { "key": "Brush.Custom", "hex": "#445566" }
                        ]
                    }
                ]
            }
            """);

        try
        {
            var loaded = ThemePaletteConfigurationLoader.TryLoadFromDirectory(appBase, out var configuration);

            Assert.True(loaded);
            var systemTheme = Assert.IsType<SystemDependentThemeDefinition>(
                Assert.Single(configuration.Themes, t => t.Id == ThemePaletteConfigurationLoader.SystemThemeId));
            Assert.Equal("sunrise", systemTheme.SourcesByMode[SystemThemeMode.Light]);
            Assert.Equal("midnight", systemTheme.SourcesByMode[SystemThemeMode.Dark]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryLoadFromDirectory_WhenThemeHasEntriesFromString_LoadsFixedThemeInheritance()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config"));
        File.WriteAllText(
            Path.Combine(appBase, "Config", "theme-palette.json"),
            """
            {
                "themes": [
                    {
                        "id": "light",
                        "entries": [
                            { "key": "Brush.Custom", "hex": "#112233" }
                        ]
                    },
                    {
                        "id": "sunset",
                        "entriesFrom": "light",
                        "entries": [
                            { "key": "Brush.Custom2", "hex": "#AABBCC" }
                        ]
                    }
                ]
            }
            """);

        try
        {
            var loaded = ThemePaletteConfigurationLoader.TryLoadFromDirectory(appBase, out var configuration);

            Assert.True(loaded);
            var sunsetTheme = Assert.IsType<FixedThemeDefinition>(
                Assert.Single(configuration.Themes, t => t.Id == "sunset"));
            Assert.Equal("light", sunsetTheme.InheritedThemeId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryLoadUserDefined_WhenFileAbsent_ReturnsFalse()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config", "UserDefined"));

        try
        {
            var loaded = ThemePaletteConfigurationLoader.TryLoadUserDefined(appBase, out _);

            Assert.False(loaded);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryLoadUserDefined_WhenFilePresent_LoadsThemes()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config", "UserDefined", "theme-palette"));
        File.WriteAllText(
            Path.Combine(appBase, "Config", "UserDefined", "theme-palette", "ocean.json"),
            """
            {
                "themes": [
                    {
                        "id": "ocean",
                        "displayNames": { "en": "Ocean", "ja": "オーシャン" },
                        "entries": [
                            { "key": "Brush.AppBackground", "hex": "#001E3C" }
                        ]
                    }
                ]
            }
            """);

        try
        {
            var loaded = ThemePaletteConfigurationLoader.TryLoadUserDefined(appBase, out var configuration);

            Assert.True(loaded);
            var theme = Assert.Single(configuration.Themes);
            Assert.Equal("ocean", theme.Id);
            Assert.Equal("Ocean", theme.DisplayName.Resolve(LanguageOption.English));
            var entry = Assert.Single(configuration.Entries);
            Assert.Equal("#001E3C", entry.ColorsByThemeId["ocean"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryLoadUserDefined_MergesAllJsonFilesInDirectory()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config", "UserDefined", "theme-palette"));

        File.WriteAllText(
            Path.Combine(appBase, "Config", "UserDefined", "theme-palette", "01-base.json"),
            """
            {
                "themes": [
                    {
                        "id": "ocean",
                        "displayNames": { "en": "Ocean", "ja": "オーシャン" },
                        "entries": [
                            { "key": "Brush.AppBackground", "hex": "#001E3C" }
                        ]
                    }
                ]
            }
            """);

        File.WriteAllText(
            Path.Combine(appBase, "Config", "UserDefined", "theme-palette", "02-override.json"),
            """
            {
                "themes": [
                    {
                        "id": "ocean",
                        "displayNames": { "en": "Ocean Override", "ja": "オーシャン" },
                        "entries": [
                            { "key": "Brush.AppBackground", "hex": "#112244" },
                            { "key": "Brush.Surface", "hex": "#0A1628" }
                        ]
                    },
                    {
                        "id": "forest",
                        "displayNames": { "en": "Forest", "ja": "フォレスト" },
                        "entries": [
                            { "key": "Brush.AppBackground", "hex": "#102A1A" }
                        ]
                    }
                ]
            }
            """);

        try
        {
            var loaded = ThemePaletteConfigurationLoader.TryLoadUserDefined(appBase, out var configuration);

            Assert.True(loaded);
            Assert.Contains(configuration.Themes, t => t.Id == "ocean");
            Assert.Contains(configuration.Themes, t => t.Id == "forest");

            var background = Assert.Single(configuration.Entries, e => e.Key == "Brush.AppBackground");
            Assert.Equal("#112244", background.ColorsByThemeId["ocean"]);
            Assert.Equal("#102A1A", background.ColorsByThemeId["forest"]);

            var surface = Assert.Single(configuration.Entries, e => e.Key == "Brush.Surface");
            Assert.Equal("#0A1628", surface.ColorsByThemeId["ocean"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Merge_AddsNewThemesFromOverlay()
    {
        var @base = BuildConfig(
            [("light", "Light", "#FFFFFF")],
            [("Brush.AppBackground", [("light", "#FFFFFF")])]);
        var overlay = BuildConfig(
            [("ocean", "Ocean", "#001E3C")],
            [("Brush.AppBackground", [("ocean", "#001E3C")])]);

        var merged = ThemePaletteConfigurationLoader.Merge(@base, overlay);

        Assert.Equal(2, merged.Themes.Count);
        Assert.Contains(merged.Themes, t => t.Id == "light");
        Assert.Contains(merged.Themes, t => t.Id == "ocean");
    }

    [Fact]
    public void Merge_OverlayEntryColorsOverrideBaseForSameTheme()
    {
        var @base = BuildConfig(
            [("light", "Light", "#FFFFFF")],
            [("Brush.AppBackground", [("light", "#FFFFFF")])]);
        var overlay = BuildConfig(
            [("light", "Light", "#F0F0F0")],
            [("Brush.AppBackground", [("light", "#F0F0F0")])]);

        var merged = ThemePaletteConfigurationLoader.Merge(@base, overlay);

        var entry = Assert.Single(merged.Entries, e => e.Key == "Brush.AppBackground");
        Assert.Equal("#F0F0F0", entry.ColorsByThemeId["light"]);
    }

    [Fact]
    public void Merge_BaseEntryColorsNotInOverlayArePreserved()
    {
        var @base = BuildConfig(
            [("light", "Light", "#FFFFFF"), ("dark", "Dark", "#000000")],
            [
                ("Brush.AppBackground", [("light", "#FFFFFF"), ("dark", "#000000")]),
                ("Brush.Surface", [("light", "#F8F8F8"), ("dark", "#111111")])
            ]);
        var overlay = BuildConfig(
            [("light", "Light", "#F0F0F0")],
            [("Brush.AppBackground", [("light", "#F0F0F0")])]);

        var merged = ThemePaletteConfigurationLoader.Merge(@base, overlay);

        var surface = Assert.Single(merged.Entries, e => e.Key == "Brush.Surface");
        Assert.Equal("#F8F8F8", surface.ColorsByThemeId["light"]);
        Assert.Equal("#111111", surface.ColorsByThemeId["dark"]);

        var background = Assert.Single(merged.Entries, e => e.Key == "Brush.AppBackground");
        Assert.Equal("#F0F0F0", background.ColorsByThemeId["light"]);
        Assert.Equal("#000000", background.ColorsByThemeId["dark"]);
    }

    [Fact]
    public void Merge_OverlayThemeWithSameIdIsNotDuplicated()
    {
        var @base = BuildConfig(
            [("light", "Light", "#FFFFFF")],
            [("Brush.AppBackground", [("light", "#FFFFFF")])]);
        var overlay = BuildConfig(
            [("light", "Light Override", "#F0F0F0")],
            [("Brush.AppBackground", [("light", "#F0F0F0")])]);

        var merged = ThemePaletteConfigurationLoader.Merge(@base, overlay);

        Assert.Single(merged.Themes, t => t.Id == "light");
    }

    [Fact]
    public void Merge_OverlayAddsNewEntryKeysMissingFromBase()
    {
        var @base = BuildConfig(
            [("light", "Light", "#FFFFFF")],
            [("Brush.AppBackground", [("light", "#FFFFFF")])]);
        var overlay = BuildConfig(
            [("ocean", "Ocean", "#001E3C")],
            [("Brush.NewKey", [("ocean", "#AABBCC")])]);

        var merged = ThemePaletteConfigurationLoader.Merge(@base, overlay);

        Assert.Contains(merged.Entries, e => e.Key == "Brush.NewKey");
        Assert.Contains(merged.Entries, e => e.Key == "Brush.AppBackground");
    }

    [Fact]
    public void Merge_OverlayThemeDefinitionsOverrideBase()
    {
        var @base = new ThemePaletteConfiguration(
            [
                new FixedThemeDefinition("light", new LocalizedText("Light")),
                new FixedThemeDefinition("sunset", new LocalizedText("Sunset"), "light")
            ],
            [new ThemePaletteEntry("Brush.AppBackground", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["light"] = "#FFFFFF" })],
            LoadedFromConfig: true);
        var overlay = new ThemePaletteConfiguration(
            [new FixedThemeDefinition("sunset", new LocalizedText("Sunset"), "dark")],
            [new ThemePaletteEntry("Brush.AppBackground", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["sunset"] = "#FFEECC" })],
            LoadedFromConfig: true);

        var merged = ThemePaletteConfigurationLoader.Merge(@base, overlay);

        var sunsetTheme = Assert.IsType<FixedThemeDefinition>(Assert.Single(merged.Themes, t => t.Id == "sunset"));
        Assert.Equal("dark", sunsetTheme.InheritedThemeId);
    }

    private static ThemePaletteConfiguration BuildConfig(
        (string Id, string Name, string Hex)[] themes,
        (string Key, (string ThemeId, string Hex)[] Colors)[] entries)
    {
        var themeDefinitions = themes
            .Select(t => new FixedThemeDefinition(t.Id, new LocalizedText(t.Name)))
            .ToList();

        var entryList = entries
            .Select(e => new ThemePaletteEntry(
                e.Key,
                e.Colors.ToDictionary(
                    static c => c.ThemeId,
                    static c => c.Hex,
                    StringComparer.OrdinalIgnoreCase)))
            .ToList();

        return new ThemePaletteConfiguration(themeDefinitions, entryList, LoadedFromConfig: true);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "applanch-theme-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalUiCulture;
        private readonly CultureInfo _originalCulture;

        public CultureScope(string cultureName)
        {
            _originalUiCulture = CultureInfo.CurrentUICulture;
            _originalCulture = CultureInfo.CurrentCulture;

            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentUICulture = _originalUiCulture;
            CultureInfo.CurrentCulture = _originalCulture;
        }
    }
}

