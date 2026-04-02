using System.Windows;

namespace applanch.ViewModels;

public sealed class QuickAddFeedbackState : ObservableObject
{
    private string _message = string.Empty;
    private QuickAddMessageSeverity _severity;

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
}
