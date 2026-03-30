using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace applanch;

public sealed class FloatingNotificationState : INotifyPropertyChanged
{
    private string _message = string.Empty;
    private NotificationIconType _iconType;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Message
    {
        get => _message;
        internal set => SetField(ref _message, value);
    }

    public NotificationIconType IconType
    {
        get => _iconType;
        internal set => SetField(ref _iconType, value);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
