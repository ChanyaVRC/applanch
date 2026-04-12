using System.Buffers;
using System.IO;
using System.Text.Json;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Theming;

internal static class ThemePaletteConfigurationLoader
{
    private const string ConfigDescription = "theme palette config";
    internal const string SystemThemeId = "system";
    internal const string LightThemeId = "light";
    internal const string DarkThemeId = "dark";

    private const string UserDefinedThemePaletteDirectoryName = "theme-palette";

    private static ThemeDefinition[] CreateFallbackThemes()
    {
        // Creates the three built-in themes that are always guaranteed to exist:
        // light (fixed), dark (fixed), and system (system-dependent).
        // These mirror what's defined in theme-palette.json but provide fallback
        // when configuration file is unavailable.
        return new ThemeDefinition[]
        {
            new FixedThemeDefinition(
                LightThemeId,
                ResolveDisplayName(LightThemeId),
                inheritedThemeId: null,
                BuildLightFallbackColors()),
            new FixedThemeDefinition(
                DarkThemeId,
                ResolveDisplayName(DarkThemeId),
                inheritedThemeId: null,
                BuildDarkFallbackColors()),
            new SystemDependentThemeDefinition(
                SystemThemeId,
                ResolveDisplayName(SystemThemeId),
                new Dictionary<SystemThemeMode, string>
                {
                    [SystemThemeMode.Light] = LightThemeId,
                    [SystemThemeMode.Dark] = DarkThemeId,
                }),
        };
    }

    private static Dictionary<string, string> BuildLightFallbackColors() =>
        new()
        {
            { "Brush.AppBackground", "#F1F5F9" },
            { "Brush.Surface", "#FFFFFF" },
            { "Brush.SurfaceBorder", "#D0D7E2" },
            { "Brush.TextPrimary", "#0F172A" },
            { "Brush.SidebarPinSlash", "#0F172A" },
            { "Brush.TextSecondary", "#475569" },
            { "Brush.TextTertiary", "#64748B" },
            { "Brush.ScrollbarThumb", "#64748B" },
            { "Brush.PrereleaseBadgeBackground", "#FFFFFF" },
            { "Brush.PrereleaseBadgeBorder", "#D0D7E2" },
            { "Brush.PrereleaseBadgeText", "#475569" },
            { "Brush.ItemBackground", "#F8FAFC" },
            { "Brush.ItemBorder", "#D7DEE8" },
            { "Brush.IconBackground", "#E2E8F0" },
            { "Brush.NotificationInfoBackground", "#FFFFFF" },
            { "Brush.NotificationInfoBorder", "#D7DEE8" },
            { "Brush.NotificationActionHover", "#D7DEE8" },
            { "Brush.NotificationWarningBackground", "#FFF7ED" },
            { "Brush.NotificationWarningBorder", "#FDBA74" },
            { "Brush.MissingPathWarningBadge", "#FDBA74" },
            { "Brush.NotificationErrorBackground", "#FEF2F2" },
            { "Brush.NotificationErrorBorder", "#FCA5A5" },
            { "Brush.NotificationProgressTrack", "#E2E8F0" },
            { "Brush.NotificationProgressValue", "#94A3B8" },
            { "Brush.QuickAddInfoText", "#B45309" },
            { "Brush.QuickAddWarningText", "#92400E" },
            { "Brush.DialogInfo", "#475569" },
            { "Brush.DialogQuestion", "#475569" },
            { "Brush.DialogWarning", "#FDBA74" },
            { "Brush.DialogError", "#FCA5A5" },
        };

