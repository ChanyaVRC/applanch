using Xunit;
using applanch.Infrastructure.Integration;

namespace applanch.Tests.Infrastructure.Integration;

public class SparsePackageRegistrarTests
{
    [Fact]
    public void IsAlreadyRegistered_ReturnsFalse_WhenCheckerReportsNotRegistered()
    {
        var registrar = new SparsePackageRegistrar(
            msixPathProvider: () => @"C:\App\applanch.msix",
            externalLocationProvider: () => @"C:\App",
            isPackageRegisteredChecker: (_, _) => false,
            registerPackageAsync: (_, _) => Task.FromResult(false));

        Assert.False(registrar.IsAlreadyRegistered());
    }

    [Fact]
    public void IsAlreadyRegistered_ReturnsTrue_WhenCheckerReportsRegistered()
    {
        var registrar = new SparsePackageRegistrar(
            msixPathProvider: () => @"C:\App\applanch.msix",
            externalLocationProvider: () => @"C:\App",
            isPackageRegisteredChecker: (_, _) => true,
            registerPackageAsync: (_, _) => Task.FromResult(false));

        Assert.True(registrar.IsAlreadyRegistered());
    }

    [Fact]
    public async Task TryEnsureRegistered_ReturnsFalse_WhenMsixPathIsNull()
    {
        var registrar = new SparsePackageRegistrar(
            msixPathProvider: () => null,
            externalLocationProvider: () => @"C:\App",
            isPackageRegisteredChecker: (_, _) => false,
            registerPackageAsync: (_, _) => Task.FromResult(true));

        var result = await registrar.TryEnsureRegisteredAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task TryEnsureRegistered_ReturnsFalse_WhenExternalLocationIsNull()
    {
        var registrar = new SparsePackageRegistrar(
            msixPathProvider: () => @"C:\App\applanch.msix",
            externalLocationProvider: () => null,
            isPackageRegisteredChecker: (_, _) => false,
            registerPackageAsync: (_, _) => Task.FromResult(true));

        var result = await registrar.TryEnsureRegisteredAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task TryEnsureRegistered_InvokesRegisterWithCorrectPaths_WhenArtifactsAreAvailable()
    {
        const string expectedMsix = @"C:\App\applanch.msix";
        const string expectedLocation = @"C:\App";
        string? capturedMsix = null;
        string? capturedLocation = null;

        var registrar = new SparsePackageRegistrar(
            msixPathProvider: () => expectedMsix,
            externalLocationProvider: () => expectedLocation,
            isPackageRegisteredChecker: (_, _) => false,
            registerPackageAsync: (msix, location) =>
            {
                capturedMsix = msix;
                capturedLocation = location;
                return Task.FromResult(true);
            });

        var result = await registrar.TryEnsureRegisteredAsync();

        Assert.True(result);
        Assert.Equal(expectedMsix, capturedMsix);
        Assert.Equal(expectedLocation, capturedLocation);
    }

    [Fact]
    public async Task TryEnsureRegistered_ReturnsFalse_WhenRegistrationFails()
    {
        var registrar = new SparsePackageRegistrar(
            msixPathProvider: () => @"C:\App\applanch.msix",
            externalLocationProvider: () => @"C:\App",
            isPackageRegisteredChecker: (_, _) => false,
            registerPackageAsync: (_, _) => Task.FromResult(false));

        var result = await registrar.TryEnsureRegisteredAsync();

        Assert.False(result);
    }
}
