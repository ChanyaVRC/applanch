namespace applanch.Infrastructure.Integration;

internal interface ISparsePackageRuntime
{
    string? ResolveMsixPath();

    string? ResolveExternalLocation();

    bool IsPackageRegistered(string name, string publisher);

    bool ShouldAttemptRegistration();

    Task<bool> RegisterPackageAsync(string msixPath, string externalLocation);
}
