using applanch.Theming;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace applanch.ThemeCreator;

internal sealed class ThemeCreatorEditableEntry : INotifyPropertyChanged
{
    private string _hex;

    internal ThemeCreatorEditableEntry(string key, string description, string hex)
    {
        Key = key;
        Description = description;
        _hex = hex;
    }

    public string Key { get; }

    public string Description { get; }

    public string Hex
    {
        get => _hex;
        set
        {
            if (string.Equals(_hex, value, StringComparison.Ordinal))
            {
                return;
            }

            _hex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewBrush));
        }
    }

    public System.Windows.Media.Brush PreviewBrush
    {
        get
        {
            if (ThemeColor.TryParse(Hex, out var color))
            {
                return new SolidColorBrush(color.ToMediaColor());
            }
            return System.Windows.Media.Brushes.Transparent;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
