using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Serialization;

namespace applanch.Infrastructure.Storage;

[JsonConverter(typeof(LanguageOptionJsonConverter))]
public sealed record LanguageOption
{
    public static LanguageOption System { get; } = new("system");
    public static LanguageOption English { get; } = new("en");
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

    internal static LanguageOption PrimaryFallbackLanguage => English;

    internal string Code { get; }

    private LanguageOption(string code)
    {
        Code = code;
    }

    internal static bool TryFromCode(
        string code,
        [NotNullWhen(true)] out LanguageOption? language) => ByCode.TryGetValue(code, out language);

    internal CultureInfo GetCultureInfo() =>
        ConcreteByCode.ContainsKey(Code) ? CultureInfo.GetCultureInfo(Code) : CultureInfo.InstalledUICulture;


    internal static bool TryMapFromCultureCode(
        string cultureCode,
        [NotNullWhen(true)] out LanguageOption? language) => ConcreteByCode.TryGetValue(cultureCode, out language);

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

    public override string ToString() => Code;
}
