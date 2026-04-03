using System.IO;
using System.Runtime.Versioning;
using Windows.Management.Deployment;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Integration;

/// <summary>
/// Registers the sparse MSIX package that grants package identity to applanch,
/// enabling the verb to appear in the Windows 11 simplified context menu.
/// </summary>
internal sealed class SparsePackageRegistrar(
    Func<string?> msixPathProvider,
    Func<string?> externalLocationProvider,
    Func<string, string, bool> isPackageRegisteredChecker,
    Func<string, string, Task<bool>> registerPackageAsync)
{
    private const string PackageName = "Applanch";
    private const string PackagePublisher = "CN=applanch";
    private const string SparsePackageFileName = "applanch.msix";

    public SparsePackageRegistrar()
        : this(ResolveMsixPath, ResolveExternalLocation, IsRegistered, RegisterAsync)
    {
    }

    public bool IsAlreadyRegistered()
        => isPackageRegisteredChecker(PackageName, PackagePublisher);

    public Task<bool> TryEnsureRegisteredAsync()
    {
        var msixPath = msixPathProvider();
        var externalLocation = externalLocationProvider();
        if (string.IsNullOrWhiteSpace(msixPath) || string.IsNullOrWhiteSpace(externalLocation))
        {
            AppLogger.Instance.Info("Sparse package registration skipped: applanch.msix not found alongside the executable.");
            return Task.FromResult(false);
        }

        return registerPackageAsync(msixPath, externalLocation);
    }

    private static string? ResolveMsixPath()
    {
        var executableDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        if (string.IsNullOrWhiteSpace(executableDirectory))
        {
            return null;
        }

        var msixPath = Path.Combine(executableDirectory, SparsePackageFileName);
        return File.Exists(msixPath) ? msixPath : null;
    }

    private static string? ResolveExternalLocation()
    {
        var dir = Path.GetDirectoryName(Environment.ProcessPath);
        return string.IsNullOrWhiteSpace(dir) ? null : dir;
    }

    [SupportedOSPlatform("windows10.0.19041.0")]
    private static bool IsRegistered(string name, string publisher)
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
            AppLogger.Instance.Warn($"Sparse package registration check failed: {ex.Message}");
            return false;
        }
    }

    [SupportedOSPlatform("windows10.0.22000.0")]
    private static async Task<bool> RegisterAsync(string msixPath, string externalLocation)
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
                AllowUnsigned = true,
            };

            var result = await manager.AddPackageByUriAsync(new Uri(msixPath), options);
            if (result.ExtendedErrorCode is null)
            {
                AppLogger.Instance.Info("Sparse package registered successfully.");
                return true;
            }

            AppLogger.Instance.Warn($"Sparse package registration failed: 0x{result.ExtendedErrorCode.HResult:X8} – {result.ErrorText}");
            return false;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Sparse package registration error: {ex.Message}");
            return false;
        }
    }
}
