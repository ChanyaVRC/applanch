using System.IO;
using System.Text.Json;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Storage;

internal static class LauncherStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly string StoreDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "applanch");

    private static readonly string StoreFilePath = Path.Combine(StoreDirectory, "launch-items.json");
    private static readonly string LegacyStoreFilePath = Path.Combine(StoreDirectory, "launch-items.txt");

    public static void EnsureStorageDirectory() => Directory.CreateDirectory(StoreDirectory);

    public static IReadOnlyList<LauncherEntry> LoadAll()
    {
        EnsureStorageDirectory();

        if (!File.Exists(StoreFilePath))
        {
            return LoadLegacyEntries();
        }

        return TryLoadFromJson(out var entries)
            ? entries
            : LoadLegacyEntries();
    }

    private static bool TryLoadFromJson(out IReadOnlyList<LauncherEntry> entries)
    {
        try
        {
            var json = File.ReadAllText(StoreFilePath);
            var parsedEntries = JsonSerializer.Deserialize<List<LauncherEntry>>(json, JsonOptions) ?? [];
            entries = NormalizeEntries(parsedEntries);
            return true;
        }
        catch (JsonException ex)
        {
            AppLogger.Instance.Error(ex, "Failed to load launch items from JSON");
            TryQuarantineCorruptedStoreFile();
            entries = [];
            return false;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, "Failed to load launch items from JSON");
            entries = [];
            return false;
        }
    }

    private static void TryQuarantineCorruptedStoreFile()
    {
        try
        {
            if (!File.Exists(StoreFilePath))
            {
                return;
            }

            var quarantinedPath = $"{StoreFilePath}.bad.{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            File.Move(StoreFilePath, quarantinedPath);
            AppLogger.Instance.Warn($"Quarantined corrupted launch items file: '{StoreFilePath}' -> '{quarantinedPath}'");
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Failed to quarantine corrupted launch items file '{StoreFilePath}': {ex.Message}");
        }
    }

    public static void Add(string path)
    {
        Add(path, LauncherEntry.DefaultCategory, string.Empty, null);
    }

    public static void Add(string path, string category, string arguments, string? displayName)
    {
        EnsureStorageDirectory();

        if (!TryNormalizePersistablePath(path, out var normalizedPath))
        {
            return;
        }

        var existing = LoadAll().ToList();
        var seenPaths = new HashSet<string>(existing.Select(static x => x.Path.Value), StringComparer.OrdinalIgnoreCase);
        if (!seenPaths.Add(normalizedPath))
        {
            return;
        }

        existing.Add(new LauncherEntry(
            new LaunchPath(normalizedPath),
            LaunchItemNormalization.NormalizeCategory(category),
            LaunchItemNormalization.NormalizeArguments(arguments),
            LaunchItemNormalization.NormalizeDisplayName(displayName, normalizedPath))
        {
            IsNormalized = true
        });

        SaveAll(existing);
    }

    public static void SaveAll(IEnumerable<LauncherEntry> entries)
    {
        EnsureStorageDirectory();

        var json = JsonSerializer.Serialize(NormalizeEntries(entries), JsonOptions);
        File.WriteAllText(StoreFilePath, json);
    }

    private static List<LauncherEntry> LoadLegacyEntries()
    {
        if (!File.Exists(LegacyStoreFilePath))
        {
            return [];
        }

        var entries = File.ReadAllLines(LegacyStoreFilePath)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Select(static path => new LauncherEntry(path, LauncherEntry.DefaultCategory, string.Empty, Path.GetFileName(path)))
            .ToList();

        var normalized = NormalizeEntries(entries);
        SaveAll(normalized);
        return normalized;
    }

    private static List<LauncherEntry> NormalizeEntries(IEnumerable<LauncherEntry> entries)
    {
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<LauncherEntry>();

        foreach (var entry in entries)
        {
            if (!TryNormalizeEntry(entry, out var normalizedEntry))
            {
                continue;
            }

            if (!seenPaths.Add(normalizedEntry.Path.Value))
            {
                continue;
            }

            result.Add(normalizedEntry);
        }

        return result;
    }

    private static bool TryNormalizeEntry(LauncherEntry entry, out LauncherEntry normalizedEntry)
    {
        normalizedEntry = default!;

        if (string.IsNullOrWhiteSpace(entry.Path.Value))
        {
            return false;
        }

        if (entry.IsNormalized)
        {
            normalizedEntry = entry;
            return true;
        }

        if (!TryNormalizePersistablePath(entry.Path.Value, out var normalizedPath))
        {
            return false;
        }

        var normalizedCategory = LaunchItemNormalization.NormalizeCategory(entry.Category);
        var normalizedArguments = LaunchItemNormalization.NormalizeArguments(entry.Arguments);
        var normalizedDisplayName = LaunchItemNormalization.NormalizeDisplayName(entry.DisplayName, normalizedPath);

        normalizedEntry = entry with
        {
            Path = new LaunchPath(normalizedPath),
            Category = normalizedCategory,
            Arguments = normalizedArguments,
            DisplayName = normalizedDisplayName,
            IsNormalized = true
        };

        return true;
    }

    private static bool TryNormalizePersistablePath(string path, out string normalizedPath)
    {
        return PathNormalization.TryNormalizePersistablePath(path, out normalizedPath);
    }
}

