using applanch.Infrastructure.Integration;
using applanch.Tests.Infrastructure.Integration.TestDoubles;
using Xunit;

namespace applanch.Tests.Infrastructure.Integration;

public class SparsePackageRegistrarTests
{
    [Fact]
    public void IsAlreadyRegistered_ReturnsFalse_WhenCheckerReportsNotRegistered()
    {
        var registrar = new SparsePackageRegistrar(new SparsePackageRuntimeTestDouble
        {
            ResolveMsixPathHandler = () => @"C:\App\applanch.msix",
            ResolveExternalLocationHandler = () => @"C:\App",
            IsPackageRegisteredHandler = (_, _) => false,
            ShouldAttemptRegistrationHandler = () => true,
            RegisterPackageAsyncHandler = (_, _) => Task.FromResult(false)
        });

        Assert.False(registrar.IsAlreadyRegistered());
    }

    [Fact]
    public void IsAlreadyRegistered_ReturnsTrue_WhenCheckerReportsRegistered()
    {
        var registrar = new SparsePackageRegistrar(new SparsePackageRuntimeTestDouble
        {
            ResolveMsixPathHandler = () => @"C:\App\applanch.msix",
            ResolveExternalLocationHandler = () => @"C:\App",
            IsPackageRegisteredHandler = (_, _) => true,
            ShouldAttemptRegistrationHandler = () => true,
            RegisterPackageAsyncHandler = (_, _) => Task.FromResult(false)
        });

        Assert.True(registrar.IsAlreadyRegistered());
    }

    [Fact]
    public async Task TryEnsureRegistered_ReturnsFalse_WhenMsixPathIsNull()
    {
        var registrar = new SparsePackageRegistrar(new SparsePackageRuntimeTestDouble
        {
            ResolveMsixPathHandler = () => null,
            ResolveExternalLocationHandler = () => @"C:\App",
            IsPackageRegisteredHandler = (_, _) => false,
            ShouldAttemptRegistrationHandler = () => true,
            RegisterPackageAsyncHandler = (_, _) => Task.FromResult(true)
        });

        var result = await registrar.TryEnsureRegisteredAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task TryEnsureRegistered_ReturnsFalse_WhenExternalLocationIsNull()
    {
        var registrar = new SparsePackageRegistrar(new SparsePackageRuntimeTestDouble
        {
            ResolveMsixPathHandler = () => @"C:\App\applanch.msix",
            ResolveExternalLocationHandler = () => null,
            IsPackageRegisteredHandler = (_, _) => false,
            ShouldAttemptRegistrationHandler = () => true,
            RegisterPackageAsyncHandler = (_, _) => Task.FromResult(true)
        });

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

        var registrar = new SparsePackageRegistrar(new SparsePackageRuntimeTestDouble
        {
            ResolveMsixPathHandler = () => expectedMsix,
            ResolveExternalLocationHandler = () => expectedLocation,
            IsPackageRegisteredHandler = (_, _) => false,
            ShouldAttemptRegistrationHandler = () => true,
            RegisterPackageAsyncHandler = (msix, location) =>
            {
                capturedMsix = msix;
                capturedLocation = location;
                return Task.FromResult(true);
            }
        });

        var result = await registrar.TryEnsureRegisteredAsync();

        Assert.True(result);
        Assert.Equal(expectedMsix, capturedMsix);
        Assert.Equal(expectedLocation, capturedLocation);
    }

    [Fact]
    public async Task TryEnsureRegistered_ReturnsFalse_WhenRegistrationFails()
    {
        var registrar = new SparsePackageRegistrar(new SparsePackageRuntimeTestDouble
        {
            ResolveMsixPathHandler = () => @"C:\App\applanch.msix",
            ResolveExternalLocationHandler = () => @"C:\App",
            IsPackageRegisteredHandler = (_, _) => false,
            ShouldAttemptRegistrationHandler = () => true,
            RegisterPackageAsyncHandler = (_, _) => Task.FromResult(false)
        });

        var result = await registrar.TryEnsureRegisteredAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task TryEnsureRegistered_ReturnsFalse_WhenAttemptIsSuppressed()
    {
        var registrar = new SparsePackageRegistrar(new SparsePackageRuntimeTestDouble
        {
            ResolveMsixPathHandler = () => @"C:\App\applanch.msix",
            ResolveExternalLocationHandler = () => @"C:\App",
            IsPackageRegisteredHandler = (_, _) => false,
            ShouldAttemptRegistrationHandler = () => false,
            RegisterPackageAsyncHandler = (_, _) => Task.FromResult(true)
        });

        var result = await registrar.TryEnsureRegisteredAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task TryEnsureRegistered_DoesNotInvokeRegister_WhenAttemptIsSuppressed()
    {
        var wasInvoked = false;
        var registrar = new SparsePackageRegistrar(new SparsePackageRuntimeTestDouble
        {
            ResolveMsixPathHandler = () => @"C:\App\applanch.msix",
            ResolveExternalLocationHandler = () => @"C:\App",
            IsPackageRegisteredHandler = (_, _) => false,
            ShouldAttemptRegistrationHandler = () => false,
            RegisterPackageAsyncHandler = (_, _) =>
            {
                wasInvoked = true;
                return Task.FromResult(true);
            }
        });

        _ = await registrar.TryEnsureRegisteredAsync();

        Assert.False(wasInvoked);
    }
}




