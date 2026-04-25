using applanch.Core.ViewModels;
using applanch.Localization;

namespace applanch.Theming;

public sealed class ThemeOption : ObservableObject
{
    public ThemeOption(string themeId, LocalizedText displayNameText, bool IsSystemOption = false)
    {
        ArgumentNullException.ThrowIfNull(themeId);
        ArgumentNullException.ThrowIfNull(displayNameText);

        ThemeId = themeId;
        DisplayNameText = displayNameText;
        this.IsSystemOption = IsSystemOption;
    }

    public string ThemeId { get; }

    public LocalizedText DisplayNameText { get; }

    public bool IsSystemOption { get; }

    public string DisplayName => DisplayNameText.ResolveCurrentCulture();

    public void NotifyDisplayNameChanged()
    {
        OnPropertyChanged(nameof(DisplayName));
    }
}

