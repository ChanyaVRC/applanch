using Microsoft.Win32;
using applanch.Core.Utilities;

namespace applanch.Infrastructure.Launch.AppIdResolvers;

/// <summary>
/// Resolves app IDs from Windows Registry values.
/// Configuration format: "registry:{hive}:{keyPath}:{valueName}"
/// Example: "registry:HKEY_LOCAL_MACHINE:SOFTWARE\\Wow6432Node\\Epic Games\\EpicGamesLauncher:AppDataPath"
/// </summary>
[AppIdSourcePrefix("registry")]
internal sealed class RegistryAppIdResolver : IAppIdResolver
{
    private readonly record struct ParsedSource(RegistryHive Hive, string HiveName, string KeyPath, string ValueName);

    private readonly string _source;

    internal RegistryAppIdResolver(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    public bool CanResolve(LaunchPath launchPath)
    {
        return ParseSource() is not null;
    }

    public string Resolve(LaunchPath launchPath)
    {
        var parsed = ParseSource() ?? throw new AppIdResolutionException($"Invalid registry app-id source '{_source}'.");

        try
        {
            using var key = RegistryKey.OpenBaseKey(parsed.Hive, RegistryView.Default);
            using var subKey = key.OpenSubKey(parsed.KeyPath, writable: false) ?? throw new AppIdResolutionException($"Registry key '{parsed.KeyPath}' was not found in hive '{parsed.HiveName}'.");

            var value = subKey.GetValue(parsed.ValueName);
            if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
            {
                return stringValue;
            }

            throw new AppIdResolutionException($"Registry value '{parsed.ValueName}' in key '{parsed.KeyPath}' is missing or empty.");
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

        var parts = _source.Split(':', 3, StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
        {
            return null;
        }

        var hiveName = parts[0];
        var keyPath = parts[1];
        var valueName = parts[2];

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
        registryHive = hive switch
        {
            "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKEY_USERS" => RegistryHive.Users,
            "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
            _ => default,
        };
        return registryHive != default;
    }
}