using applanch.Infrastructure.Integration;

namespace applanch.Tests.Infrastructure.Integration.TestDoubles;

internal sealed class SparsePackageRuntimeTestDouble : ISparsePackageRuntime
{
    public Func<string?> ResolveMsixPathHandler { get; init; } = static () => null;
    public Func<string?> ResolveExternalLocationHandler { get; init; } = static () => null;
    public Func<string, string, bool> IsPackageRegisteredHandler { get; init; } = static (_, _) => false;
    public Func<bool> ShouldAttemptRegistrationHandler { get; init; } = static () => true;
    public Func<string, string, Task<bool>> RegisterPackageAsyncHandler { get; init; } = static (_, _) => Task.FromResult(false);

    public string? ResolveMsixPath() => ResolveMsixPathHandler();

    public string? ResolveExternalLocation() => ResolveExternalLocationHandler();

    public bool IsPackageRegistered(string name, string publisher) => IsPackageRegisteredHandler(name, publisher);

    public bool ShouldAttemptRegistration() => ShouldAttemptRegistrationHandler();

    public Task<bool> RegisterPackageAsync(string msixPath, string externalLocation)
        => RegisterPackageAsyncHandler(msixPath, externalLocation);
}
