using System.Windows;

namespace applanch.ViewModels;

public sealed class FloatingNotificationState : ObservableObject
{
    private string _message = string.Empty;
    private NotificationIconType _iconType;
    private string _actionText = string.Empty;
    private Action? _action;

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

    public string ActionText
    {
        get => _actionText;
        internal set => SetField(ref _actionText, value);
    }

    public Action? Action
    {
        get => _action;
        internal set
        {
            if (SetField(ref _action, value))
            {
                OnPropertyChanged(nameof(ActionVisibility));
            }
        }
    }

    public Visibility ActionVisibility => _action is null ? Visibility.Collapsed : Visibility.Visible;

    internal void Show(string message, NotificationIconType iconType, string? actionText = null, Action? action = null)
    {
        Message = message;
        IconType = iconType;
        ActionText = actionText ?? string.Empty;
        Action = action;
    }

    internal void Clear()
    {
        Message = string.Empty;
        IconType = NotificationIconType.None;
        ActionText = string.Empty;
        Action = null;
    }
}
