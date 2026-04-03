using Microsoft.Win32;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Launch.AppIdResolvers;

/// <summary>
/// Resolves app IDs from Windows Registry values.
/// Configuration format: "registry:{hive}:{keyPath}:{valueName}"
/// Example: "registry:HKEY_LOCAL_MACHINE:SOFTWARE\\Wow6432Node\\Epic Games\\EpicGamesLauncher:AppDataPath"
/// </summary>
internal sealed class RegistryAppIdResolver : IAppIdResolver
{
    private readonly string _source;

    internal RegistryAppIdResolver(string source)
    {
        _source = source;
    }

    public bool TryResolve(LaunchPath launchPath, out string appId)
    {
        appId = string.Empty;

        if (string.IsNullOrWhiteSpace(_source))
        {
            return false;
        }

        // Parse format: registry:HIVE:KeyPath:ValueName
        Span<Range> parts = stackalloc Range[4];
        var sourceSpan = _source.AsSpan();
        if (sourceSpan.Split(parts, ':') != 4 ||
            !sourceSpan[parts[0]].Equals("registry", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var hive = sourceSpan[parts[1]].Trim().ToString();
        var keyPath = sourceSpan[parts[2]].Trim().ToString();
        var valueName = sourceSpan[parts[3]].Trim().ToString();

        RegistryHive registryHive = hive.ToUpperInvariant() switch
        {
            "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKEY_USERS" => RegistryHive.Users,
            "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
            _ => (RegistryHive)(-1),
        };

        if ((int)registryHive < 0)
        {
            return false;
        }

        try
        {
            using (var key = RegistryKey.OpenBaseKey(registryHive, RegistryView.Registry64))
            using (var subKey = key.OpenSubKey(keyPath, writable: false))
            {
                if (subKey is null)
                {
                    return false;
                }

                var value = subKey.GetValue(valueName);
                if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                {
                    appId = stringValue;
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
