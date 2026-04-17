using Microsoft.Win32;
using applanch.Infrastructure.Registry;
namespace applanch.Infrastructure.Integration;

internal sealed class StartupRegistrationService
{
    private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string EntryName = "applanch";
    private readonly IRegistryRuntime _registry;

    public StartupRegistrationService()
        : this(new RegistryRuntime())
    {
    }

    internal StartupRegistrationService(IRegistryRuntime registry)
    {
        _registry = registry;
    }

    public void Apply(bool enabled, string executablePath)
    {
        using var runKey = _registry.OpenSubKey(WinRegistry.CurrentUser, RunKeyPath, writable: true)
            ?? _registry.CreateSubKey(WinRegistry.CurrentUser, RunKeyPath, writable: true);

        if (runKey is null)
        {
            return;
        }

        if (enabled)
        {
            SetStartupValue(runKey, executablePath);
            return;
        }

        RemoveStartupValue(runKey);
    }

    private static void SetStartupValue(IRegistryKey runKey, string executablePath)
        => runKey.SetValue(EntryName, Quote(executablePath), RegistryValueKind.String);

    private static void RemoveStartupValue(IRegistryKey runKey)
    {
        if (runKey.GetValue(EntryName) is null)
        {
            return;
        }

        runKey.DeleteValue(EntryName, throwOnMissingValue: false);
    }

    private static string Quote(string value) => $"\"{value}\"";
}
