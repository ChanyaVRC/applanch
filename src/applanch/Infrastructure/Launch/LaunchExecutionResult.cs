using System.Windows;

namespace applanch.Infrastructure.Launch;

internal readonly record struct LaunchExecutionResult(bool IsSuccess, string Message, MessageBoxImage Icon, LaunchFailureKind FailureKind)
{
    public static LaunchExecutionResult Success() => new(true, string.Empty, MessageBoxImage.None, LaunchFailureKind.None);

    public static LaunchExecutionResult Failed(string message, MessageBoxImage icon, LaunchFailureKind failureKind = LaunchFailureKind.Other)
        => new(false, message, icon, failureKind);
}

