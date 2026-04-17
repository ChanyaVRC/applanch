using System.Globalization;

namespace applanch.Localization;

/// <summary>
/// Manages localized text with support for multiple language options.
/// </summary>
public sealed record LocalizedText
{
    private readonly Dictionary<LanguageOption, string> _translations;

    /// <summary>
    /// Gets the read-only dictionary of translations.
    /// </summary>
    public IReadOnlyDictionary<LanguageOption, string> Translations => _translations;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizedText"/> class.
    /// </summary>
    /// <param name="defaultText">The default text to use as fallback.</param>
    /// <param name="translations">Optional dictionary of language-specific translations.</param>
    public LocalizedText(string defaultText, IReadOnlyDictionary<LanguageOption, string>? translations = null)
    {
        ArgumentNullException.ThrowIfNull(defaultText);
        _translations = NormalizeTranslations(defaultText, translations);
    }

    /// <summary>
    /// Resolves the text for a specific language option.
    /// </summary>
    /// <param name="language">The language option.</param>
    /// <returns>The translated text, or fallback if unavailable.</returns>
    public string Resolve(LanguageOption language)
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

    /// <summary>
    /// Resolves the text based on the current UI culture.
    /// </summary>
    /// <returns>The translated text, or fallback if culture is unsupported.</returns>
    public string ResolveCurrentCulture()
    {
        var cultureCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (LanguageOption.TryMapFromCultureCode(cultureCode, out var mappedLanguage))
        {
            return Resolve(mappedLanguage);
        }

        return ResolveFallback();
    }

    private string ResolveFallback()
    {
        return _translations[LanguageOption.PrimaryFallbackLanguage];
    }

    private static Dictionary<LanguageOption, string> NormalizeTranslations(
        string defaultText,
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

        if (!normalized.ContainsKey(LanguageOption.PrimaryFallbackLanguage))
        {
            normalized[LanguageOption.PrimaryFallbackLanguage] = defaultText;
        }

        return normalized;
    }
}

