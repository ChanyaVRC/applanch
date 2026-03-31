using System.ComponentModel;
using applanch.Properties;

namespace applanch;

internal sealed class LocalizedStrings : INotifyPropertyChanged
{
    public static LocalizedStrings Instance { get; } = new();

    public string this[string key]
    {
        get
        {
            var value = typeof(Resources).GetProperty(key)?.GetValue(null) as string;
            return string.IsNullOrEmpty(value) ? key : value;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void NotifyLanguageChanged() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
}