    private static Dictionary<string, string> BuildDarkFallbackColors() =>
        new()
        {
            { "Brush.AppBackground", "#0B1220" },
            { "Brush.Surface", "#131D31" },
            { "Brush.SurfaceBorder", "#223149" },
            { "Brush.TextPrimary", "#E2E8F0" },
            { "Brush.SidebarPinSlash", "#E2E8F0" },
            { "Brush.TextSecondary", "#9FB2C9" },
            { "Brush.TextTertiary", "#7C93AF" },
            { "Brush.ScrollbarThumb", "#7C93AF" },
            { "Brush.PrereleaseBadgeBackground", "#131D31" },
            { "Brush.PrereleaseBadgeBorder", "#223149" },
            { "Brush.PrereleaseBadgeText", "#9FB2C9" },
            { "Brush.ItemBackground", "#111C30" },
            { "Brush.ItemBorder", "#2A3B57" },
            { "Brush.IconBackground", "#20304B" },
            { "Brush.NotificationInfoBackground", "#131D31" },
            { "Brush.NotificationInfoBorder", "#2A3B57" },
            { "Brush.NotificationActionHover", "#2A3B57" },
            { "Brush.NotificationWarningBackground", "#2B2111" },
            { "Brush.NotificationWarningBorder", "#B45309" },
            { "Brush.MissingPathWarningBadge", "#B45309" },
            { "Brush.NotificationErrorBackground", "#2A1618" },
            { "Brush.NotificationErrorBorder", "#B45353" },
            { "Brush.NotificationProgressTrack", "#2A3B57" },
            { "Brush.NotificationProgressValue", "#7C93AF" },
            { "Brush.QuickAddInfoText", "#FBBF24" },
            { "Brush.QuickAddWarningText", "#F59E0B" },
            { "Brush.DialogInfo", "#9FB2C9" },
            { "Brush.DialogQuestion", "#9FB2C9" },
            { "Brush.DialogWarning", "#B45309" },
            { "Brush.DialogError", "#B45353" },
        };

    private static readonly ThemePaletteConfiguration FallbackConfiguration = new(
        CreateFallbackThemes(),
        LoadedFromConfig: false);

    internal static ThemePaletteConfiguration LoadForRuntime()
    {
        var builtIn = TryLoadFromDirectory(AppContext.BaseDirectory, out var configuration)
            ? configuration
            : FallbackConfiguration;

        var merged = MergeWithUserDefinedIfAvailable(AppContext.BaseDirectory, builtIn);
        return EnsureBuiltInThemesExist(merged);
    }

    internal static bool TryLoadForSettings(out ThemePaletteConfiguration configuration)
    {
        if (!TryLoadFromDirectory(AppContext.BaseDirectory, out var builtIn))
        {
            configuration = FallbackConfiguration;
            return false;
        }

        configuration = MergeWithUserDefinedIfAvailable(AppContext.BaseDirectory, builtIn);
        configuration = EnsureBuiltInThemesExist(configuration);
        return true;
    }

    /// <summary>
    /// Gets the set of built-in theme IDs that are always guaranteed to exist.
    /// </summary>
    private static IReadOnlyList<string> BuiltInThemeIds => FallbackConfiguration.Themes.Select(t => t.Id).ToList();

    /// <summary>
    /// Determines whether the given theme ID is a built-in theme (system, light, or dark).
    /// </summary>
    internal static bool IsBuiltInTheme(string? themeId) =>
        !string.IsNullOrWhiteSpace(themeId) && BuiltInThemeIds.Contains(themeId);

    private static ThemePaletteConfiguration MergeWithUserDefinedIfAvailable(
        string appBaseDirectory,
        ThemePaletteConfiguration builtIn)
    {
        return TryLoadUserDefined(appBaseDirectory, out var userDefined)
            ? Merge(builtIn, userDefined)
            : builtIn;
    }

