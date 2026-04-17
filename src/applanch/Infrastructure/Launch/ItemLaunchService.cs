using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using applanch.ViewModels;
using applanch.Core.Utilities;

namespace applanch.Infrastructure.Launch;

internal sealed class ItemLaunchService : IItemLaunchService
{
    private static readonly string[] AccessDeniedMessageTokens = ["access is denied", "アクセスが拒否"];

    private readonly Func<ProcessStartInfo, Process?> _startProcess;
    private readonly ILaunchFallbackResolver _fallbackResolver;

    public ItemLaunchService()
        : this(Process.Start, LaunchFallbackResolver.CreateDefault())
    {
    }

    internal ItemLaunchService(Func<ProcessStartInfo, Process?> startProcess)
        : this(startProcess, LaunchFallbackResolver.CreateDefault())
    {
    }

    internal ItemLaunchService(
        Func<ProcessStartInfo, Process?> startProcess,
        ILaunchFallbackResolver fallbackResolver)
    {
        _startProcess = startProcess;
        _fallbackResolver = fallbackResolver;
    }

    public LaunchExecutionResult TryLaunch(LaunchPath launchPath, string arguments, bool runAsAdministrator = false)
    {
        var path = launchPath.Value;
        var isUrl = launchPath.IsUrl;
        var isDirectory = !isUrl && Directory.Exists(path);
        var isFile = !isUrl && !isDirectory && File.Exists(path);

        if (!isUrl && !isFile && !isDirectory)
        {
            return LaunchExecutionResult.Failed(
                string.Format(AppResources.Error_FileNotFound, path),
                NotificationIconType.Warning,
                LaunchFailureKind.MissingTarget);
        }

        if (_fallbackResolver.TryCreatePreferred(launchPath, runAsAdministrator) is { } preferredFallback)
        {
            try
            {
                AppLogger.Instance.Info($"Using preferred fallback for '{path}' via {preferredFallback.Name}.");
                var preferredProcess = _startProcess(preferredFallback.StartInfo);
                if (preferredProcess is null)
                {
                    return LaunchExecutionResult.Failed(AppResources.Error_LaunchFailed, NotificationIconType.Error);
                }

                return LaunchExecutionResult.Success();
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Error(ex, $"Preferred fallback launch failed for '{path}' via {preferredFallback.Name}");
                return LaunchExecutionResult.Failed(string.Format(AppResources.Error_LaunchFailedWithMessage, ex.Message), NotificationIconType.Error);
            }
        }

        var startInfo = new ProcessStartInfo { UseShellExecute = true };
        if (isDirectory)
        {
            startInfo.FileName = "explorer.exe";
            startInfo.Arguments = $"\"{path}\"";
        }
        else
        {
            startInfo.FileName = path;
            startInfo.Arguments = arguments;
        }

        if (runAsAdministrator)
        {
            startInfo.Verb = "runas";
        }

        try
        {
            var process = _startProcess(startInfo);
            if (process is null)
                return LaunchExecutionResult.Failed(AppResources.Error_LaunchFailed, NotificationIconType.Error);

            return LaunchExecutionResult.Success();
        }
        catch (Exception ex)
        {
            if (IsAccessDenied(ex) && _fallbackResolver.TryCreate(launchPath, runAsAdministrator) is { } fallback)
            {
                try
                {
                    AppLogger.Instance.Warn($"Primary launch denied for '{path}'. Trying fallback: {fallback.Name}.");
                    var fallbackProcess = _startProcess(fallback.StartInfo);
                    if (fallbackProcess is not null)
                    {
                        AppLogger.Instance.Info($"Fallback launch succeeded for '{path}' via {fallback.Name}.");
                        return LaunchExecutionResult.Success();
                    }

                    AppLogger.Instance.Warn($"Fallback launch returned null process for '{path}' via {fallback.Name}.");
                }
                catch (Exception fallbackEx)
                {
                    AppLogger.Instance.Error(fallbackEx, $"Fallback launch failed for '{path}' via {fallback.Name}");
                }
            }

            AppLogger.Instance.Error(ex, $"Failed to launch: {path}");
            return LaunchExecutionResult.Failed(string.Format(AppResources.Error_LaunchFailedWithMessage, ex.Message), NotificationIconType.Error);
        }
    }

    private static bool IsAccessDenied(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is UnauthorizedAccessException)
            {
                return true;
            }

            if (current is Win32Exception win32 && win32.NativeErrorCode == 5)
            {
                return true;
            }

            if (AccessDeniedMessageTokens.Any(token => current.Message.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
