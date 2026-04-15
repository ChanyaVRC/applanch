using applanch.ViewModels;

namespace applanch.Infrastructure.Launch;

internal readonly record struct LaunchExecutionResult(bool IsSuccess, string Message, NotificationIconType Icon, LaunchFailureKind FailureKind)
{
    public static LaunchExecutionResult Success() => new(true, string.Empty, NotificationIconType.None, LaunchFailureKind.None);

    public static LaunchExecutionResult Failed(string message, NotificationIconType icon, LaunchFailureKind failureKind = LaunchFailureKind.Other)
        => new(false, message, icon, failureKind);
}

