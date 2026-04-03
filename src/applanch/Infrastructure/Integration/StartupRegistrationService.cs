using Microsoft.Win32;

namespace applanch.Infrastructure.Integration;

internal sealed class StartupRegistrationService
{
    private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string EntryName = "applanch";
    private readonly Func<IStartupRunKey?> _openRunKey;

    public StartupRegistrationService()
        : this(OpenRunKey)
    {
    }

    internal StartupRegistrationService(Func<IStartupRunKey?> openRunKey)
    {
        _openRunKey = openRunKey;
    }

    public void Apply(bool enabled, string executablePath)
    {
        using var runKey = _openRunKey();

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

    private static void SetStartupValue(IStartupRunKey runKey, string executablePath)
        => runKey.SetValue(EntryName, Quote(executablePath), RegistryValueKind.String);

    private static void RemoveStartupValue(IStartupRunKey runKey)
    {
        if (runKey.GetValue(EntryName) is null)
        {
            return;
        }

        runKey.DeleteValue(EntryName, throwOnMissingValue: false);
    }

    private static string Quote(string value) => $"\"{value}\"";

    private static IStartupRunKey? OpenRunKey()
    {
        var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        return key is null ? null : new RegistryStartupRunKey(key);
    }

    internal interface IStartupRunKey : IDisposable
    {
        object? GetValue(string name);
        void SetValue(string name, object value, RegistryValueKind valueKind);
        void DeleteValue(string name, bool throwOnMissingValue);
    }

    private sealed class RegistryStartupRunKey(RegistryKey key) : IStartupRunKey
    {
        public object? GetValue(string name) => key.GetValue(name);

        public void SetValue(string name, object value, RegistryValueKind valueKind) =>
            key.SetValue(name, value, valueKind);

        public void DeleteValue(string name, bool throwOnMissingValue) =>
            key.DeleteValue(name, throwOnMissingValue);

        public void Dispose() => key.Dispose();
    }
}
