using System.Globalization;
using applanch.Theming;
using applanch.Localization;

namespace applanch.ThemeCreator;

public sealed class ThemeCreatorService
{
    public string CreateThemeJson(string sourceJson, ThemeCreatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(sourceJson);
        ArgumentNullException.ThrowIfNull(options);

        var sourceDocument = DeserializeDocument(sourceJson);
        var outputDocument = CreateThemeDocument(sourceDocument, options);
        return ThemePaletteConfigurationJsonSerializer.Serialize(outputDocument, writeIndented: true);
    }

    public string CreateThemeJsonFromFile(ThemeCreatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var sourceDocument = ResolveSourceDocument(options);
        var outputDocument = CreateThemeDocument(sourceDocument, options);
        return ThemePaletteConfigurationJsonSerializer.Serialize(outputDocument, writeIndented: true);
    }

    public void CreateThemeFile(ThemeCreatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var sourceDocument = ResolveSourceDocument(options);
        var outputDocument = CreateThemeDocument(sourceDocument, options);
        ThemePaletteConfigurationJsonSerializer.SerializeFile(options.OutputPath, outputDocument, writeIndented: true);
    }

    public IReadOnlyList<string> GetThemeIds(string sourceJson)
    {
        ArgumentNullException.ThrowIfNull(sourceJson);

        var sourceDocument = DeserializeDocument(sourceJson);
        return GetThemeIds(sourceDocument);
    }

    public IReadOnlyList<string> GetThemeIdsFromFile(string sourcePalettePath)
    {
        ArgumentNullException.ThrowIfNull(sourcePalettePath);

        var sourceDocument = DeserializeDocumentFromFile(sourcePalettePath);
        return GetThemeIds(sourceDocument);
    }

    public IReadOnlyList<ThemeEntryDto> GetEntries(string sourceJson, string baseThemeId)
    {
        ArgumentNullException.ThrowIfNull(sourceJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseThemeId);

        var sourceDocument = DeserializeDocument(sourceJson);
        return GetEntries(sourceDocument, baseThemeId);
    }

    public IReadOnlyList<ThemeEntryDto> GetEntriesFromFile(string sourcePalettePath, string baseThemeId)
    {
        ArgumentNullException.ThrowIfNull(sourcePalettePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseThemeId);

        var sourceDocument = DeserializeDocumentFromFile(sourcePalettePath);
        return GetEntries(sourceDocument, baseThemeId);
    }

    private static ThemePaletteConfigurationDto DeserializeDocument(string sourceJson)
        => ThemePaletteConfigurationJsonSerializer.Deserialize(sourceJson);

    private static ThemePaletteConfigurationDto DeserializeDocumentFromFile(string sourcePalettePath)
        => ThemePaletteConfigurationJsonSerializer.DeserializeFile(sourcePalettePath);

    private static ThemePaletteConfigurationDto ResolveSourceDocument(ThemeCreatorOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SourcePalettePath))
        {
            return DeserializeDocumentFromFile(options.SourcePalettePath);
        }

        return new ThemePaletteConfigurationDto([]);
    }

    private static ThemePaletteConfigurationDto CreateThemeDocument(ThemePaletteConfigurationDto sourceDocument, ThemeCreatorOptions options)
    {
        var baseThemeId = string.IsNullOrWhiteSpace(options.BaseThemeId)
            ? null
            : options.BaseThemeId.Trim();
        var sourceTheme = baseThemeId is null
            ? null
            : FindTheme(sourceDocument, baseThemeId)
                ?? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, AppResources.ServiceBaseThemeNotFoundFormat, baseThemeId));
        var entriesFrom = baseThemeId is null
            ? null
            : new InheritedEntriesFromSpec(baseThemeId);

        return new ThemePaletteConfigurationDto(
        [
            new ThemeDto(
                options.ThemeId,
                CreateDisplayNames(options.DisplayNames),
                entriesFrom,
                options.IncludeEntries ? CloneEntries(options.Entries ?? sourceTheme?.Entries) : null)
        ]);
    }

    private static string[] GetThemeIds(ThemePaletteConfigurationDto sourceDocument)
    {
        var themes = GetThemes(sourceDocument);

        return themes
            .Select(static theme => theme.Id)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .ToArray();
    }

    private static ThemeDto? FindTheme(ThemePaletteConfigurationDto sourceDocument, string themeId)
    {
        var themes = GetThemes(sourceDocument);

        foreach (var node in themes)
        {
            if (string.IsNullOrWhiteSpace(node.Id))
            {
                continue;
            }

            if (string.Equals(node.Id, themeId, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }
        }

        return null;
    }

    private static List<ThemeEntryDto> GetEntries(ThemePaletteConfigurationDto sourceDocument, string baseThemeId)
    {
        var sourceTheme = FindTheme(sourceDocument, baseThemeId)
            ?? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, AppResources.ServiceBaseThemeNotFoundFormat, baseThemeId));

        return CloneEntries(sourceTheme.Entries) ?? [];
    }

    private static ThemeDto[] GetThemes(ThemePaletteConfigurationDto sourceDocument)
        => sourceDocument.Themes?.ToArray()
            ?? throw new InvalidOperationException(AppResources.ServiceThemesArrayMissing);

    private static List<ThemeEntryDto>? CloneEntries(IReadOnlyList<ThemeEntryDto>? source)
    {
        return source?.Select(static entry => new ThemeEntryDto(entry.Key, entry.Hex)).ToList();
    }

    private static LocalizedText? CreateDisplayNames(IReadOnlyDictionary<LanguageOption, string>? displayNames)
    {
        if (displayNames is null || displayNames.Count == 0)
        {
            return null;
        }

        var translations = new Dictionary<LanguageOption, string>();
        foreach (var (language, value) in displayNames)
        {
            if (language == LanguageOption.System || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            translations[language] = value.Trim();
        }

        if (translations.Count == 0)
        {
            return null;
        }

        var fallback = translations.TryGetValue(LanguageOption.PrimaryFallbackLanguage, out var primaryFallback)
            ? primaryFallback
            : translations.Values.First();
        return new LocalizedText(fallback, translations);
    }
}

