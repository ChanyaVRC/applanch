using Microsoft.Win32;

namespace applanch.Infrastructure.Registry;

/// <summary>
/// Abstracts Windows Registry key operations for improved testability and decoupling.
/// </summary>
public interface IRegistryRuntime
{
    /// <summary>
    /// Opens a registry subkey.
    /// </summary>
    IRegistryKey? OpenSubKey(IRegistryKey rootKey, string keyPath, bool writable);

    /// <summary>
    /// Opens a registry subkey.
    /// </summary>
    IRegistryKey? OpenSubKey(RegistryKey rootKey, string keyPath, bool writable)
        => OpenSubKey((RegistryKeyRuntime)rootKey, keyPath, writable);

    /// <summary>
    /// Creates a registry subkey.
    /// </summary>
    IRegistryKey? CreateSubKey(IRegistryKey rootKey, string keyPath, bool writable);

    /// <summary>
    /// Creates a registry subkey.
    /// </summary>
    IRegistryKey? CreateSubKey(RegistryKey rootKey, string keyPath, bool writable)
        => CreateSubKey((RegistryKeyRuntime)rootKey, keyPath, writable);

    /// <summary>
    /// Deletes a registry subkey tree.
    /// </summary>
    void DeleteSubKeyTree(IRegistryKey rootKey, string keyPath, bool throwOnMissingSubKey);

    /// <summary>
    /// Deletes a registry subkey tree.
    /// </summary>
    void DeleteSubKeyTree(RegistryKey rootKey, string keyPath, bool throwOnMissingSubKey)
        => DeleteSubKeyTree((RegistryKeyRuntime)rootKey, keyPath, throwOnMissingSubKey);
}
