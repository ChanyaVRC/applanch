using System.IO;
using System.Runtime.Versioning;
using Windows.Management.Deployment;
using applanch.Utilities;

namespace applanch.Infrastructure.Integration;

internal sealed class SparsePackageRuntime : ISparsePackageRuntime
{
    public string? ResolveMsixPath()
    {
        var executableDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        if (string.IsNullOrWhiteSpace(executableDirectory))
        {
            return null;
        }

        var msixPath = Path.Combine(executableDirectory, SparsePackageRegistrar.SparsePackageFileName);
        return File.Exists(msixPath) ? msixPath : null;
    }

    public string? ResolveExternalLocation()
    {
        var dir = Path.GetDirectoryName(Environment.ProcessPath);
        return string.IsNullOrWhiteSpace(dir) ? null : dir;
    }

    public bool ShouldAttemptRegistration()
    {
#if DEBUG
        if (System.Diagnostics.Debugger.IsAttached && !IsDebugRegistrationExplicitlyEnabled())
        {
            return false;
        }
#endif

        return true;
    }

    [SupportedOSPlatform("windows10.0.19041.0")]
    public bool IsPackageRegistered(string name, string publisher)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
        {
            return false;
        }

        try
        {
            var manager = new PackageManager();
            return manager.FindPackagesForUser(string.Empty, name, publisher).Any();
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn(ex, "Sparse package registration check failed");
            return false;
        }
    }

    [SupportedOSPlatform("windows10.0.22000.0")]
    public async Task<bool> RegisterPackageAsync(string msixPath, string externalLocation)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            AppLogger.Instance.Info("Sparse package registration skipped: requires Windows 11.");
            return false;
        }

        try
        {
            var manager = new PackageManager();
            var options = new AddPackageOptions
            {
                ExternalLocationUri = new Uri("file:///" + externalLocation.Replace('\\', '/').TrimEnd('/')),
            };

            var result = await manager.AddPackageByUriAsync(new Uri(msixPath), options);
            if (result.ExtendedErrorCode is null)
            {
                AppLogger.Instance.Info("Sparse package registered successfully.");
                return true;
            }

            AppLogger.Instance.Warn($"Sparse package registration failed: 0x{result.ExtendedErrorCode.HResult:X8} - {result.ErrorText}");
            AppLogger.Instance.Warn("If the MSIX is unsigned, run scripts/setup-dev-signing.ps1 (as Admin) then rebuild with scripts/build-sparse-package.ps1.");
            return false;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn(ex, "Sparse package registration error");
            return false;
        }
    }

    private static bool IsDebugRegistrationExplicitlyEnabled()
    {
        var value = Environment.GetEnvironmentVariable(SparsePackageRegistrar.DebugRegistrationOverrideEnvironmentVariable);
        return value is not null &&
            (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase));
    }
}
