using Microsoft.Win32;

namespace applanch.Infrastructure.Integration;

internal sealed class StartupRegistrationService
{
    private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string EntryName = "applanch";

    public void Apply(bool enabled, string executablePath)
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (runKey is null)
        {
            return;
        }

        if (enabled)
        {
            runKey.SetValue(EntryName, Quote(executablePath), RegistryValueKind.String);
            return;
        }

        if (runKey.GetValue(EntryName) is not null)
        {
            runKey.DeleteValue(EntryName, throwOnMissingValue: false);
        }
    }

    private static string Quote(string value) => $"\"{value}\"";
}
