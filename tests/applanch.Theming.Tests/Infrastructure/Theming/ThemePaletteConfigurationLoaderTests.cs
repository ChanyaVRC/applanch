using applanch.Theming;
using applanch.Core.Configuration;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Theming;

public sealed class ThemePaletteConfigurationLoaderTests
{
    [Fact]
    public void BundledConfig_InRepository_IsLoadableByCurrentLoader()
    {
        var appBase = Path.Combine(ProjectPaths.Root, "src", "applanch");

        var loaded = ThemePaletteConfigurationLoader.TryLoadFromDirectory(appBase, out var configuration);

        Assert.True(loaded);
        Assert.Contains(configuration.Themes, t => t.Id == ThemePaletteConfigurationLoader.SystemThemeId);
        Assert.Contains(configuration.Themes, t => t.Id == ThemePaletteConfigurationLoader.LightThemeId);
        Assert.Contains(configuration.Themes, t => t.Id == ThemePaletteConfigurationLoader.DarkThemeId);
        Assert.All(configuration.Themes, t => Assert.Equal(t.Id.ToLowerInvariant(), t.Id));
    }

    [Fact]
    public void UserDefinedSampleConfig_InRepository_IsLoadableByCurrentLoader()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        var userDefinedDirectory = Path.Combine(appBase, "Config", "UserDefined", "theme-palette");
        Directory.CreateDirectory(userDefinedDirectory);

        var samplePath = Path.Combine(ProjectPaths.Root, "src", "applanch", "Config", "UserDefined", "theme-palette", "theme-palette.sample.json");
        File.Copy(samplePath, Path.Combine(userDefinedDirectory, "theme-palette.sample.json"));

