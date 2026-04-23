using System.Reflection;
using applanch.Settings;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Application;

public class AppArgumentHandlingTests
{
    [Fact]
    public void Parse_WithNoArgs_ReturnsNullRegisterPath()
    {
        var parsed = AppStartupArguments.Parse([]);

        Assert.Null(parsed.RegisterPath);
    }

    [Fact]
    public void Parse_WithRegisterArgumentOnly_ReturnsEmptyRegisterPath()
    {
        var parsed = AppStartupArguments.Parse([AppStartupArguments.RegisterArgument]);

        Assert.Equal(string.Empty, parsed.RegisterPath);
    }

    [Fact]
    public void Parse_WithDifferentOption_ReturnsNullRegisterPath()
    {
        var parsed = AppStartupArguments.Parse(["--other", "C:\\temp\\tool.exe"]);

        Assert.Null(parsed.RegisterPath);
    }

    [Fact]
    public void Parse_WithRegisterOptionAndMissingPath_ReturnsRegisterPath()
    {
        var parsed = AppStartupArguments.Parse([AppStartupArguments.RegisterArgument, "C:\\path\\that\\does\\not\\exist.exe"]);

        Assert.Equal("C:\\path\\that\\does\\not\\exist.exe", parsed.RegisterPath);
    }

    [Fact]
    public void ParseCommandLineArguments_WithThemeAndRegisterArguments_ReturnsBothValues()
    {
        var parsedArgs = AppStartupArguments.Parse([
            AppStartupArguments.RegisterArgument,
            "C:\\temp\\tool.exe",
            AppStartupArguments.ThemeArgument,
            "preview-theme" ]);

        Assert.True(parsedArgs.TryGetValue(AppStartupArguments.RegisterArgument, out var registerPath));
        Assert.True(parsedArgs.TryGetValue(AppStartupArguments.ThemeArgument, out var themeValue));
        Assert.Equal("C:\\temp\\tool.exe", registerPath);
        Assert.Equal("preview-theme", themeValue);
    }

    [Fact]
    public void CreateStartupSettings_WithParsedThemeOverride_UsesPreviewTheme()
    {
        var parsedArgs = AppStartupArguments.Parse([
            AppStartupArguments.RegisterArgument,
            "C:\\temp\\tool.exe",
            AppStartupArguments.ThemeArgument,
            "preview-theme" ]);

        var overrideThemeId = parsedArgs.ThemeOverrideId;
        var startupSettings = InvokeCreateStartupSettings(
            new AppSettings { ThemeId = "light" },
            overrideThemeId);

        Assert.Equal("preview-theme", startupSettings.ThemeId);
    }

    [Fact]
    public void CreatePersistedSettings_WithStartupOverrideTheme_RestoresConfiguredTheme()
    {
        var persisted = InvokeCreatePersistedSettings(
            new AppSettings { ThemeId = "preview-theme" },
            "preview-theme",
            "dark");

        Assert.Equal("dark", persisted.ThemeId);
    }

    [Fact]
    public void CreatePersistedSettings_WithUserChangedTheme_KeepsUserTheme()
    {
        var persisted = InvokeCreatePersistedSettings(
            new AppSettings { ThemeId = "light" },
            "preview-theme",
            "dark");

        Assert.Equal("light", persisted.ThemeId);
    }

    [Fact]
    public void CreatePersistedSettings_WithoutOverride_UsesNormalizedSettings()
    {
        var persisted = InvokeCreatePersistedSettings(
            new AppSettings { ThemeId = "dark" },
            null,
            "light");

        Assert.Equal("dark", persisted.ThemeId);
    }

    private static AppSettings InvokeCreateStartupSettings(AppSettings settings, string? overrideThemeId)
    {
        var method = typeof(App).GetMethod("CreateStartupSettings", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return Assert.IsType<AppSettings>(method.Invoke(null, [settings, overrideThemeId]));
    }

    private static AppSettings InvokeCreatePersistedSettings(
        AppSettings settings,
        string? startupThemeOverrideId,
        string? startupConfiguredThemeId)
    {
        var method = typeof(App).GetMethod("CreatePersistedSettings", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return Assert.IsType<AppSettings>(method.Invoke(null, [settings, startupThemeOverrideId, startupConfiguredThemeId]));
    }

    [Fact]
    public void ThemeOverride_IsNotStoredAsInstanceField()
    {
        // The override is one-shot: applied only during startup via a local variable,
        // not persisted as a field that could leak into Refresh or OnUserPreferenceChanged.
        var field = typeof(App).GetField("_overrideThemeId", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.Null(field);
    }

    [Fact]
    public void CreateStartupSettings_WithNoOverride_UsesConfiguredTheme()
    {
        // Mirrors Refresh behavior: when no override is present (post-startup), the
        // configured theme from settings is used unchanged.
        var result = InvokeCreateStartupSettings(new AppSettings { ThemeId = "dark-theme" }, null);

        Assert.Equal("dark-theme", result.ThemeId);
    }

    [Fact]
    public void TryHandleStartupArgument_WithRegisterArgumentOnly_ReturnsTrue()
    {
        var startupArguments = AppStartupArguments.Parse([AppStartupArguments.RegisterArgument]);
        bool handled = false;

        WpfTestHost.RunInSta(() =>
        {
            var app = new App();
            var method = typeof(App).GetMethod("TryHandleStartupArgument", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            handled = Assert.IsType<bool>(method.Invoke(app, [startupArguments]));
            app.Shutdown();
        });

        Assert.True(handled);
    }
}

