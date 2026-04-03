using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Theming;

public sealed class ThemeOptionsProviderTests
{
    [Fact]
    public void BuildOptions_WhenConfigurationContainsSystem_DoesNotDuplicateSystemOption()
    {
        var configuration = new ThemePaletteConfiguration(
            [
                new FixedThemeDefinition(ThemePaletteConfigurationLoader.SystemThemeId, new LocalizedText("System")),
                new FixedThemeDefinition(ThemePaletteConfigurationLoader.LightThemeId, new LocalizedText("Light")),
                new FixedThemeDefinition("monochrome", new LocalizedText("Monochrome"))
            ],
            [],
            LoadedFromConfig: true);

        var options = ThemeOptionsProvider.BuildOptions(configuration);

        Assert.Collection(
            options,
            option =>
            {
                Assert.Equal(ThemePaletteConfigurationLoader.SystemThemeId, option.ThemeId);
                Assert.True(option.IsSystemOption);
            },
            option =>
            {
                Assert.Equal(ThemePaletteConfigurationLoader.LightThemeId, option.ThemeId);
                Assert.False(option.IsSystemOption);
            },
            option =>
            {
                Assert.Equal("monochrome", option.ThemeId);
                Assert.False(option.IsSystemOption);
            });
    }

    [Fact]
    public void BuildOptions_WhenConfigurationOmitsSystem_PrependsSyntheticSystemOption()
    {
        var configuration = new ThemePaletteConfiguration(
            [
                new FixedThemeDefinition(ThemePaletteConfigurationLoader.LightThemeId, new LocalizedText("Light")),
                new FixedThemeDefinition("monochrome", new LocalizedText("Monochrome"))
            ],
            [],
            LoadedFromConfig: true);

        var options = ThemeOptionsProvider.BuildOptions(configuration);

        Assert.Collection(
            options,
            option =>
            {
                Assert.Equal(ThemePaletteConfigurationLoader.SystemThemeId, option.ThemeId);
                Assert.True(option.IsSystemOption);
            },
            option => Assert.Equal(ThemePaletteConfigurationLoader.LightThemeId, option.ThemeId),
            option => Assert.Equal("monochrome", option.ThemeId));
    }

    [Fact]
    public void BuildOptions_WhenSystemThemeComesFromConfiguration_UsesLocalizedDisplayName()
    {
        var configuration = new ThemePaletteConfiguration(
            [
                new FixedThemeDefinition(
                    ThemePaletteConfigurationLoader.SystemThemeId,
                    new LocalizedText(
                        "System",
                        new Dictionary<LanguageOption, string>
                        {
                            [LanguageOption.Japanese] = "システム設定"
                        }))
            ],
            [],
            LoadedFromConfig: true);

        using var cultureScope = new CultureScope("ja-JP");

        var option = Assert.Single(ThemeOptionsProvider.BuildOptions(configuration));

        Assert.Equal("システム設定", option.DisplayName);
        Assert.True(option.IsSystemOption);
    }
}
