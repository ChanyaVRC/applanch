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
    private readonly record struct ParsedSource(RegistryHive Hive, string HiveName, string KeyPath, string ValueName);

    private readonly string _source;

    internal RegistryAppIdResolver(string source)
    {
        _source = source;
    }

    public bool CanResolve(LaunchPath launchPath)
    {
        return ParseSource() is not null;
    }

    public string Resolve(LaunchPath launchPath)
    {
        var parsedCandidate = ParseSource();
        if (parsedCandidate is null)
        {
            throw new AppIdResolutionException($"Invalid registry app-id source '{_source}'.");
        }

        var parsed = parsedCandidate.Value;

        try
        {
            using (var key = RegistryKey.OpenBaseKey(parsed.Hive, RegistryView.Registry64))
            using (var subKey = key.OpenSubKey(parsed.KeyPath, writable: false))
            {
                if (subKey is null)
                {
                    throw new AppIdResolutionException($"Registry key '{parsed.KeyPath}' was not found in hive '{parsed.HiveName}'.");
                }

                var value = subKey.GetValue(parsed.ValueName);
                if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                {
                    return stringValue;
                }

                throw new AppIdResolutionException($"Registry value '{parsed.ValueName}' in key '{parsed.KeyPath}' is missing or empty.");
            }
        }
        catch (AppIdResolutionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AppIdResolutionException($"Failed to resolve app ID from registry source '{_source}'.", ex);
        }
    }

    private ParsedSource? ParseSource()
    {
        if (string.IsNullOrWhiteSpace(_source))
        {
            return null;
        }

        var parts = _source.Split(':', 4, StringSplitOptions.TrimEntries);
        if (parts.Length != 4 || !string.Equals(parts[0], "registry", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var hiveName = parts[1];
        var keyPath = parts[2];
        var valueName = parts[3];

        if (string.IsNullOrWhiteSpace(keyPath) || string.IsNullOrWhiteSpace(valueName))
        {
            return null;
        }

        if (!TryParseRegistryHive(hiveName, out var hive))
        {
            return null;
        }

        return new ParsedSource(hive, hiveName, keyPath, valueName);
    }

    private static bool TryParseRegistryHive(string hive, out RegistryHive registryHive)
    {
        registryHive = hive.ToUpperInvariant() switch
        {
            "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKEY_USERS" => RegistryHive.Users,
            "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
            _ => (RegistryHive)(-1),
        };

        return (int)registryHive >= 0;
    }
}
