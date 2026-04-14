using System.Buffers;
using System.IO;
using System.Text.Json;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Theming;

internal static class ThemePaletteConfigurationLoader
{
    private const string ConfigDescription = "theme palette config";
    internal const string SystemThemeId = "system";
    internal const string LightThemeId = "light";
    internal const string DarkThemeId = "dark";

    private const string UserDefinedThemePaletteDirectoryName = "theme-palette";

    private static readonly ThemePaletteConfiguration EmptyConfiguration = new([]);
    private static readonly Lazy<ThemePaletteConfiguration> CachedConfiguration = new(LoadCore);

    internal static ThemePaletteConfiguration Load()
        => CachedConfiguration.Value;

    private static ThemePaletteConfiguration LoadCore()
    {
        if (!TryLoadFromDirectory(AppContext.BaseDirectory, out var bundled))
        {
            throw new InvalidOperationException("Bundled theme palette config could not be loaded.");
        }

        return TryLoadUserDefined(AppContext.BaseDirectory, out var userDefined)
            ? Merge(bundled, userDefined)
            : bundled;
    }

    internal static bool TryLoadUserDefined(string appBaseDirectory, out ThemePaletteConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(appBaseDirectory);

        var userDefinedDirectory = ConfigJsonPathResolver.GetUserDefinedDirectory(appBaseDirectory, UserDefinedThemePaletteDirectoryName);

        if (!Directory.Exists(userDefinedDirectory))
        {
            configuration = EmptyConfiguration;
            return false;
        }

        ThemePaletteConfiguration? merged = null;

        ConfigJsonLoadHelper.LoadAndMerge(
            ConfigJsonPathResolver
                .EnumerateUserDefinedJsonPaths(appBaseDirectory, UserDefinedThemePaletteDirectoryName)
                .Select(static path => new ConfigJsonPathCandidate(path, IsBundled: false)),
            ConfigDescription,
            LoadThemePaletteConfiguration,
            parsed =>
            {
                merged = merged is null
                    ? parsed
                    : Merge(merged, parsed);
            });

        if (merged is null)
        {
            configuration = EmptyConfiguration;
            return false;
        }

        configuration = merged;
        return true;
    }

    internal static ThemePaletteConfiguration Merge(ThemePaletteConfiguration @base, ThemePaletteConfiguration overlay)
    {
        var mergedThemes = @base.Themes.ToDictionary(static theme => theme.Id);
        foreach (var theme in overlay.Themes)
        {
            if (mergedThemes.TryGetValue(theme.Id, out var baseTheme))
            {
                mergedThemes[theme.Id] = MergeTheme(baseTheme, theme);
            }
            else
            {
                mergedThemes[theme.Id] = theme;
            }
        }

        return new ThemePaletteConfiguration(mergedThemes.Values);
    }

    private static ThemeDefinition MergeTheme(ThemeDefinition @base, ThemeDefinition overlay)
    {
        if (@base is FixedThemeDefinition baseFixedTheme &&
            overlay is FixedThemeDefinition overlayFixedTheme)
        {
            var mergedColors = new Dictionary<string, ThemeColor>(baseFixedTheme.ColorsByKey);
            foreach (var (key, color) in overlayFixedTheme.ColorsByKey)
            {
                mergedColors[key] = color;
            }

            return new FixedThemeDefinition(
                overlayFixedTheme.Id,
                overlayFixedTheme.DisplayName,
                overlayFixedTheme.InheritedThemeId,
                mergedColors,
                overlayFixedTheme.IsVisibleInThemeList);
        }

        return overlay;
    }

    internal static bool TryLoadFromDirectory(string appBaseDirectory, out ThemePaletteConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(appBaseDirectory);

        var path = ConfigJsonPathResolver.GetBundledPath(appBaseDirectory, "theme-palette.json");
        try
        {
            var loadedConfiguration = ConfigJsonLoadHelper.Load(
                new ConfigJsonPathCandidate(path, IsBundled: true),
                ConfigDescription,
                LoadThemePaletteConfiguration);

            configuration = loadedConfiguration;
            return true;
        }
        catch (Exception)
        {
        }

        configuration = EmptyConfiguration;
        return false;
    }

