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
