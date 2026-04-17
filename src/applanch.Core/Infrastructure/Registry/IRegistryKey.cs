using Microsoft.Win32;

namespace applanch.Infrastructure.Registry;

/// <summary>
/// Represents an opened registry key without leaking <see cref="RegistryKey"/> to callers.
/// </summary>
public interface IRegistryKey : IDisposable
{
    /// <summary>
    /// Gets a value from this registry key.
    /// </summary>
    object? GetValue(string name);

    /// <summary>
    /// Sets a value in this registry key.
    /// </summary>
    void SetValue(string name, object value, RegistryValueKind valueKind);

    /// <summary>
    /// Deletes a value from this registry key.
    /// </summary>
    void DeleteValue(string name, bool throwOnMissingValue);

    /// <summary>
    /// Opens a child subkey under this key.
    /// </summary>
    IRegistryKey? OpenSubKey(string keyPath, bool writable);

    /// <summary>
    /// Creates a child subkey under this key.
    /// </summary>
    IRegistryKey? CreateSubKey(string keyPath, bool writable);

    /// <summary>
    /// Deletes a subkey tree under this key.
    /// </summary>
    void DeleteSubKeyTree(string keyPath, bool throwOnMissingSubKey);
}