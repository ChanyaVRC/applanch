namespace applanch.Infrastructure.Registry;

/// <summary>
/// Default implementation of <see cref="IRegistryRuntime"/> that delegates to the Windows Registry API.
/// </summary>
public sealed class RegistryRuntime : IRegistryRuntime
{
    public IRegistryKey CurrentUser => (RegistryKeyRuntime)Microsoft.Win32.Registry.CurrentUser;

    public IRegistryKey? OpenSubKey(IRegistryKey rootKey, string keyPath, bool writable)
        => rootKey.OpenSubKey(keyPath, writable);

    public IRegistryKey? CreateSubKey(IRegistryKey rootKey, string keyPath, bool writable)
        => rootKey.CreateSubKey(keyPath, writable);

    public void DeleteSubKeyTree(IRegistryKey rootKey, string keyPath, bool throwOnMissingSubKey)
        => rootKey.DeleteSubKeyTree(keyPath, throwOnMissingSubKey);
}
