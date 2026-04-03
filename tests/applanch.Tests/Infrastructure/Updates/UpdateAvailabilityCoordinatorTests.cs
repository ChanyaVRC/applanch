using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Updates;
using Xunit;

namespace applanch.Tests.Infrastructure.Updates;

public class UpdateAvailabilityCoordinatorTests
{
    [Fact]
    public void ShouldAutoApply_AutoMode_FirstObservation_ReturnsTrue()
    {
        var coordinator = new UpdateAvailabilityCoordinator();

        var shouldAutoApply = coordinator.ShouldAutoApply(CreateUpdate("1.2.0"), UpdateInstallBehavior.AutomaticallyApply);

        Assert.True(shouldAutoApply);
    }

    [Theory]
    [InlineData((int)UpdateInstallBehavior.Manual)]
    [InlineData((int)UpdateInstallBehavior.NotifyOnly)]
    public void ShouldAutoApply_NonAutoMode_ReturnsFalse(int behaviorValue)
    {
        var coordinator = new UpdateAvailabilityCoordinator();
        var behavior = (UpdateInstallBehavior)behaviorValue;

        var shouldAutoApply = coordinator.ShouldAutoApply(CreateUpdate("1.2.0"), behavior);

        Assert.False(shouldAutoApply);
    }

    [Fact]
    public void ShouldAutoApply_SameVersionAfterAttempt_ReturnsFalse()
    {
        var coordinator = new UpdateAvailabilityCoordinator();

        Assert.True(coordinator.ShouldAutoApply(CreateUpdate("1.2.0"), UpdateInstallBehavior.AutomaticallyApply));

        var shouldAutoApply = coordinator.ShouldAutoApply(CreateUpdate("1.2.0"), UpdateInstallBehavior.AutomaticallyApply);

        Assert.False(shouldAutoApply);
    }

    [Fact]
    public void ShouldAutoApply_ClearsVersionHistory_WhenUpdateBecomesNull()
    {
        var coordinator = new UpdateAvailabilityCoordinator();

        Assert.True(coordinator.ShouldAutoApply(CreateUpdate("1.2.0"), UpdateInstallBehavior.AutomaticallyApply));
        Assert.False(coordinator.ShouldAutoApply(null, UpdateInstallBehavior.AutomaticallyApply));

        var shouldAutoApply = coordinator.ShouldAutoApply(CreateUpdate("1.2.0"), UpdateInstallBehavior.AutomaticallyApply);

        Assert.True(shouldAutoApply);
    }

    [Fact]
    public void ShouldAutoApply_WhileAutomaticApplyRunning_ReturnsFalse()
    {
        var coordinator = new UpdateAvailabilityCoordinator();
        coordinator.BeginAutomaticApply();

        var shouldAutoApply = coordinator.ShouldAutoApply(CreateUpdate("1.2.0"), UpdateInstallBehavior.AutomaticallyApply);

        Assert.False(shouldAutoApply);
    }

    private static AppUpdateInfo CreateUpdate(string version)
    {
        return new AppUpdateInfo(
            version,
            "1.0.0",
            new Uri("https://example.com/download.zip"),
            new Uri("https://example.com/release"));
    }
}
