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
    private static readonly ThemePaletteConfiguration FallbackConfiguration = new(
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

        return MergeWithUserDefinedIfAvailable(AppContext.BaseDirectory, builtIn);
    }

    internal static bool TryLoadForSettings(out ThemePaletteConfiguration configuration)
    {
        if (!TryLoadFromDirectory(AppContext.BaseDirectory, out var builtIn))
        {
            configuration = FallbackConfiguration;
            return false;
        }

        configuration = MergeWithUserDefinedIfAvailable(AppContext.BaseDirectory, builtIn);
        return true;
    }

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
                mergedColors);
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

            if (themeDto.Disabled)
            {
                continue;
            }

            var displayName = ResolveDisplayName(themeId, themeDto.DisplayNames);
            var themeDef = BuildThemeDefinition(themeId, displayName, themeDto.EntriesFrom);

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
                        colorsByKey);
                }
            }

            themes.Add(themeDef);
        }

        return themes.ToArray();
    }

    private static ThemeDefinition BuildThemeDefinition(
        string themeId,
        LocalizedText displayName,
        System.Text.Json.JsonElement? entriesFrom)
    {
        if (!entriesFrom.HasValue)
        {
            return new FixedThemeDefinition(themeId, displayName);
        }

        return entriesFrom.Value.ValueKind switch
        {
            JsonValueKind.String =>
                new FixedThemeDefinition(themeId, displayName, NormalizeThemeId(entriesFrom.Value.GetString())),
            JsonValueKind.Object =>
                BuildSystemDependentThemeFromElements(themeId, displayName, entriesFrom.Value),
            _ => new FixedThemeDefinition(themeId, displayName),
        };
    }

    private static SystemDependentThemeDefinition BuildSystemDependentThemeFromElements(
        string themeId,
        LocalizedText displayName,
        JsonElement modeElement)
    {
        var sources = new Dictionary<SystemThemeMode, string>();

        foreach (var property in modeElement.EnumerateObject())
        {
            var normalizedMode = NormalizeThemeId(property.Name);
            var systemMode = normalizedMode switch
            {
                LightThemeId => SystemThemeMode.Light,
                DarkThemeId => SystemThemeMode.Dark,
                _ => (SystemThemeMode?)null,
            };

            if (systemMode is null || property.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var sourceThemeId = property.Value.GetString();
            var normalizedSource = NormalizeThemeId(sourceThemeId);
            if (!string.IsNullOrEmpty(normalizedSource))
            {
                sources[systemMode.Value] = normalizedSource;
            }
        }

        return sources.Count == 0
            ? new SystemDependentThemeDefinition(themeId, displayName, new Dictionary<SystemThemeMode, string>())
            : new SystemDependentThemeDefinition(themeId, displayName, sources);
    }

    private static string NormalizeThemeId(string? themeId) =>
        string.IsNullOrWhiteSpace(themeId) ? string.Empty : themeId.Trim().ToLowerInvariant();

    private static ThemePaletteEntry FallbackEntry(string key, string lightHex, string darkHex)
    {
        return new ThemePaletteEntry(
            key,
            new Dictionary<string, string>
            {
                [LightThemeId] = lightHex,
                [DarkThemeId] = darkHex,
            });
    }



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

        var normalizedThemeId = NormalizeThemeId(themeId);
        var fallback = normalizedThemeId switch
        {
            SystemThemeId => AppResources.Theme_System,
            LightThemeId => AppResources.Theme_Light,
            DarkThemeId => AppResources.Theme_Dark,
            _ => ToTitleCase(themeId),
        };

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
