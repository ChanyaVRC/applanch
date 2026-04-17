using System.Buffers;
using Microsoft.Win32;
using System.IO;
using System.Security.Cryptography;
using System.Security;
using applanch.ShellIntegration;
using applanch.Utilities;

namespace applanch.Infrastructure.Integration;

internal sealed class ContextMenuRegistrar
{
    private readonly IContextMenuRegistrarRuntime _runtime;

    private const string BasePath = @"Software\Classes";
    private const string MenuKeyName = "applanch.register";
    private const string ShellExtensionAssemblyName = "applanch.ShellExtension";
    private const string ShellExtensionDisplayName = "Applanch Explorer Command";
    private const string ShellExtensionDeploymentDirectoryName = "ShellExtension";
    private const string LegacyMisspelledFileSystemObjectsKeyPath = @"Software\Classes\AllFilesystemObjects\shell\applanch.register";
    private static string MenuText => AppResources.ContextMenu_Register;
    private static readonly RegistrationTarget[] RegistrationTargets =
    [
        // Windows 11 aggregates context-menu sources differently; registering
        // the same verb under AllFileSystemObjects improves discoverability.
        new("AllFileSystemObjects", "%1", true),
        new("*", "%1", true),
        new("exefile", "%1", true),
        new("Directory", "%1", true),
        new("Directory\\Background", "%V", false)
    ];

    public ContextMenuRegistrar()
        : this(new ContextMenuRegistrarRuntime())
    {
    }

    internal ContextMenuRegistrar(IContextMenuRegistrarRuntime runtime)
    {
        _runtime = runtime;
    }

    public void EnsureRegistered()
    {
        CleanupLegacyRegistrationSafely();

        var exePath = _runtime.GetExecutablePath();
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return;
        }

        var explorerCommandEnabled = TryRegisterExplorerCommandServer(exePath);

