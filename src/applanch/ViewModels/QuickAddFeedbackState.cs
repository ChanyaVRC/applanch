using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace applanch;

public sealed class QuickAddFeedbackState : INotifyPropertyChanged
{
    private string _message = string.Empty;
    private QuickAddMessageSeverity _severity;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Message
    {
        get => _message;
        internal set
        {
            if (SetField(ref _message, value))
            {
                OnPropertyChanged(nameof(MessageVisibility));
            }
        }
    }

    public QuickAddMessageSeverity Severity
    {
        get => _severity;
        internal set => SetField(ref _severity, value);
    }

    public Visibility MessageVisibility =>
        string.IsNullOrEmpty(_message) ? Visibility.Collapsed : Visibility.Visible;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
