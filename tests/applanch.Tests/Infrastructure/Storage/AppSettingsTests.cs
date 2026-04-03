using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using Xunit;

namespace applanch.Tests.Infrastructure.Storage;

public class AppSettingsTests
{
    [Fact]
    public void Normalize_WhenThemeIdIsNull_UsesSystemThemeId()
    {
        var settings = new AppSettings { ThemeId = null! };

        var normalized = AppSettings.Normalize(settings);

        Assert.Equal(ThemePaletteConfigurationLoader.SystemThemeId, normalized.ThemeId);
    }

    [Fact]
    public void Normalize_WhenThemeIdIsWhitespace_UsesSystemThemeId()
    {
        var settings = new AppSettings { ThemeId = "   " };

        var normalized = AppSettings.Normalize(settings);

        Assert.Equal(ThemePaletteConfigurationLoader.SystemThemeId, normalized.ThemeId);
    }

    [Fact]
    public void Normalize_WhenThemeIdHasSurroundingSpaces_Trims()
    {
        var settings = new AppSettings { ThemeId = "  monochrome  " };

        var normalized = AppSettings.Normalize(settings);

        Assert.Equal("monochrome", normalized.ThemeId);
    }

    [Fact]
    public void Normalize_PreservesPostLaunchBehavior()
    {
        var settings = new AppSettings { PostLaunchBehavior = PostLaunchBehavior.MinimizeWindow };

        var normalized = AppSettings.Normalize(settings);

        Assert.Equal(PostLaunchBehavior.MinimizeWindow, normalized.PostLaunchBehavior);
    }

    [Fact]
    public void Defaults_UseCloseAppPostLaunchBehavior()
    {
        var settings = new AppSettings();

        Assert.Equal(PostLaunchBehavior.CloseApp, settings.PostLaunchBehavior);
    }

    [Fact]
    public void Defaults_EnableHttpIconFetching_ButBlockPrivateRequests()
    {
        var settings = new AppSettings();

        Assert.True(settings.FetchHttpIcons);
        Assert.False(settings.AllowPrivateNetworkHttpIconRequests);
    }
}
