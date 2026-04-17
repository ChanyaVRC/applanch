using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Serialization;

namespace applanch.Core.Localization;

/// <summary>
/// Represents a language option for the application.
/// </summary>
[JsonConverter(typeof(LanguageOptionJsonConverter))]
public sealed record LanguageOption
{
    /// <summary>
    /// System language option (uses current UI culture).
    /// </summary>
    public static LanguageOption System { get; } = new("system");

    /// <summary>
    /// English language option.
    /// </summary>
    public static LanguageOption English { get; } = new("en");

    /// <summary>
    /// Japanese language option.
    /// </summary>
    public static LanguageOption Japanese { get; } = new("ja");

    private static readonly Dictionary<string, LanguageOption> ByCode =
        new()
        {
            [System.Code] = System,
            [English.Code] = English,
            [Japanese.Code] = Japanese,
        };

    private static readonly Dictionary<string, LanguageOption> ConcreteByCode =
        new()
        {
            [English.Code] = English,
            [Japanese.Code] = Japanese,
        };

    private static readonly CultureInfo[] SupportedCulturesCache =
    [
        .. ConcreteByCode.Keys.Select(static code => CultureInfo.GetCultureInfo(code)),
    ];

    /// <summary>
    /// Gets the primary fallback language (English).
    /// </summary>
    public static LanguageOption PrimaryFallbackLanguage => English;

    /// <summary>
    /// Gets the culture code for this language option.
    /// </summary>
    public string Code { get; }

    private LanguageOption(string code)
    {
        Code = code;
    }

    /// <summary>
    /// Attempts to retrieve the language option for a given culture code.
    /// </summary>
    public static bool TryFromCode(
        string code,
        [NotNullWhen(true)] out LanguageOption? language) => ByCode.TryGetValue(code, out language);

    /// <summary>
    /// Gets the CultureInfo associated with this language option.
    /// </summary>
    public CultureInfo GetCultureInfo() =>
        ConcreteByCode.ContainsKey(Code) ? CultureInfo.GetCultureInfo(Code) : CultureInfo.InstalledUICulture;

    /// <summary>
    /// Attempts to map a culture code to a concrete language option.
    /// </summary>
    public static bool TryMapFromCultureCode(
        string cultureCode,
        [NotNullWhen(true)] out LanguageOption? language) => ConcreteByCode.TryGetValue(cultureCode, out language);

    /// <summary>
    /// Enumerates all supported cultures.
    /// </summary>
    public static IEnumerable<CultureInfo> EnumerateSupportedCultures(bool includeInvariantCulture)
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

    public override string ToString() => Code;
}

