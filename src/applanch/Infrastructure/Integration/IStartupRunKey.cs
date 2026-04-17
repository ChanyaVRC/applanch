using Microsoft.Win32;

namespace applanch.Infrastructure.Integration;

internal interface IStartupRunKey : IDisposable
{
    object? GetValue(string name);

    void SetValue(string name, object value, RegistryValueKind valueKind);

    void DeleteValue(string name, bool throwOnMissingValue);
}
