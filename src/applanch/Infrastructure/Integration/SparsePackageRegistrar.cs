using applanch.Utilities;

namespace applanch.Infrastructure.Integration;

/// <summary>
/// Registers the sparse MSIX package that grants package identity to applanch,
/// enabling the verb to appear in the Windows 11 simplified context menu.
/// </summary>
internal sealed class SparsePackageRegistrar
{
    private const string PackageName = "Applanch";
    private const string PackagePublisher = "CN=applanch";
    internal const string SparsePackageFileName = "applanch.msix";
    internal const string DebugRegistrationOverrideEnvironmentVariable = "APPLANCH_ENABLE_SPARSE_PACKAGE_REGISTRATION_IN_DEBUG";

    private readonly ISparsePackageRuntime _runtime;

    public SparsePackageRegistrar()
        : this(new SparsePackageRuntime())
    {
    }

    internal SparsePackageRegistrar(ISparsePackageRuntime runtime)
    {
        _runtime = runtime;
    }

    public bool IsAlreadyRegistered()
        => _runtime.IsPackageRegistered(PackageName, PackagePublisher);

    internal static bool IsPackageRegistered()
        => new SparsePackageRuntime().IsPackageRegistered(PackageName, PackagePublisher);

    public Task<bool> TryEnsureRegisteredAsync()
    {
        if (!_runtime.ShouldAttemptRegistration())
        {
            AppLogger.Instance.Info($"Sparse package registration skipped while a debugger is attached. Set {DebugRegistrationOverrideEnvironmentVariable}=1 to force registration during F5 sessions.");
            return Task.FromResult(false);
        }

        var msixPath = _runtime.ResolveMsixPath();
        var externalLocation = _runtime.ResolveExternalLocation();
        if (string.IsNullOrWhiteSpace(msixPath) || string.IsNullOrWhiteSpace(externalLocation))
        {
            AppLogger.Instance.Info("Sparse package registration skipped: applanch.msix not found alongside the executable.");
            return Task.FromResult(false);
        }

        return _runtime.RegisterPackageAsync(msixPath, externalLocation);
    }
}
