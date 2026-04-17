using Microsoft.Win32;

namespace applanch.Infrastructure.Integration;

internal sealed class RegistryStartupRunKey(RegistryKey key) : IStartupRunKey
{
    public object? GetValue(string name) => key.GetValue(name);

    public void SetValue(string name, object value, RegistryValueKind valueKind)
        => key.SetValue(name, value, valueKind);

    public void DeleteValue(string name, bool throwOnMissingValue)
        => key.DeleteValue(name, throwOnMissingValue);

    public void Dispose() => key.Dispose();
}
