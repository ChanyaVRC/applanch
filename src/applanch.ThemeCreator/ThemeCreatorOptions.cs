using applanch.Theming;
using applanch.Localization;

namespace applanch.ThemeCreator;

public sealed record ThemeCreatorOptions(
    string ThemeId,
    string OutputPath,
    string? BaseThemeId,
    string? SourcePalettePath,
    IReadOnlyDictionary<LanguageOption, string>? DisplayNames,
    bool IncludeEntries,
    IReadOnlyList<ThemeEntryDto>? Entries = null);
