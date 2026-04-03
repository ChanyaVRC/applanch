using applanch.Infrastructure.Utilities;
using Xunit;

namespace applanch.Tests.Infrastructure.Utilities;

public class AppVersionProviderTests
{
    [Fact]
    public void GetDisplayVersion_ReturnsNonEmptyValue()
    {
        var version = AppVersionProvider.GetDisplayVersion();

        Assert.False(string.IsNullOrWhiteSpace(version));
    }

    [Fact]
    public void GetDisplayVersion_DoesNotContainBuildMetadataSuffix()
    {
        var version = AppVersionProvider.GetDisplayVersion();

        Assert.DoesNotContain('+', version);
    }
}
