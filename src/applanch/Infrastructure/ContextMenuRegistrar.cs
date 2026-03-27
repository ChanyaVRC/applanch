using Microsoft.Win32;
using System.IO;
using System.Security;

namespace applanch;

internal sealed class ContextMenuRegistrar(Func<string?> executablePathProvider, Action<string, string, string, string> writeRegistryCommand)
{
    private const string BasePath = @"Software\Classes";
    private const string MenuKeyName = "applanch.register";
    private const string MenuText = "Applanch に登録";
    private static readonly RegistrationTarget[] RegistrationTargets =
    [
        new("*", "%1"),
        new("exefile", "%1"),
        new("Directory", "%1"),
        new("Directory\\Background", "%V")
    ];

    public ContextMenuRegistrar()
        : this(static () => Environment.ProcessPath, WriteRegistryCommand)
    {
    }

    public void EnsureRegistered()
    {
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

    private void RegisterTargetSafely(string exePath, RegistrationTarget target)
    {
        try
        {
            RegisterTarget(exePath, target);
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort registration only.
        }
        catch (SecurityException)
        {
            // Best-effort registration only.
        }
        catch (IOException)
        {
            // Best-effort registration only.
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