        try
        {
            // Sample has enabled=false by default, so the definition is loaded but hidden from options
            var loaded = ThemePaletteConfigurationLoader.TryLoadUserDefined(appBase, out var configuration);

            Assert.True(loaded);
            var myTheme = Assert.Single(configuration.Themes, t => t.Id == "my-theme");
            Assert.False(myTheme.IsVisibleInThemeList);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

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

            var entry = Assert.Single(configuration.Entries);
            Assert.Equal("Brush.Custom", entry.Key);
            Assert.Equal(ThemeColor.Parse("#102030"), entry.ColorsByThemeId["light"]);
            Assert.Equal(ThemeColor.Parse("#405060"), entry.ColorsByThemeId["dark"]);
            Assert.Equal(ThemeColor.Parse("#708090"), entry.ColorsByThemeId["monochrome"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryLoadFromDirectory_WhenConfigMissing_ReturnsFalse()
    {
        using var scope = BundledConfigLoadNotificationTestScope.Enter();
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config"));

        try
        {
            var loaded = ThemePaletteConfigurationLoader.TryLoadFromDirectory(appBase, out var configuration);

            Assert.False(loaded);
            Assert.Empty(configuration.Themes);
            Assert.Contains(
                BundledConfigLoadNotificationCenter.DrainPending(),
                static issue => issue == new BundledConfigLoadIssue("theme-palette.json", IsInvalidFormat: false));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryLoadFromDirectory_WhenConfigIsInvalid_ReportsInvalidFormat()
    {
        using var scope = BundledConfigLoadNotificationTestScope.Enter();
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config"));
        File.WriteAllText(Path.Combine(appBase, "Config", "theme-palette.json"), "{");

        try
        {
            var loaded = ThemePaletteConfigurationLoader.TryLoadFromDirectory(appBase, out var configuration);

            Assert.False(loaded);
            Assert.Empty(configuration.Themes);
            Assert.Contains(
                BundledConfigLoadNotificationCenter.DrainPending(),
                static issue => issue == new BundledConfigLoadIssue("theme-palette.json", IsInvalidFormat: true));
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
            var themes = configuration.Themes;
            Assert.Contains(themes, t => t.Id == "ocean");
            var theme = Assert.Single(themes, t => t.Id == "ocean");
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
    public void TryLoadFromDirectory_WithoutDisplayNames_UsesTitleCasedThemeIdAsFallback()
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
                        "id": "high__contrast-dark",
                        "entries": [
                            { "key": "Brush.Custom", "hex": "#123456" }
                        ]
                    }
                ]
            }
            """);

        try
        {
            var loaded = ThemePaletteConfigurationLoader.TryLoadFromDirectory(appBase, out var configuration);

            Assert.True(loaded);
            var theme = Assert.Single(configuration.Themes, static t => t.Id == "high__contrast-dark");
            Assert.Equal("High Contrast Dark", theme.DisplayName.Resolve(LanguageOption.English));
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
            var loaded = ThemePaletteConfigurationLoader.TryLoadUserDefined(appBase, out var configuration);

            Assert.False(loaded);
            Assert.Empty(configuration.Themes);
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
            var theme = Assert.Single(configuration.Themes, t => t.Id == "ocean");
            Assert.Equal("ocean", theme.Id);
            Assert.Equal("Ocean", theme.DisplayName.Resolve(LanguageOption.English));
            var entry = Assert.Single(configuration.Entries);
            Assert.Equal(ThemeColor.Parse("#001E3C"), entry.ColorsByThemeId["ocean"]);
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
            Assert.Equal(ThemeColor.Parse("#112244"), background.ColorsByThemeId["ocean"]);
            Assert.Equal(ThemeColor.Parse("#102A1A"), background.ColorsByThemeId["forest"]);

            var surface = Assert.Single(configuration.Entries, e => e.Key == "Brush.Surface");
            Assert.Equal(ThemeColor.Parse("#0A1628"), surface.ColorsByThemeId["ocean"]);
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
        Assert.Equal(ThemeColor.Parse("#F0F0F0"), entry.ColorsByThemeId["light"]);
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
        Assert.Equal(ThemeColor.Parse("#F8F8F8"), surface.ColorsByThemeId["light"]);
        Assert.Equal(ThemeColor.Parse("#111111"), surface.ColorsByThemeId["dark"]);

        var background = Assert.Single(merged.Entries, e => e.Key == "Brush.AppBackground");
        Assert.Equal(ThemeColor.Parse("#F0F0F0"), background.ColorsByThemeId["light"]);
        Assert.Equal(ThemeColor.Parse("#000000"), background.ColorsByThemeId["dark"]);
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
            [new ThemePaletteEntry("Brush.AppBackground", new Dictionary<string, ThemeColor>(StringComparer.OrdinalIgnoreCase) { ["light"] = ThemeColor.Parse("#FFFFFF") })]);
        var overlay = new ThemePaletteConfiguration(
            [new FixedThemeDefinition("sunset", new LocalizedText("Sunset"), "dark")],
            [new ThemePaletteEntry("Brush.AppBackground", new Dictionary<string, ThemeColor>(StringComparer.OrdinalIgnoreCase) { ["sunset"] = ThemeColor.Parse("#FFEECC") })]);

        var merged = ThemePaletteConfigurationLoader.Merge(@base, overlay);

        var sunsetTheme = Assert.IsType<FixedThemeDefinition>(Assert.Single(merged.Themes, t => t.Id == "sunset"));
        Assert.Equal("dark", sunsetTheme.InheritedThemeId);
    }

    [Fact]
    public void Merge_WhenOverlayChangesThemeType_OverlayTypeWins()
    {
        var @base = new ThemePaletteConfiguration(
            [
                new SystemDependentThemeDefinition(
                    "system",
                    new LocalizedText("System"),
                    new Dictionary<SystemThemeMode, string>
                    {
                        [SystemThemeMode.Light] = "light",
                        [SystemThemeMode.Dark] = "dark",
                    }),
                new FixedThemeDefinition("light", new LocalizedText("Light")),
                new FixedThemeDefinition("dark", new LocalizedText("Dark"))
            ],
            [
                new ThemePaletteEntry("Brush.Custom", new Dictionary<string, ThemeColor>(StringComparer.OrdinalIgnoreCase)
                {
                    ["light"] = ThemeColor.Parse("#FFFFFF"),
                    ["dark"] = ThemeColor.Parse("#000000"),
                })
            ]);
        var overlay = new ThemePaletteConfiguration(
            [new FixedThemeDefinition("system", new LocalizedText("System Override"), "light")],
            []);

        var merged = ThemePaletteConfigurationLoader.Merge(@base, overlay);

        var systemTheme = Assert.IsType<FixedThemeDefinition>(Assert.Single(merged.Themes, t => t.Id == "system"));
        Assert.Equal("light", systemTheme.InheritedThemeId);
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
                    static c => ThemeColor.Parse(c.Hex),
                    StringComparer.OrdinalIgnoreCase)))
            .ToList();

        return new ThemePaletteConfiguration(themeDefinitions, entryList);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "applanch-theme-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
