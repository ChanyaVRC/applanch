using System.IO;
using System.Text;
using System.Text.Json;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Theming;

internal static class ThemePaletteConfigurationLoader
{
    internal const string SystemThemeId = "system";
    internal const string LightThemeId = "light";
    internal const string DarkThemeId = "dark";

    private const string UserDefinedDirectoryName = "UserDefined";
    private const string UserDefinedThemePaletteDirectoryName = "theme-palette";
    private static readonly ThemePaletteConfiguration FallbackConfiguration = new(
        Themes:
        [
            new FixedThemeDefinition(LightThemeId, ResolveDisplayName(LightThemeId)),
            new FixedThemeDefinition(DarkThemeId, ResolveDisplayName(DarkThemeId)),
            new SystemDependentThemeDefinition(
                SystemThemeId,
                ResolveDisplayName(SystemThemeId),
                new Dictionary<SystemThemeMode, string>
                {
                    [SystemThemeMode.Light] = LightThemeId,
                    [SystemThemeMode.Dark] = DarkThemeId,
                })
        ],
        Entries:
        [
            FallbackEntry("Brush.AppBackground", "#F1F5F9", "#0B1220"),
            FallbackEntry("Brush.Surface", "#FFFFFF", "#131D31"),
            FallbackEntry("Brush.SurfaceBorder", "#D0D7E2", "#223149"),
            FallbackEntry("Brush.TextPrimary", "#0F172A", "#E2E8F0"),
            FallbackEntry("Brush.TextSecondary", "#475569", "#9FB2C9"),
            FallbackEntry("Brush.TextTertiary", "#64748B", "#7C93AF"),
            FallbackEntry("Brush.ItemBackground", "#F8FAFC", "#111C30"),
            FallbackEntry("Brush.ItemBorder", "#D7DEE8", "#2A3B57"),
            FallbackEntry("Brush.IconBackground", "#E2E8F0", "#20304B"),
            FallbackEntry("Brush.NotificationInfoBackground", "#FFFFFF", "#131D31"),
            FallbackEntry("Brush.NotificationInfoBorder", "#D7DEE8", "#2A3B57"),
            FallbackEntry("Brush.NotificationWarningBackground", "#FFF7ED", "#2B2111"),
            FallbackEntry("Brush.NotificationWarningBorder", "#FDBA74", "#B45309"),
            FallbackEntry("Brush.NotificationErrorBackground", "#FEF2F2", "#2A1618"),
            FallbackEntry("Brush.NotificationErrorBorder", "#FCA5A5", "#B45353"),
            FallbackEntry("Brush.NotificationProgressTrack", "#E2E8F0", "#2A3B57"),
            FallbackEntry("Brush.NotificationProgressValue", "#94A3B8", "#7C93AF"),
            FallbackEntry("Brush.QuickAddInfoText", "#B45309", "#FBBF24"),
            FallbackEntry("Brush.QuickAddWarningText", "#92400E", "#F59E0B")
        ],
        LoadedFromConfig: false);

    internal static ThemePaletteConfiguration LoadForRuntime()
    {
        var builtIn = TryLoadFromDirectory(AppContext.BaseDirectory, out var configuration)
            ? configuration
            : FallbackConfiguration;
        return TryLoadUserDefined(AppContext.BaseDirectory, out var userDefined)
            ? Merge(builtIn, userDefined)
            : builtIn;
    }

    internal static bool TryLoadForSettings(out ThemePaletteConfiguration configuration)
    {
        if (!TryLoadFromDirectory(AppContext.BaseDirectory, out var builtIn))
        {
            configuration = FallbackConfiguration;
            return false;
        }

        configuration = TryLoadUserDefined(AppContext.BaseDirectory, out var userDefined)
            ? Merge(builtIn, userDefined)
            : builtIn;
        return true;
    }

    internal static bool TryLoadUserDefined(string appBaseDirectory, out ThemePaletteConfiguration configuration)
    {
        var userDefinedDirectory = Path.Combine(
            appBaseDirectory,
            "Config",
            UserDefinedDirectoryName,
            UserDefinedThemePaletteDirectoryName);

        if (!Directory.Exists(userDefinedDirectory))
        {
            configuration = FallbackConfiguration;
            return false;
        }

        ThemePaletteConfiguration? merged = null;

        foreach (var path in Directory
                     .EnumerateFiles(userDefinedDirectory, "*.json", SearchOption.TopDirectoryOnly)
                     .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase))
        {
            if (!TryParseFile(path, out var parsed))
            {
                continue;
            }

            merged = merged is null
                ? parsed
                : Merge(merged, parsed);
        }

        if (merged is null)
        {
            configuration = FallbackConfiguration;
            return false;
        }

