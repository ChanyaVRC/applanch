using System.Globalization;
using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Theming;

internal sealed record LocalizedText
{
    private readonly Dictionary<LanguageOption, string> _translations;

    internal LocalizedText(string @default, IReadOnlyDictionary<LanguageOption, string>? translations = null)
    {
        ArgumentNullException.ThrowIfNull(@default);
        _translations = NormalizeTranslations(@default, translations);
    }

    internal string Resolve(LanguageOption language)
    {
        if (language == LanguageOption.System)
        {
            return ResolveCurrentCulture();
        }

        if (_translations.TryGetValue(language, out var translated))
        {
            return translated;
        }

        return ResolveFallback();
    }

    internal string ResolveCurrentCulture()
    {
        var cultureCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (LanguageOptionMap.TryMapFromCultureCode(cultureCode, out var mappedLanguage))
        {
            return Resolve(mappedLanguage);
        }

        return ResolveFallback();
    }

    private string ResolveFallback()
    {
        return _translations[LanguageOptionMap.PrimaryFallbackLanguage];
    }

    private static Dictionary<LanguageOption, string> NormalizeTranslations(
        string @default,
        IReadOnlyDictionary<LanguageOption, string>? translations)
    {
        var normalized = new Dictionary<LanguageOption, string>();
        if (translations is not null)
        {
            foreach (var (language, text) in translations)
            {
                if (language == LanguageOption.System)
                {
                    continue;
                }

                if (text is null)
                {
                    continue;
                }

                normalized[language] = text;
            }
        }

        if (!normalized.ContainsKey(LanguageOptionMap.PrimaryFallbackLanguage))
        {
            normalized[LanguageOptionMap.PrimaryFallbackLanguage] = @default;
        }

        return normalized;
    }

}
