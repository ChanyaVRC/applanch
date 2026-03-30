using System.Windows;

namespace applanch.Infrastructure.Launch;

internal readonly record struct LaunchExecutionResult(bool IsSuccess, string Message, MessageBoxImage Icon)
{
    public static LaunchExecutionResult Success() => new(true, string.Empty, MessageBoxImage.None);

    public static LaunchExecutionResult Failed(string message, MessageBoxImage icon) => new(false, message, icon);
}

