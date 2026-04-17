using Microsoft.Win32;

namespace applanch.Infrastructure.Registry;

internal sealed class RegistryKeyRuntime(RegistryKey key, bool ownsKey = false) : IRegistryKey
{
    public static implicit operator RegistryKeyRuntime(RegistryKey key)
        => new(key, ownsKey: false);

    public object? GetValue(string name) => key.GetValue(name);

    public void SetValue(string name, object value, RegistryValueKind valueKind)
        => key.SetValue(name, value, valueKind);

    public void DeleteValue(string name, bool throwOnMissingValue)
        => key.DeleteValue(name, throwOnMissingValue);

    public IRegistryKey? OpenSubKey(string keyPath, bool writable)
    {
        var subKey = key.OpenSubKey(keyPath, writable);
        return subKey is null ? null : new RegistryKeyRuntime(subKey, true);
    }

    public IRegistryKey? CreateSubKey(string keyPath, bool writable)
    {
        var subKey = key.CreateSubKey(keyPath, writable);
        return subKey is null ? null : new RegistryKeyRuntime(subKey, true);
    }

    public void DeleteSubKeyTree(string keyPath, bool throwOnMissingSubKey)
        => key.DeleteSubKeyTree(keyPath, throwOnMissingSubKey);

    public void Dispose()
    {
        if (ownsKey)
        {
            key.Dispose();
        }
    }
}