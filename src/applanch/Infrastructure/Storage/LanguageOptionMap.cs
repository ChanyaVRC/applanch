using System.Globalization;

namespace applanch.Infrastructure.Storage;

internal static class LanguageOptionMap
{
    private static readonly Dictionary<LanguageOption, string> CultureByLanguage =
        new()
        {
            [LanguageOption.English] = "en",
            [LanguageOption.Japanese] = "ja",
        };

    private static readonly string[] SupportedCultureCodesCache =
    [
        .. CultureByLanguage.Values,
    ];

    private static readonly Dictionary<string, LanguageOption> LanguageByCulture = CultureByLanguage
        .ToDictionary(static x => x.Value, static x => x.Key, StringComparer.OrdinalIgnoreCase);

    private static readonly CultureInfo[] SupportedCulturesCache =
    [
        .. SupportedCultureCodesCache.Select(static cultureCode => CultureInfo.GetCultureInfo(cultureCode)),
    ];

    internal static IReadOnlyDictionary<LanguageOption, string> SupportedLanguages => CultureByLanguage;

    internal static IEnumerable<string> SupportedCultureCodes => SupportedCultureCodesCache;

    internal static LanguageOption PrimaryFallbackLanguage => LanguageOption.English;

    internal static bool TryGetCultureCode(LanguageOption language, out string cultureCode) =>
        CultureByLanguage.TryGetValue(language, out cultureCode!);

    internal static bool TryMapFromCultureCode(string? cultureCode, out LanguageOption language)
    {
        language = LanguageOption.System;
        if (string.IsNullOrWhiteSpace(cultureCode))
        {
            return false;
        }

        return LanguageByCulture.TryGetValue(cultureCode.Trim(), out language);
    }

    internal static IEnumerable<CultureInfo> EnumerateSupportedCultures(bool includeInvariantCulture)
    {
        if (includeInvariantCulture)
        {
            yield return CultureInfo.InvariantCulture;
        }

        foreach (var culture in SupportedCulturesCache)
        {
            yield return culture;
        }
    }
}
