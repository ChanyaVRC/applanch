using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows.Data;
using applanch.Infrastructure.Localization;

namespace applanch;

internal sealed class LocalizedStrings : INotifyPropertyChanged
{
    private static readonly ResourceManager ResourceManager =
        AppResourceManagerProvider.Instance;

    public static LocalizedStrings Instance { get; } = new();

    public string this[string key]
    {
        get
        {
            var value = ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
            return string.IsNullOrEmpty(value) ? key : value;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void NotifyLanguageChanged() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
}