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
            []);

        var options = ThemeOptionsProvider.BuildOptions(configuration);

        Assert.Equal(3, options.Count);
        Assert.True(options[ThemePaletteConfigurationLoader.SystemThemeId].IsSystemOption);
        Assert.False(options[ThemePaletteConfigurationLoader.LightThemeId].IsSystemOption);
        Assert.False(options["monochrome"].IsSystemOption);
    }

    [Fact]
    public void BuildOptions_WhenConfigurationOmitsSystem_DoesNotIncludeMissingSystem()
    {
        var configuration = new ThemePaletteConfiguration(
            [
                new FixedThemeDefinition(ThemePaletteConfigurationLoader.LightThemeId, new LocalizedText("Light")),
                new FixedThemeDefinition("monochrome", new LocalizedText("Monochrome"))
            ],
            []);

        var options = ThemeOptionsProvider.BuildOptions(configuration);

        Assert.Equal(2, options.Count);
        Assert.True(options.ContainsKey(ThemePaletteConfigurationLoader.LightThemeId));
        Assert.True(options.ContainsKey("monochrome"));
        Assert.False(options.ContainsKey(ThemePaletteConfigurationLoader.SystemThemeId));
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
            []);

        using var cultureScope = new CultureScope("ja-JP");

        var options = ThemeOptionsProvider.BuildOptions(configuration);
        var option = Assert.Single(options.Values);

        Assert.Equal("システム設定", option.DisplayName);
        Assert.True(option.IsSystemOption);
    }

    [Fact]
    public void BuildOptions_WhenThemeIsHidden_ExcludesItFromOptions()
    {
        var configuration = new ThemePaletteConfiguration(
            [
                new FixedThemeDefinition(ThemePaletteConfigurationLoader.LightThemeId, new LocalizedText("Light")),
                new FixedThemeDefinition("hidden-theme", new LocalizedText("Hidden"), isVisibleInThemeList: false)
            ],
            []);

        var options = ThemeOptionsProvider.BuildOptions(configuration);

        Assert.DoesNotContain("hidden-theme", options.Keys);
        Assert.Single(options);
        Assert.True(options.ContainsKey(ThemePaletteConfigurationLoader.LightThemeId));
    }
}
