using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using applanch.Properties;
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

        if (!TryNormalizePersistablePath(path, out var normalizedPath))
        {
            return;
        }

        var existing = LoadAll().ToList();
        var seenPaths = new HashSet<string>(existing.Select(static x => x.Path), StringComparer.OrdinalIgnoreCase);
        if (!seenPaths.Add(normalizedPath))
        {
            return;
        }

        existing.Add(new LauncherEntry(
            normalizedPath,
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
            if (!TryNormalizeEntry(entry, out var normalizedEntry))
            {
                continue;
            }

            if (!seenPaths.Add(normalizedEntry.Path))
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

        if (entry.IsNormalized)
        {
            normalizedEntry = entry;
            return true;
        }

        if (!TryNormalizePersistablePath(entry.Path, out var normalizedPath))
        {
            return false;
        }

        var normalizedCategory = LaunchItemNormalization.NormalizeCategory(entry.Category);
        var normalizedArguments = LaunchItemNormalization.NormalizeArguments(entry.Arguments);
        var normalizedDisplayName = LaunchItemNormalization.NormalizeDisplayName(entry.DisplayName, normalizedPath);

        normalizedEntry = entry with
        {
            Path = normalizedPath,
            Category = normalizedCategory,
            Arguments = normalizedArguments,
            DisplayName = normalizedDisplayName,
            IsNormalized = true
        };

        return true;
    }

    private static bool TryNormalizePersistablePath(string path, out string normalizedPath)
    {
        normalizedPath = string.Empty;

        var trimmedPathSpan = path.AsSpan().Trim();
        if (trimmedPathSpan.IsEmpty)
        {
            return false;
        }

        ReadOnlySpan<char> candidatePath = trimmedPathSpan;
        string? driveRootPath = null;
        if (IsDriveLetterSpecifier(trimmedPathSpan))
        {
            driveRootPath = string.Concat(trimmedPathSpan.ToString(), Path.DirectorySeparatorChar);
            candidatePath = driveRootPath.AsSpan();
        }

        if (!Path.IsPathFullyQualified(candidatePath))
        {
            return false;
        }

        normalizedPath = NormalizePathCore(candidatePath);
        return !string.IsNullOrWhiteSpace(normalizedPath);
    }

    private static string NormalizePathCore(ReadOnlySpan<char> path)
    {
        var trimmedPathSpan = path.Trim();
        if (trimmedPathSpan.IsEmpty)
        {
            return string.Empty;
        }

        var trimmedPath = trimmedPathSpan.ToString();
        try
        {
            var full = Path.GetFullPath(trimmedPath);
            return Directory.Exists(full)
                ? EnsureTrailingDirectorySeparator(full)
                : Path.TrimEndingDirectorySeparator(full);
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Path normalization failed for '{trimmedPath}': {ex.Message}");
            return trimmedPath;
        }
    }

    private static bool IsDriveLetterSpecifier(ReadOnlySpan<char> path) =>
        path.Length == 2 && char.IsLetter(path[0]) && path[1] == ':';

    private static string EnsureTrailingDirectorySeparator(string path) =>
        Path.EndsInDirectorySeparator(path)
            ? path
            : path + Path.DirectorySeparatorChar;

    internal sealed record LauncherEntry(string Path, string Category, string Arguments, string DisplayName)
    {
        public static string DefaultCategory => Resources.DefaultCategory;

        [JsonIgnore]
        public bool IsNormalized { get; init; }
    }
}

