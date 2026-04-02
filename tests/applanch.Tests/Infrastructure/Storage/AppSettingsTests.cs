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
    public void ResolvePostLaunchBehavior_UsesExplicitValue_WhenProvided()
    {
        var settings = new AppSettings
        {
            PostLaunchBehavior = PostLaunchBehavior.MinimizeWindow,
            CloseOnLaunch = true,
        };

        var behavior = settings.ResolvePostLaunchBehavior();

        Assert.Equal(PostLaunchBehavior.MinimizeWindow, behavior);
    }

    [Fact]
    public void ResolvePostLaunchBehavior_FallsBackToCloseOnLaunch_WhenNotProvided()
    {
        var closeSettings = new AppSettings { CloseOnLaunch = true };
        var keepOpenSettings = new AppSettings { CloseOnLaunch = false };

        Assert.Equal(PostLaunchBehavior.CloseApp, closeSettings.ResolvePostLaunchBehavior());
        Assert.Equal(PostLaunchBehavior.KeepOpen, keepOpenSettings.ResolvePostLaunchBehavior());
    }

    [Fact]
    public void Defaults_EnableHttpIconFetching_ButBlockPrivateRequests()
    {
        var settings = new AppSettings();

        Assert.True(settings.FetchHttpIcons);
        Assert.False(settings.AllowPrivateNetworkHttpIconRequests);
    }
}
