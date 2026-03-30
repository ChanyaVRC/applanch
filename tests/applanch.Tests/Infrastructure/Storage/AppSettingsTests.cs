using applanch.Infrastructure.Storage;
using Xunit;

namespace applanch.Tests.Infrastructure.Storage;

public class AppSettingsTests
{
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
}
