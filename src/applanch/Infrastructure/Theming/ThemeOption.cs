namespace applanch.Infrastructure.Theming;

internal sealed class ThemeOption : System.ComponentModel.INotifyPropertyChanged
{
    internal ThemeOption(string themeId, LocalizedText displayNameText, bool IsSystemOption = false)
    {
        ThemeId = themeId;
        DisplayNameText = displayNameText;
        this.IsSystemOption = IsSystemOption;
    }

    public string ThemeId { get; }

    internal LocalizedText DisplayNameText { get; }

    public bool IsSystemOption { get; }

    public string DisplayName => DisplayNameText.ResolveCurrentCulture();

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    internal void NotifyDisplayNameChanged()
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(DisplayName)));
    }
}
