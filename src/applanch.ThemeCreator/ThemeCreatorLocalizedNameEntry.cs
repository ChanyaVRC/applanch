using applanch.Localization;
using System.Globalization;

namespace applanch.ThemeCreator;

internal sealed class ThemeCreatorLocalizedNameEntry
{
    private string? _cachedLabel;

    internal ThemeCreatorLocalizedNameEntry(LanguageOption language)
    {
        Language = language;
    }

    public LanguageOption Language { get; }

    public string Label => _cachedLabel ??= string.Format(
        CultureInfo.CurrentCulture,
        AppResources.DisplayNameLabelFormat,
        Language.GetCultureInfo().NativeName,
        Language.Code);

    public string Value { get; set; } = string.Empty;
}
