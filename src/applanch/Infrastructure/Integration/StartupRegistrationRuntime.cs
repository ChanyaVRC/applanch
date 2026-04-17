using Microsoft.Win32;

namespace applanch.Infrastructure.Integration;

internal sealed class StartupRegistrationRuntime : IStartupRegistrationRuntime
{
    private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

    public IStartupRunKey? OpenRunKey()
    {
        var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        return key is null ? null : new RegistryStartupRunKey(key);
    }
}
