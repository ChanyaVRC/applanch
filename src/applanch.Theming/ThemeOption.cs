using System.ComponentModel;
using applanch.Core.Localization;

namespace applanch.Theming;

internal sealed class ThemeOption : INotifyPropertyChanged
{
    internal ThemeOption(string themeId, LocalizedText displayNameText, bool IsSystemOption = false)
    {
        ArgumentNullException.ThrowIfNull(themeId);
        ArgumentNullException.ThrowIfNull(displayNameText);

        ThemeId = themeId;
        DisplayNameText = displayNameText;
        this.IsSystemOption = IsSystemOption;
    }

    public string ThemeId { get; }

    internal LocalizedText DisplayNameText { get; }

    public bool IsSystemOption { get; }

    public string DisplayName => DisplayNameText.ResolveCurrentCulture();

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void NotifyDisplayNameChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
    }
}
