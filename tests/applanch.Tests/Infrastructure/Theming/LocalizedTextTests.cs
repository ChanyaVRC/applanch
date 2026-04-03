using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Theming;

public sealed class LocalizedTextTests
{
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

    [Fact]
    public void Resolve_WhenTargetTranslationMissing_FallsBackToEnglish()
    {
        var localized = new LocalizedText(
            "Default",
            new Dictionary<LanguageOption, string>
            {
                [LanguageOption.English] = "English",
            });

        Assert.Equal("English", localized.Resolve(LanguageOption.Japanese));
    }

    [Fact]
    public void Resolve_WhenNoTranslations_FallsBackToDefault()
    {
        var localized = new LocalizedText("Fallback", new Dictionary<LanguageOption, string>());

        Assert.Equal("Fallback", localized.Resolve(LanguageOption.System));
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
    public void Resolve_WhenLanguageOptionTranslationValueIsNull_IgnoresNullAndFallsBack()
    {
        var localized = new LocalizedText(
            "Default",
            new Dictionary<LanguageOption, string>
            {
                [LanguageOption.English] = null!,
            });

        Assert.Equal("Default", localized.Resolve(LanguageOption.English));
    }

    [Fact]
    public void Resolve_WhenSecondaryLanguageTranslationValueIsNull_IgnoresNullAndFallsBack()
    {
        var localized = new LocalizedText(
            "Default",
            new Dictionary<LanguageOption, string>
            {
                [LanguageOption.Japanese] = null!,
            });

        Assert.Equal("Default", localized.Resolve(LanguageOption.English));
    }
}