    private static ThemePaletteConfiguration LoadThemePaletteConfiguration(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var stream = File.OpenRead(path);
        var dto = JsonSerializer.Deserialize<ThemePaletteConfigurationDto>(
            stream,
            ConfigJsonLoadHelper.SerializerOptions)
            ?? throw new InvalidDataException("Theme palette config is null or invalid.");

        var themes = BuildThemesFromDto(dto);
        if (themes.Count == 0)
        {
            throw new InvalidDataException("Theme palette config has no valid entries.");
        }

        return new ThemePaletteConfiguration(themes);
    }

    private static List<ThemeDefinition> BuildThemesFromDto(ThemePaletteConfigurationDto dto)
    {
        var themes = new List<ThemeDefinition>();

        foreach (var themeDto in dto.Themes)
        {
            var themeId = themeDto.Id;
            if (string.IsNullOrEmpty(themeId))
            {
                continue;
            }

            var displayName = ResolveDisplayName(themeId, themeDto.DisplayNames);
            var themeDef = BuildThemeDefinition(
                themeId,
                displayName,
                themeDto.EntriesFrom,
                themeDto.Enabled);

            // Apply entries from theme DTO if present
            if (themeDto.Entries is not null && themeDef is FixedThemeDefinition fixedTheme)
            {
                var colorsByKey = new Dictionary<string, ThemeColor>();
                foreach (var entry in themeDto.Entries)
                {
                    if (!string.IsNullOrWhiteSpace(entry.Key))
                    {
                        colorsByKey[entry.Key] = entry.Hex;
                    }
                }

                if (colorsByKey.Count > 0)
                {
                    themeDef = new FixedThemeDefinition(
                        fixedTheme.Id,
                        fixedTheme.DisplayName,
                        fixedTheme.InheritedThemeId,
                        colorsByKey,
                        fixedTheme.IsVisibleInThemeList);
                }
            }

            themes.Add(themeDef);
        }

        return themes;
    }

    private static ThemeDefinition BuildThemeDefinition(
        string themeId,
        LocalizedText displayName,
        EntriesFromSpec? entriesFrom,
        bool isVisibleInThemeList)
    {
        if (entriesFrom is null)
        {
            return new FixedThemeDefinition(themeId, displayName, isVisibleInThemeList: isVisibleInThemeList);
        }

        return entriesFrom switch
        {
            InheritedEntriesFromSpec inherited =>
                new FixedThemeDefinition(
                    themeId,
                    displayName,
                    inherited.SourceThemeId,
                    isVisibleInThemeList: isVisibleInThemeList),
            SystemDependentEntriesFromSpec systemDependent =>
                new SystemDependentThemeDefinition(
                    themeId,
                    displayName,
                    systemDependent.SourcesByMode,
                    isVisibleInThemeList),
            _ => new FixedThemeDefinition(themeId, displayName, isVisibleInThemeList: isVisibleInThemeList),
        };
    }

    private static LocalizedText ResolveDisplayName(
        string themeId,
        LocalizedText? displayNames = null)
    {
        if (displayNames is not null)
        {
            return displayNames;
        }

        // All themes use title-case name as fallback if not defined in config.
        // Built-in themes (light, dark, system) have display names in theme-palette.json.
        return new LocalizedText(ToTitleCase(themeId));
    }

    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        const int stackBufferLength = 256;
        char[]? rented = null;
        var buffer = value.Length <= stackBufferLength
            ? stackalloc char[value.Length]
            : (rented = ArrayPool<char>.Shared.Rent(value.Length));

        var written = 0;
        var makeUpper = true;

        try
        {
            foreach (var c in value)
            {
                if (c is '-' or '_' or ' ')
                {
                    if (written > 0 && buffer[written - 1] != ' ')
                    {
                        buffer[written++] = ' ';
                    }

                    makeUpper = true;
                    continue;
                }

                buffer[written++] = makeUpper ? char.ToUpperInvariant(c) : c;
                makeUpper = false;
            }

            var start = 0;
            while (start < written && buffer[start] == ' ')
            {
                start++;
            }

            var end = written - 1;
            while (end >= start && buffer[end] == ' ')
            {
                end--;
            }

            if (end < start)
            {
                return value;
            }

            return new string(buffer[start..(end + 1)]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }
}
