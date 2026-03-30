using System.IO;
using System.Text.Json;
using applanch.Properties;

namespace applanch;

internal static class LauncherStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly string StoreDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "applanch");

    private static readonly string StoreFilePath = Path.Combine(StoreDirectory, "launch-items.json");
    private static readonly string LegacyStoreFilePath = Path.Combine(StoreDirectory, "launch-items.txt");

    public static void EnsureStorageDirectory()
    {
        Directory.CreateDirectory(StoreDirectory);
    }

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
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, "Failed to load launch items from JSON");
            entries = [];
            return false;
        }
    }

    public static void Add(string path)
    {
        Add(path, LauncherEntry.DefaultCategory, string.Empty, null);
    }

    public static void Add(string path, string category, string arguments, string? displayName)
    {
        EnsureStorageDirectory();

        var normalizedPath = NormalizePath(path);
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return;
        }

        var existing = LoadAll().ToList();
        if (ContainsPath(existing, normalizedPath))
        {
            return;
        }

        existing.Add(new LauncherEntry(
            normalizedPath,
            LaunchItemNormalization.NormalizeCategory(category),
            LaunchItemNormalization.NormalizeArguments(arguments),
            LaunchItemNormalization.NormalizeDisplayName(displayName, normalizedPath)));

        SaveAll(existing);
    }

    public static void SaveAll(IEnumerable<LauncherEntry> entries)
    {
        EnsureStorageDirectory();

        var json = JsonSerializer.Serialize(NormalizeEntries(entries), JsonOptions);
        File.WriteAllText(StoreFilePath, json);
    }

    private static bool ContainsPath(IEnumerable<LauncherEntry> entries, string path) =>
        entries.Any(item => string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<LauncherEntry> LoadLegacyEntries()
    {
        if (!File.Exists(LegacyStoreFilePath))
        {
            return [];
        }

        var entries = File.ReadAllLines(LegacyStoreFilePath)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Select(static path => new LauncherEntry(path, LauncherEntry.DefaultCategory, string.Empty, Path.GetFileName(path)))
            .ToList();

        var normalized = NormalizeEntries(entries).ToList();
        SaveAll(normalized);
        return normalized;
    }

    private static IReadOnlyList<LauncherEntry> NormalizeEntries(IEnumerable<LauncherEntry> entries)
    {
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<LauncherEntry>();

        foreach (var entry in entries)
        {
            var normalizedPath = NormalizePath(entry.Path);
            if (string.IsNullOrWhiteSpace(normalizedPath) || !seenPaths.Add(normalizedPath))
            {
                continue;
            }

            result.Add(entry with
            {
                Path = normalizedPath,
                Category = LaunchItemNormalization.NormalizeCategory(entry.Category),
                Arguments = LaunchItemNormalization.NormalizeArguments(entry.Arguments),
                DisplayName = LaunchItemNormalization.NormalizeDisplayName(entry.DisplayName, normalizedPath)
            });
        }

        return result;
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            var full = Path.GetFullPath(path.Trim());
            if (full.Length > 3)
            {
                full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            return full;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Path normalization failed for '{path}': {ex.Message}");
            return path.Trim();
        }
    }

    internal sealed record LauncherEntry(string Path, string Category, string Arguments, string DisplayName)
    {
        public static string DefaultCategory => Resources.DefaultCategory;
    }
}