        configuration = merged;
        return true;
    }

    internal static ThemePaletteConfiguration Merge(ThemePaletteConfiguration @base, ThemePaletteConfiguration overlay)
    {
        var mergedThemes = @base.Themes.ToList();
        foreach (var theme in overlay.Themes)
        {
            var existingIndex = mergedThemes.FindIndex(x => string.Equals(x.Id, theme.Id, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                mergedThemes[existingIndex] = theme;
            }
            else
            {
                mergedThemes.Add(theme);
            }
        }

        var entriesByKey = @base.Entries.ToDictionary(
            static e => e.Key,
            static e => new Dictionary<string, string>(e.ColorsByThemeId, StringComparer.OrdinalIgnoreCase),
            StringComparer.Ordinal);

        foreach (var entry in overlay.Entries)
        {
            if (!entriesByKey.TryGetValue(entry.Key, out var colors))
            {
                colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                entriesByKey[entry.Key] = colors;
            }

            foreach (var (themeId, hex) in entry.ColorsByThemeId)
            {
                colors[themeId] = hex;
            }
        }

        var mergedEntries = entriesByKey
            .Select(static kvp => new ThemePaletteEntry(kvp.Key, kvp.Value))
            .ToArray();

        return new ThemePaletteConfiguration(
            mergedThemes,
            mergedEntries,
            LoadedFromConfig: true);
    }

    internal static bool TryLoadFromDirectory(string appBaseDirectory, out ThemePaletteConfiguration configuration)
    {
        var path = Path.Combine(appBaseDirectory, "Config", "theme-palette.json");
        if (!File.Exists(path))
        {
            AppLogger.Instance.Warn($"Theme palette config not found: {path}");
            configuration = FallbackConfiguration;
            return false;
        }

        return TryParseFile(path, out configuration);
    }

    private static bool TryParseFile(string path, out ThemePaletteConfiguration configuration)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });

            if (!TryParseConfiguration(doc.RootElement, out var parsedConfiguration))
            {
                AppLogger.Instance.Warn($"Theme palette config has no valid entries: {path}");
                configuration = FallbackConfiguration;
                return false;
            }

            configuration = parsedConfiguration;
            AppLogger.Instance.Info($"Loaded theme palette config: {path}");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Failed to load theme palette config '{path}': {ex.Message}");
            configuration = FallbackConfiguration;
            return false;
        }
    }

    private static bool TryParseConfiguration(JsonElement root, out ThemePaletteConfiguration configuration)
    {
        if (!TryParseThemesAndEntries(root, out var parsed))
        {
            configuration = FallbackConfiguration;
            return false;
        }

        configuration = new ThemePaletteConfiguration(
            parsed.Themes,
            parsed.Entries,
            LoadedFromConfig: true);
        return true;
    }

    private static bool TryParseThemesAndEntries(
        JsonElement root,
        out ParsedThemePalette parsed)
    {
        parsed = new ParsedThemePalette([], []);

        if (!root.TryGetProperty("themes", out var themesNode) || themesNode.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var estimatedThemeCount = themesNode.GetArrayLength();
        var parsedThemes = new List<ThemeDefinition>(estimatedThemeCount);
        var colorsByEntryKey = new Dictionary<string, Dictionary<string, string>>(estimatedThemeCount, StringComparer.Ordinal);

        foreach (var themeNode in themesNode.EnumerateArray())
        {
            if (!TryGetStringProperty(themeNode, "id", out var rawThemeId))
            {
                continue;
            }

            var themeId = NormalizeThemeId(rawThemeId);
            if (string.IsNullOrWhiteSpace(themeId))
            {
                continue;
            }

            parsedThemes.Add(ParseThemeDefinition(themeNode, themeId));

            if (!themeNode.TryGetProperty("entries", out var entriesNode) || entriesNode.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entryNode in entriesNode.EnumerateArray())
            {
                if (!TryGetStringProperty(entryNode, "key", out var key) || string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (!TryGetStringProperty(entryNode, "hex", out var hex) || string.IsNullOrWhiteSpace(hex))
                {
                    continue;
                }

                AddColorEntry(colorsByEntryKey, key, themeId, hex);
            }
        }

        if (parsedThemes.Count == 0 || colorsByEntryKey.Count == 0)
        {
            return false;
        }

        parsed = new ParsedThemePalette(
            parsedThemes.ToArray(),
            colorsByEntryKey.Select(static x => new ThemePaletteEntry(x.Key, x.Value)).ToArray());

        return true;
    }

    private static ThemeDefinition ParseThemeDefinition(JsonElement themeNode, string themeId)
    {
        var displayName = ResolveDisplayName(themeId, ParseDisplayNames(themeNode));

        if (!themeNode.TryGetProperty("entriesFrom", out var entriesFromNode))
        {
            return new FixedThemeDefinition(themeId, displayName);
        }

        if (entriesFromNode.ValueKind == JsonValueKind.String)
        {
            var sourceThemeId = NormalizeThemeId(entriesFromNode.GetString());
            return string.IsNullOrWhiteSpace(sourceThemeId)
                ? new FixedThemeDefinition(themeId, displayName)
                : new FixedThemeDefinition(themeId, displayName, sourceThemeId);
        }

        if (entriesFromNode.ValueKind == JsonValueKind.Object)
        {
            var sourcesByMode = ParseSystemDependentSourcesByMode(entriesFromNode);
            return sourcesByMode is null
                ? new FixedThemeDefinition(themeId, displayName)
                : new SystemDependentThemeDefinition(themeId, displayName, sourcesByMode);
        }

        return new FixedThemeDefinition(themeId, displayName);
    }

    private static Dictionary<SystemThemeMode, string>? ParseSystemDependentSourcesByMode(JsonElement entriesFromNode)
    {
        var sources = new Dictionary<SystemThemeMode, string>();

        foreach (var property in entriesFromNode.EnumerateObject())
        {
            var mode = NormalizeThemeId(property.Name);
            var systemThemeMode = mode switch
            {
                LightThemeId => SystemThemeMode.Light,
                DarkThemeId => SystemThemeMode.Dark,
                _ => (SystemThemeMode?)null,
            };
            if (systemThemeMode is null)
            {
                continue;
            }

            if (property.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var sourceThemeId = NormalizeThemeId(property.Value.GetString());
            if (string.IsNullOrWhiteSpace(sourceThemeId))
            {
                continue;
            }

            sources[systemThemeMode.Value] = sourceThemeId;
        }

        return sources.Count == 0 ? null : sources;
    }

    private static void AddColorEntry(
        Dictionary<string, Dictionary<string, string>> byKey,
        string entryKey,
        string themeId,
        string hex)
    {
        if (!byKey.TryGetValue(entryKey, out var colors))
        {
            colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            byKey[entryKey] = colors;
        }

        colors[themeId] = hex;
    }

    private static ThemePaletteEntry FallbackEntry(string key, string lightHex, string darkHex)
    {
        return new ThemePaletteEntry(
            key,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [LightThemeId] = lightHex,
                [DarkThemeId] = darkHex,
            });
    }

    private static string NormalizeThemeId(string? themeId) =>
        string.IsNullOrWhiteSpace(themeId) ? string.Empty : themeId.Trim().ToLowerInvariant();

    private static bool TryGetStringProperty(JsonElement node, string propertyName, out string value)
    {
        value = string.Empty;
        if (!node.TryGetProperty(propertyName, out var propertyNode) || propertyNode.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var parsed = propertyNode.GetString();
        if (parsed is null)
        {
            return false;
        }

        value = parsed;
        return true;
    }

    private static LocalizedText ResolveDisplayName(
        string themeId,
        IReadOnlyDictionary<LanguageOption, string>? displayNames = null)
    {
        var fallback = string.Equals(themeId, SystemThemeId, StringComparison.OrdinalIgnoreCase)
                ? AppResources.Theme_System
                : string.Equals(themeId, LightThemeId, StringComparison.OrdinalIgnoreCase)
                ? AppResources.Theme_Light
                : string.Equals(themeId, DarkThemeId, StringComparison.OrdinalIgnoreCase)
                    ? AppResources.Theme_Dark
                    : ToTitleCase(themeId);

        return new LocalizedText(fallback, displayNames);
    }

    private static Dictionary<LanguageOption, string> ParseDisplayNames(JsonElement themeNode)
    {
        var displayNames = new Dictionary<LanguageOption, string>();
        if (!themeNode.TryGetProperty("displayNames", out var displayNamesNode) || displayNamesNode.ValueKind != JsonValueKind.Object)
        {
            return displayNames;
        }

        foreach (var property in displayNamesNode.EnumerateObject())
        {
            var value = property.Value.GetString();
            if (value is null)
            {
                continue;
            }

            if (LanguageOptionMap.TryMapFromCultureCode(property.Name, out var language))
            {
                displayNames[language] = value;
            }
        }

        return displayNames;
    }

    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        var makeUpper = true;

        foreach (var c in value)
        {
            if (c is '-' or '_' or ' ')
            {
                if (builder.Length > 0 && builder[^1] != ' ')
                {
                    builder.Append(' ');
                }

                makeUpper = true;
                continue;
            }

            builder.Append(makeUpper ? char.ToUpperInvariant(c) : c);
            makeUpper = false;
        }

        var result = builder.ToString().Trim();
        return result.Length == 0 ? value : result;
    }
}
