using applanch.Core.Utilities;
using Xunit;

namespace applanch.Tests.Infrastructure.Utilities;

public class AppVersionProviderTests
{
    [Fact]
    public void CurrentVersion_ReturnsSemanticVersion()
    {
        var version = AppVersionProvider.CurrentVersion;

        Assert.True(version.Major >= 0);
        Assert.True(version.Minor >= 0);
        Assert.True(version.Patch >= 0);
    }
}
