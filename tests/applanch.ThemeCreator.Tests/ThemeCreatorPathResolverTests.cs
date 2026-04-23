using applanch.ThemeCreator;
using Xunit;

namespace applanch.Tests.ThemeCreator;

public class ThemeCreatorPathResolverTests
{
    [Fact]
    public void ResolveDefaultOutputPath_SourceInConfigDirectory_UsesUserDefinedThemePaletteDirectory()
    {
        var sourcePath = Path.Combine("C:", "repo", "applanch", "Config", "theme-palette.json");

        var outputPath = ThemeCreatorPathResolver.ResolveDefaultOutputPath(sourcePath, "my-theme");

        Assert.Equal(
            Path.Combine("C:", "repo", "applanch", "Config", "UserDefined", "theme-palette", "my-theme.json"),
            outputPath);
    }

    [Fact]
    public void ResolveDefaultOutputPath_SourceInUserDefinedThemePaletteDirectory_UsesConfigUserDefinedThemePaletteDirectory()
    {
        var sourcePath = Path.Combine("C:", "repo", "applanch", "Config", "UserDefined", "theme-palette", "base.json");

        var outputPath = ThemeCreatorPathResolver.ResolveDefaultOutputPath(sourcePath, "custom");

        Assert.Equal(
            Path.Combine("C:", "repo", "applanch", "Config", "UserDefined", "theme-palette", "custom.json"),
            outputPath);
    }

    [Fact]
    public void ResolveDefaultOutputPath_SourceOutsideConfigDirectory_UsesSourceDirectory()
    {
        var sourcePath = Path.Combine("C:", "temp", "source.json");

        var outputPath = ThemeCreatorPathResolver.ResolveDefaultOutputPath(sourcePath, "night-sky");

        Assert.Equal(Path.Combine("C:", "temp", "night-sky.json"), outputPath);
    }

    [Fact]
    public void ResolveDefaultOutputPath_WhitespaceThemeId_UsesDefaultThemeName()
    {
        var sourcePath = Path.Combine("C:", "repo", "applanch", "Config", "theme-palette.json");

        var outputPath = ThemeCreatorPathResolver.ResolveDefaultOutputPath(sourcePath, "   ");

        Assert.Equal(
            Path.Combine("C:", "repo", "applanch", "Config", "UserDefined", "theme-palette", "my-theme.json"),
            outputPath);
    }

    [Fact]
    public void ResolveDefaultOutputPath_ThemeIdWithOnlyTrimmedCharacters_UsesDefaultThemeName()
    {
        var sourcePath = Path.Combine("C:", "repo", "applanch", "Config", "theme-palette.json");

        var outputPath = ThemeCreatorPathResolver.ResolveDefaultOutputPath(sourcePath, "...");

        Assert.Equal(
            Path.Combine("C:", "repo", "applanch", "Config", "UserDefined", "theme-palette", "my-theme.json"),
            outputPath);
    }
}
