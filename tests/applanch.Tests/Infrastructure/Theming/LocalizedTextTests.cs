using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using applanch.Tests.TestSupport;
using System.Text.Json;
using Xunit;

namespace applanch.Tests.Infrastructure.Theming;

public sealed class LocalizedTextTests
{
    public static IEnumerable<object[]> ResolveFallbackCases()
    {
        yield return
        [
            "Default",
            new Dictionary<LanguageOption, string>
            {
                [LanguageOption.English] = "English",
            },
            LanguageOption.Japanese,
            "English",
        ];

        yield return
        [
            "Fallback",
            new Dictionary<LanguageOption, string>(),
            LanguageOption.System,
            "Fallback",
        ];

        yield return
        [
            "Default",
            new Dictionary<LanguageOption, string>
            {
                [LanguageOption.English] = null!,
            },
            LanguageOption.English,
            "Default",
        ];

        yield return
        [
            "Default",
            new Dictionary<LanguageOption, string>
            {
                [LanguageOption.Japanese] = null!,
            },
            LanguageOption.English,
            "Default",
        ];
    }

    [Fact]
    public void ResolveCurrentCulture_WhenCultureIsUnknown_FallsBackToEnglish()
    {
        var localized = new LocalizedText(
            "Default",
            new Dictionary<LanguageOption, string>
            {
                [LanguageOption.English] = "English",
                [LanguageOption.Japanese] = "Japanese",
            });

        using var scope = new CultureScope("fr-FR");

        Assert.Equal("English", localized.ResolveCurrentCulture());
    }

    [Theory]
    [MemberData(nameof(ResolveFallbackCases))]
    public void Resolve_WhenTranslationIsUnavailable_FallsBackAsExpected(
        string @default,
        IReadOnlyDictionary<LanguageOption, string>? translations,
        LanguageOption target,
        string expected)
    {
        var localized = new LocalizedText(@default, translations);

        Assert.Equal(expected, localized.Resolve(target));
    }

    [Fact]
    public void Resolve_WhenTranslationIsWhitespace_ReturnsWhitespace()
    {
        var localized = new LocalizedText(
            "Default",
            new Dictionary<LanguageOption, string>
            {
                [LanguageOption.English] = "   ",
            });

        Assert.Equal("   ", localized.Resolve(LanguageOption.English));
    }

    [Fact]
    public void Translations_WhenDefaultOnly_ContainsOnlyFallbackLanguage()
    {
        var localized = new LocalizedText("Default");

        var translations = localized.Translations;
        Assert.Single(translations);
        Assert.Equal("Default", translations[LanguageOption.English]);
    }

    [Fact]
    public void JsonSerialize_WritesAllAvailableTranslationsFromTranslations()
    {
        var localized = new LocalizedText(
            "English",
            new Dictionary<LanguageOption, string>
            {
                [LanguageOption.Japanese] = "日本語",
            });

        var json = JsonSerializer.Serialize(
            localized,
            new JsonSerializerOptions
            {
                Converters = { new LocalizedTextJsonConverter() },
            });

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.Equal("English", root.GetProperty("en").GetString());
        Assert.Equal("日本語", root.GetProperty("ja").GetString());
    }
}
