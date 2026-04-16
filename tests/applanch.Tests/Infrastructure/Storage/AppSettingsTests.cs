using applanch.Settings;
using applanch.Theming;
using System.Text.Json;
using Xunit;

namespace applanch.Tests.Infrastructure.Storage;

public class AppSettingsTests
{
    [Fact]
    public void JsonDeserialize_WhenLanguageIsCodeString_MapsToLanguageOption()
    {
        var settings = JsonSerializer.Deserialize<AppSettings>("""
            {
              "Language": "ja"
            }
            """);

        Assert.NotNull(settings);
        Assert.Equal(LanguageOption.Japanese, settings!.Language);
    }

    [Fact]
    public void JsonDeserialize_WhenLanguageIsLegacyNameString_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<AppSettings>("""
                        {
                            "Language": "English"
                        }
                        """));
    }

    [Fact]
    public void JsonDeserialize_WhenLanguageIsLegacyNumber_MapsToLanguageOption()
    {
        var settings = JsonSerializer.Deserialize<AppSettings>("""
                        {
                            "Language": 1
                        }
                        """);

        Assert.NotNull(settings);
        Assert.Equal(LanguageOption.English, settings!.Language);
    }

    [Fact]
    public void JsonSerialize_WritesLanguageAsCodeString()
    {
        var settings = new AppSettings { Language = LanguageOption.Japanese };

        var json = JsonSerializer.Serialize(settings);

        Assert.Contains("\"Language\":\"ja\"", json);
    }

    [Fact]
    public void Normalize_WhenThemeIdIsNull_ReturnsSystemThemeId()
    {
        var settings = new AppSettings { ThemeId = null! };

        var normalized = settings.Normalize();

        Assert.Equal(ThemePaletteConfigurationLoader.SystemThemeId, normalized.ThemeId);
    }

    [Fact]
    public void Normalize_WhenThemeIdIsWhitespace_ReturnsSystemThemeId()
    {
        var settings = new AppSettings { ThemeId = "   " };

        var normalized = settings.Normalize();

        Assert.Equal(ThemePaletteConfigurationLoader.SystemThemeId, normalized.ThemeId);
    }

    [Fact]
    public void Normalize_WhenThemeIdHasSurroundingSpaces_Trims()
    {
        var settings = new AppSettings { ThemeId = "  monochrome  " };

        var normalized = settings.Normalize();

        Assert.Equal("monochrome", normalized.ThemeId);
    }

    [Fact]
    public void Normalize_PreservesPostLaunchBehavior()
    {
        var settings = new AppSettings { PostLaunchBehavior = PostLaunchBehavior.MinimizeWindow };

        var normalized = settings.Normalize();

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

    [Fact]
    public void Defaults_RegisterContextMenuOnStartup()
    {
        var settings = new AppSettings();

        Assert.True(settings.RegisterContextMenuOnStartup);
    }

    [Fact]
    public void Defaults_DisallowPrereleaseUpdates()
    {
        var settings = new AppSettings();

        Assert.False(settings.AllowPrereleaseUpdates);
    }

    [Fact]
    public void Defaults_UseFiftyQuickAddSuggestions()
    {
        var settings = new AppSettings();

        Assert.Equal(50, settings.QuickAddSuggestionLimit);
    }

    [Fact]
    public void Defaults_DisableLaunchItemIconOnlyMode()
    {
        var settings = new AppSettings();

        Assert.False(settings.LaunchItemIconOnlyMode);
    }

    [Fact]
    public void Defaults_KeepCategorySidebarPinned()
    {
        var settings = new AppSettings();

        Assert.True(settings.CategorySidebarPinned);
    }

    [Fact]
    public void Normalize_WhenQuickAddSuggestionLimitIsTooSmall_ClampsToMinimum()
    {
        var settings = new AppSettings { QuickAddSuggestionLimit = 0 };

        var normalized = settings.Normalize();

        Assert.Equal(1, normalized.QuickAddSuggestionLimit);
    }

    [Fact]
    public void Normalize_WhenQuickAddSuggestionLimitIsTooLarge_ClampsToMaximum()
    {
        var settings = new AppSettings { QuickAddSuggestionLimit = 999 };

        var normalized = settings.Normalize();

        Assert.Equal(200, normalized.QuickAddSuggestionLimit);
    }

    [Fact]
    public void Normalize_PreservesLaunchItemIconOnlyMode()
    {
        var settings = new AppSettings { LaunchItemIconOnlyMode = true };

        var normalized = settings.Normalize();

        Assert.True(normalized.LaunchItemIconOnlyMode);
    }

    [Fact]
    public void Normalize_PreservesCategorySidebarPinned()
    {
        var settings = new AppSettings { CategorySidebarPinned = false };

        var normalized = settings.Normalize();

        Assert.False(normalized.CategorySidebarPinned);
    }

    [Fact]
    public void Normalize_PreservesAllowPrereleaseUpdates()
    {
        var settings = new AppSettings { AllowPrereleaseUpdates = true };

        var normalized = settings.Normalize();

        Assert.True(normalized.AllowPrereleaseUpdates);
    }

}