    internal static bool TryLoadUserDefined(string appBaseDirectory, out ThemePaletteConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(appBaseDirectory);

        var userDefinedDirectory = ConfigJsonPathResolver.GetUserDefinedDirectory(appBaseDirectory, UserDefinedThemePaletteDirectoryName);

        if (!Directory.Exists(userDefinedDirectory))
        {
            configuration = FallbackConfiguration;
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
            configuration = FallbackConfiguration;
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

        return new ThemePaletteConfiguration(
            mergedThemes.Values.ToArray(),
            LoadedFromConfig: true);
    }

    private static ThemeDefinition MergeTheme(ThemeDefinition @base, ThemeDefinition overlay)
    {
        if (@base is FixedThemeDefinition baseFixedTheme &&
            overlay is FixedThemeDefinition overlayFixedTheme)
        {
            var mergedColors = new Dictionary<string, string>(baseFixedTheme.ColorsByKey);
            foreach (var (key, hex) in overlayFixedTheme.ColorsByKey)
            {
                mergedColors[key] = hex;
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

        configuration = FallbackConfiguration;
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
        if (themes.Length == 0)
        {
            throw new InvalidDataException("Theme palette config has no valid entries.");
        }

        return new ThemePaletteConfiguration(themes, LoadedFromConfig: true);
    }

    private static ThemePaletteConfiguration EnsureBuiltInThemesExist(ThemePaletteConfiguration configuration)
    {
        var themesById = configuration.Themes.ToDictionary(static t => t.Id);
        var builtInThemes = FallbackConfiguration.Themes;
        var missingBuiltIn = builtInThemes.Where(t => !themesById.ContainsKey(t.Id)).ToList();

        if (missingBuiltIn.Count == 0)
        {
            return configuration;
        }

        // Rebuild configuration with added built-in themes
        var allThemes = new List<ThemeDefinition>(configuration.Themes);
        allThemes.AddRange(missingBuiltIn);

        return new ThemePaletteConfiguration(allThemes, configuration.Entries, LoadedFromConfig: true);
    }

    private static ThemeDefinition[] BuildThemesFromDto(ThemePaletteConfigurationDto dto)
    {
        var themes = new List<ThemeDefinition>();

        foreach (var themeDto in dto.Themes)
        {
            var themeId = NormalizeThemeId(themeDto.Id);
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
                var colorsByKey = new Dictionary<string, string>();
                foreach (var entry in themeDto.Entries)
                {
                    if (!string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Hex))
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

        return themes.ToArray();
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
                    NormalizeThemeId(inherited.SourceThemeId),
                    isVisibleInThemeList: isVisibleInThemeList),
            SystemDependentEntriesFromSpec systemDependent =>
                BuildSystemDependentThemeFromSpec(
                    themeId,
                    displayName,
                    systemDependent,
                    isVisibleInThemeList),
            _ => new FixedThemeDefinition(themeId, displayName, isVisibleInThemeList: isVisibleInThemeList),
        };
    }

    private static SystemDependentThemeDefinition BuildSystemDependentThemeFromSpec(
        string themeId,
        LocalizedText displayName,
        SystemDependentEntriesFromSpec entriesFrom,
        bool isVisibleInThemeList)
    {
        var normalizedSources = entriesFrom.SourcesByMode
            .ToDictionary(static entry => entry.Key, entry => NormalizeThemeId(entry.Value));

        return normalizedSources.Count == 0
            ? new SystemDependentThemeDefinition(themeId, displayName, new Dictionary<SystemThemeMode, string>(), isVisibleInThemeList)
            : new SystemDependentThemeDefinition(themeId, displayName, normalizedSources, isVisibleInThemeList);
    }

    private static string NormalizeThemeId(string? themeId) =>
        string.IsNullOrWhiteSpace(themeId) ? string.Empty : themeId.Trim().ToLowerInvariant();

    private static LocalizedText ResolveDisplayName(
        string themeId,
        Dictionary<string, string>? displayNamesMap = null)
    {
        var langs = new Dictionary<LanguageOption, string>();
        if (displayNamesMap is not null)
        {
            foreach (var (cultureCode, displayName) in displayNamesMap)
            {
                if (!string.IsNullOrWhiteSpace(displayName) &&
                    LanguageOptionMap.TryMapFromCultureCode(cultureCode, out var language))
                {
                    langs[language] = displayName;
                }
            }
        }

        // All themes use title-case name as fallback if not defined in config
        // Built-in themes (light, dark, system) have display names in theme-palette.json
        var fallback = ToTitleCase(themeId);

        return new LocalizedText(fallback, langs.Count > 0 ? langs : null);
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
