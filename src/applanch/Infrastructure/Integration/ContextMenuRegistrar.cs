using Microsoft.Win32;
using System.IO;
using System.Security;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Integration;

internal sealed class ContextMenuRegistrar(
    Func<string?> executablePathProvider,
    Action<string, string, string, string> writeRegistryCommand,
    bool enableLegacyCleanup = true)
{
    private const string BasePath = @"Software\Classes";
    private const string MenuKeyName = "applanch.register";
    private const string LegacyMisspelledFileSystemObjectsKeyPath = @"Software\Classes\AllFilesystemObjects\shell\applanch.register";
    private static string MenuText => AppResources.ContextMenu_Register;
    private static readonly RegistrationTarget[] RegistrationTargets =
    [
        // Windows 11 aggregates context-menu sources differently; registering
        // the same verb under AllFileSystemObjects improves discoverability.
        new("AllFileSystemObjects", "%1"),
        new("*", "%1"),
        new("exefile", "%1"),
        new("Directory", "%1"),
        new("Directory\\Background", "%V")
    ];

    public ContextMenuRegistrar()
        : this(static () => Environment.ProcessPath, WriteRegistryCommand, enableLegacyCleanup: true)
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

        // Registry writes can fail due to policy or permissions; skip known registry failures per target.
        foreach (var target in RegistrationTargets)
        {
            RegisterTargetSafely(exePath, target);
        }
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

    private void RegisterTargetSafely(string exePath, RegistrationTarget target)
    {
        try
        {
            RegisterTarget(exePath, target);
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

    private void RegisterTarget(string exePath, RegistrationTarget target)
    {
        var keyPath = $"{BasePath}\\{target.ClassKeyPath}\\shell\\{MenuKeyName}";
        var command = $"\"{exePath}\" {App.RegisterArgument} \"{target.ArgumentToken}\"";
        writeRegistryCommand(keyPath, MenuText, exePath, command);
    }

    private static void WriteRegistryCommand(string keyPath, string menuText, string iconPath, string command)
    {
        using var shellKey = Registry.CurrentUser.CreateSubKey(keyPath);
        if (shellKey is null)
        {
            return;
        }

        shellKey.SetValue(string.Empty, menuText);
        shellKey.SetValue("Icon", iconPath);

        using var commandKey = shellKey.CreateSubKey("command");
        commandKey?.SetValue(string.Empty, command);
    }

    private readonly record struct RegistrationTarget(string ClassKeyPath, string ArgumentToken);
}

