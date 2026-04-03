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
    Action<string>? deleteRegistrySubKeyTree = null)
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
        : this(static () => Environment.ProcessPath, ResolveShellExtensionComHostPath, WriteRegistryCommand, RegisterExplorerCommandServer, enableLegacyCleanup: true)
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

        DeleteRegistrySafely(delete, $@"{BasePath}\CLSID\{{{ExplorerCommandIds.ClassId}}}");
        DeleteRegistrySafely(delete, $@"{BasePath}\{ExplorerCommandIds.ProgId}");

        foreach (var target in RegistrationTargets)
        {
            DeleteRegistrySafely(delete, $@"{BasePath}\{target.ClassKeyPath}\shell\{MenuKeyName}");
        }
    }

    private bool TryRegisterExplorerCommandServer(string exePath)
    {
        try
        {
            var shellExtensionComHostPath = shellExtensionComHostPathResolver(exePath);
            if (string.IsNullOrWhiteSpace(shellExtensionComHostPath))
            {
                AppLogger.Instance.Info("Windows 11 explorer command registration skipped because shell-extension artifacts were not found.");
                return false;
            }

            registerExplorerCommandServer(shellExtensionComHostPath);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLogger.Instance.Warn($"Explorer command registration denied: {ex.Message}");
        }
        catch (SecurityException ex)
        {
            AppLogger.Instance.Warn($"Explorer command registration security error: {ex.Message}");
        }
        catch (IOException ex)
        {
            AppLogger.Instance.Warn($"Explorer command registration I/O error: {ex.Message}");
        }

        return false;
    }

    private void CleanupLegacyRegistrationSafely()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(LegacyMisspelledFileSystemObjectsKeyPath, throwOnMissingSubKey: false);
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLogger.Instance.Warn($"Registry cleanup denied for legacy context menu key: {ex.Message}");
        }
        catch (SecurityException ex)
        {
            AppLogger.Instance.Warn($"Registry cleanup security error for legacy context menu key: {ex.Message}");
        }
        catch (IOException ex)
        {
            AppLogger.Instance.Warn($"Registry cleanup I/O error for legacy context menu key: {ex.Message}");
        }
    }

    private void RegisterTargetSafely(string exePath, RegistrationTarget target, bool enableExplorerCommand)
    {
        try
        {
            RegisterTarget(exePath, target, enableExplorerCommand);
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLogger.Instance.Warn($"Registry registration denied for {target.ClassKeyPath}: {ex.Message}");
        }
        catch (SecurityException ex)
        {
            AppLogger.Instance.Warn($"Registry registration security error for {target.ClassKeyPath}: {ex.Message}");
        }
        catch (IOException ex)
        {
            AppLogger.Instance.Warn($"Registry registration I/O error for {target.ClassKeyPath}: {ex.Message}");
        }
    }

    private static void DeleteRegistrySafely(Action<string> delete, string keyPath)
    {
        try
        {
            delete(keyPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLogger.Instance.Warn($"Registry deletion denied for {keyPath}: {ex.Message}");
        }
        catch (SecurityException ex)
        {
            AppLogger.Instance.Warn($"Registry deletion security error for {keyPath}: {ex.Message}");
        }
        catch (IOException ex)
        {
            AppLogger.Instance.Warn($"Registry deletion I/O error for {keyPath}: {ex.Message}");
        }
    }

    private void RegisterTarget(string exePath, RegistrationTarget target, bool enableExplorerCommand)
    {
        var keyPath = $"{BasePath}\\{target.ClassKeyPath}\\shell\\{MenuKeyName}";
        var command = $"\"{exePath}\" {App.RegisterArgument} \"{target.ArgumentToken}\"";
        writeRegistryCommand(keyPath, MenuText, exePath, command, enableExplorerCommand);
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
        var classKeyPath = $"{BasePath}\\CLSID\\{{{ExplorerCommandIds.ClassId}}}";
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

        using var progIdKey = Registry.CurrentUser.CreateSubKey($"{BasePath}\\{ExplorerCommandIds.ProgId}");
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