        // Registry writes can fail due to policy or permissions; skip known registry failures per target.
        foreach (var target in RegistrationTargets)
        {
            RegisterTargetSafely(exePath, target, explorerCommandEnabled && target.SupportsExplorerCommand);
        }
    }

    public void Unregister()
    {
        DeleteRegistrySafely(_runtime.DeleteRegistrySubKeyTree, GetExplorerCommandClassKeyPath());
        DeleteRegistrySafely(_runtime.DeleteRegistrySubKeyTree, GetExplorerCommandProgIdKeyPath());

        foreach (var target in RegistrationTargets)
        {
            DeleteRegistrySafely(_runtime.DeleteRegistrySubKeyTree, GetTargetMenuKeyPath(target));
        }
    }

    private bool TryRegisterExplorerCommandServer(string exePath)
    {
        try
        {
            if (!_runtime.IsExplorerCommandAllowed())
            {
                AppLogger.Instance.Info("Windows 11 explorer command registration skipped: sparse package identity is not registered.");
                return false;
            }

            var shellExtensionComHostPath = _runtime.ResolveShellExtensionComHostPath(exePath);
            if (string.IsNullOrWhiteSpace(shellExtensionComHostPath))
            {
                AppLogger.Instance.Info("Windows 11 explorer command registration skipped because shell-extension artifacts were not found.");
                return false;
            }

            _runtime.RegisterExplorerCommandServer(shellExtensionComHostPath);
            return true;
        }
        catch (Exception ex) when (TryLogKnownRegistryFailure("Explorer command registration", ex))
        {
        }

        return false;
    }

    private void CleanupLegacyRegistrationSafely()
    {
        try
        {
            _runtime.DeleteRegistrySubKeyTree(LegacyMisspelledFileSystemObjectsKeyPath);
        }
        catch (Exception ex) when (TryLogKnownRegistryFailure("Registry cleanup for legacy context menu key", ex))
        {
        }
    }

    private void RegisterTargetSafely(string exePath, RegistrationTarget target, bool enableExplorerCommand)
    {
        try
        {
            RegisterTarget(exePath, target, enableExplorerCommand);
        }
        catch (Exception ex) when (TryLogKnownRegistryFailure($"Registry registration for {target.ClassKeyPath}", ex))
        {
        }
    }

    private static void DeleteRegistrySafely(Action<string> delete, string keyPath)
    {
        try
        {
            delete(keyPath);
        }
        catch (Exception ex) when (TryLogKnownRegistryFailure($"Registry deletion for {keyPath}", ex))
        {
        }
    }

    private void RegisterTarget(string exePath, RegistrationTarget target, bool enableExplorerCommand)
    {
        var keyPath = GetTargetMenuKeyPath(target);
        var command = $"\"{exePath}\" {App.RegisterArgument} \"{target.ArgumentToken}\"";
        _runtime.WriteRegistryCommand(keyPath, MenuText, exePath, command, enableExplorerCommand);
    }

    private static string GetTargetMenuKeyPath(RegistrationTarget target)
        => $"{BasePath}\\{target.ClassKeyPath}\\shell\\{MenuKeyName}";

    private static string GetExplorerCommandClassKeyPath()
        => $"{BasePath}\\CLSID\\{{{ExplorerCommandIds.ClassId}}}";

    private static string GetExplorerCommandProgIdKeyPath()
        => $"{BasePath}\\{ExplorerCommandIds.ProgId}";

    private static bool TryLogKnownRegistryFailure(string operation, Exception ex)
    {
        var reason = ex switch
        {
            UnauthorizedAccessException => "denied",
            SecurityException => "security error",
            IOException => "I/O error",
            _ => null
        };

        if (reason is null)
        {
            return false;
        }

        AppLogger.Instance.Warn(ex, $"{operation} {reason}");
        return true;
    }

    internal static string? ResolveShellExtensionComHostPath(string exePath)
    {
        var sourceArtifactsDirectory = ResolveShellExtensionArtifactsDirectory(exePath);
        if (string.IsNullOrWhiteSpace(sourceArtifactsDirectory))
        {
            return null;
        }

        var deploymentDirectory = AppDataPaths.GetUnderLocalApplicationData(
            ShellExtensionDeploymentDirectoryName,
            ComputeShellExtensionDeploymentKey(sourceArtifactsDirectory));

        if (!HasAllRequiredArtifacts(deploymentDirectory))
        {
            Directory.CreateDirectory(deploymentDirectory);

            foreach (var sourceArtifactPath in GetRequiredArtifacts(sourceArtifactsDirectory))
            {
                var destinationArtifactPath = Path.Combine(deploymentDirectory, Path.GetFileName(sourceArtifactPath));
                File.Copy(sourceArtifactPath, destinationArtifactPath, overwrite: true);
            }
        }

        return Path.Combine(deploymentDirectory, ShellExtensionAssemblyName + ".comhost.dll");
    }

    private static string? ResolveShellExtensionArtifactsDirectory(string exePath)
    {
        var executableDirectory = Path.GetDirectoryName(exePath);
        if (!string.IsNullOrWhiteSpace(executableDirectory) && HasAllRequiredArtifacts(executableDirectory))
        {
            return executableDirectory;
        }

        if (string.IsNullOrWhiteSpace(executableDirectory))
        {
            return null;
        }

        var targetFrameworkDirectory = new DirectoryInfo(executableDirectory);
        var configurationDirectory = targetFrameworkDirectory?.Parent;
        var binDirectory = configurationDirectory?.Parent;
        var applanchProjectDirectory = binDirectory?.Parent;
        var sourceDirectory = applanchProjectDirectory?.Parent;

        if (!string.Equals(binDirectory?.Name, "bin", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(applanchProjectDirectory?.Name, "applanch", StringComparison.OrdinalIgnoreCase) ||
            sourceDirectory is null)
        {
            return null;
        }

        var siblingProjectOutputDirectory = Path.Combine(
            sourceDirectory.FullName,
            ShellExtensionAssemblyName,
            "bin",
            configurationDirectory!.Name,
            targetFrameworkDirectory!.Name);

        return HasAllRequiredArtifacts(siblingProjectOutputDirectory)
            ? siblingProjectOutputDirectory
            : null;
    }

    private static bool HasAllRequiredArtifacts(string directoryPath)
        => GetRequiredArtifacts(directoryPath).All(File.Exists);

    private static string[] GetRequiredArtifacts(string directoryPath)
        =>
        [
            Path.Combine(directoryPath, ShellExtensionAssemblyName + ".dll"),
            Path.Combine(directoryPath, ShellExtensionAssemblyName + ".comhost.dll"),
            Path.Combine(directoryPath, ShellExtensionAssemblyName + ".deps.json"),
            Path.Combine(directoryPath, ShellExtensionAssemblyName + ".runtimeconfig.json")
        ];

    private static string ComputeShellExtensionDeploymentKey(string sourceArtifactsDirectory)
    {
        var fingerprintSource = string.Join(
            '|',
            GetRequiredArtifacts(sourceArtifactsDirectory)
                .Select(static path => new FileInfo(path))
                .Select(static info => $"{info.FullName}:{info.Length}:{info.LastWriteTimeUtc.Ticks}"));

        var byteCount = System.Text.Encoding.UTF8.GetByteCount(fingerprintSource);
        var rentedBytes = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            var writtenByteCount = System.Text.Encoding.UTF8.GetBytes(fingerprintSource.AsSpan(), rentedBytes);
            Span<byte> hashBytes = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(rentedBytes.AsSpan(0, writtenByteCount), hashBytes);
            return Convert.ToHexString(hashBytes[..8]);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBytes);
        }
    }

    internal static void RegisterExplorerCommandServer(string shellExtensionComHostPath)
    {
        var classKeyPath = GetExplorerCommandClassKeyPath();
        using (var classKey = Registry.CurrentUser.CreateSubKey(classKeyPath))
        {
            if (classKey is null)
            {
                return;
            }

            classKey.SetValue(string.Empty, ShellExtensionDisplayName);
            classKey.SetValue("ProgId", ExplorerCommandIds.ProgId);
        }

        using (var inProcServerKey = Registry.CurrentUser.CreateSubKey(classKeyPath + "\\InprocServer32"))
        {
            if (inProcServerKey is null)
            {
                return;
            }

            inProcServerKey.SetValue(string.Empty, shellExtensionComHostPath);
            inProcServerKey.SetValue("ThreadingModel", "Both");
        }

        using var progIdKey = Registry.CurrentUser.CreateSubKey(GetExplorerCommandProgIdKeyPath());
        if (progIdKey is null)
        {
            return;
        }

        progIdKey.SetValue(string.Empty, ShellExtensionDisplayName);
        progIdKey.SetValue("CLSID", $"{{{ExplorerCommandIds.ClassId}}}");
    }

    internal static void WriteRegistryCommand(string keyPath, string menuText, string iconPath, string command, bool enableExplorerCommand)
    {
        using var shellKey = Registry.CurrentUser.CreateSubKey(keyPath);
        if (shellKey is null)
        {
            return;
        }

        shellKey.SetValue(string.Empty, menuText);
        shellKey.SetValue("Icon", iconPath);

        if (enableExplorerCommand)
        {
            shellKey.SetValue("ExplorerCommandHandler", $"{{{ExplorerCommandIds.ClassId}}}");
            shellKey.SetValue("MultiSelectModel", "Single");
        }
        else
        {
            shellKey.DeleteValue("ExplorerCommandHandler", throwOnMissingValue: false);
            shellKey.DeleteValue("MultiSelectModel", throwOnMissingValue: false);
        }

        using var commandKey = shellKey.CreateSubKey("command");
        commandKey?.SetValue(string.Empty, command);
    }

    private readonly record struct RegistrationTarget(string ClassKeyPath, string ArgumentToken, bool SupportsExplorerCommand);
}
