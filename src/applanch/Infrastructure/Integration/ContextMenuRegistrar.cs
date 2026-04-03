using Microsoft.Win32;
using System.IO;
using System.Security.Cryptography;
using System.Security;
using applanch.ShellIntegration;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Integration;

internal sealed class ContextMenuRegistrar(
    Func<string?> executablePathProvider,
    Func<string, string?> shellExtensionComHostPathResolver,
    Action<string, string, string, string, bool> writeRegistryCommand,
    Action<string> registerExplorerCommandServer,
    bool enableLegacyCleanup = true,
    Action<string>? deleteRegistrySubKeyTree = null,
    Func<bool>? isExplorerCommandAllowed = null)
{
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

    private static readonly Action<string> DefaultDeleteSubKeyTree =
        static keyPath => Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);

    public ContextMenuRegistrar()
        : this(static () => Environment.ProcessPath, ResolveShellExtensionComHostPath, WriteRegistryCommand, RegisterExplorerCommandServer, enableLegacyCleanup: true, deleteRegistrySubKeyTree: null, isExplorerCommandAllowed: SparsePackageRegistrar.IsPackageRegistered)
    {
    }

    public void EnsureRegistered()
    {
        if (enableLegacyCleanup)
        {
            CleanupLegacyRegistrationSafely();
        }

        var exePath = executablePathProvider();
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
        var delete = deleteRegistrySubKeyTree ?? DefaultDeleteSubKeyTree;

        DeleteRegistrySafely(delete, GetExplorerCommandClassKeyPath());
        DeleteRegistrySafely(delete, GetExplorerCommandProgIdKeyPath());

        foreach (var target in RegistrationTargets)
        {
            DeleteRegistrySafely(delete, GetTargetMenuKeyPath(target));
        }
    }

    private bool TryRegisterExplorerCommandServer(string exePath)
    {
        try
        {
            if (!(isExplorerCommandAllowed?.Invoke() ?? true))
            {
                AppLogger.Instance.Info("Windows 11 explorer command registration skipped: sparse package identity is not registered.");
                return false;
            }

            var shellExtensionComHostPath = shellExtensionComHostPathResolver(exePath);
            if (string.IsNullOrWhiteSpace(shellExtensionComHostPath))
            {
                AppLogger.Instance.Info("Windows 11 explorer command registration skipped because shell-extension artifacts were not found.");
                return false;
            }

            registerExplorerCommandServer(shellExtensionComHostPath);
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
            Registry.CurrentUser.DeleteSubKeyTree(LegacyMisspelledFileSystemObjectsKeyPath, throwOnMissingSubKey: false);
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
        writeRegistryCommand(keyPath, MenuText, exePath, command, enableExplorerCommand);
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

        AppLogger.Instance.Warn($"{operation} {reason}: {ex.Message}");
        return true;
    }

    private static string? ResolveShellExtensionComHostPath(string exePath)
    {
        var sourceArtifactsDirectory = ResolveShellExtensionArtifactsDirectory(exePath);
        if (string.IsNullOrWhiteSpace(sourceArtifactsDirectory))
        {
            return null;
        }

        var deploymentDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "applanch",
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

        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(fingerprintSource));
        return Convert.ToHexString(hashBytes[..8]);
    }

    private static void RegisterExplorerCommandServer(string shellExtensionComHostPath)
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

    private static void WriteRegistryCommand(string keyPath, string menuText, string iconPath, string command, bool enableExplorerCommand)
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

