using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Windows;
using applanch.Properties;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Launch;

internal sealed class ItemLaunchService : IItemLaunchService
{
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

    public LaunchExecutionResult TryLaunch(LaunchItemViewModel item, bool runAsAdministrator = false)
    {
        var path = item.FullPath;
        var isDirectory = Directory.Exists(path);
        var isFile = !isDirectory && File.Exists(path);

        if (!isFile && !isDirectory)
        {
            return LaunchExecutionResult.Failed(
                string.Format(Resources.Error_FileNotFound, path),
                MessageBoxImage.Warning);
        }

        if (_fallbackResolver.TryCreatePreferred(path, runAsAdministrator, out var preferredFallback, out var preferredFallbackName))
        {
            try
            {
                AppLogger.Instance.Info($"Using preferred fallback for '{path}' via {preferredFallbackName}.");
                var preferredProcess = _startProcess(preferredFallback);
                if (preferredProcess is null)
                {
                    return LaunchExecutionResult.Failed(Resources.Error_LaunchFailed, MessageBoxImage.Error);
                }

                return LaunchExecutionResult.Success();
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Error(ex, $"Preferred fallback launch failed for '{path}' via {preferredFallbackName}");
                return LaunchExecutionResult.Failed(string.Format(Resources.Error_LaunchFailedWithMessage, ex.Message), MessageBoxImage.Error);
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
            startInfo.Arguments = item.Arguments;
        }

        if (runAsAdministrator)
        {
            startInfo.Verb = "runas";
        }

        try
        {
            var process = _startProcess(startInfo);
            if (process is null)
                return LaunchExecutionResult.Failed(Resources.Error_LaunchFailed, MessageBoxImage.Error);

            return LaunchExecutionResult.Success();
        }
        catch (Exception ex)
        {
            if (IsAccessDenied(ex) && _fallbackResolver.TryCreate(path, runAsAdministrator, out var fallback, out var fallbackName))
            {
                try
                {
                    AppLogger.Instance.Warn($"Primary launch denied for '{path}'. Trying fallback: {fallbackName}.");
                    var fallbackProcess = _startProcess(fallback);
                    if (fallbackProcess is not null)
                    {
                        AppLogger.Instance.Info($"Fallback launch succeeded for '{path}' via {fallbackName}.");
                        return LaunchExecutionResult.Success();
                    }

                    AppLogger.Instance.Warn($"Fallback launch returned null process for '{path}' via {fallbackName}.");
                }
                catch (Exception fallbackEx)
                {
                    AppLogger.Instance.Error(fallbackEx, $"Fallback launch failed for '{path}' via {fallbackName}");
                }
            }

            AppLogger.Instance.Error(ex, $"Failed to launch: {path}");
            return LaunchExecutionResult.Failed(string.Format(Resources.Error_LaunchFailedWithMessage, ex.Message), MessageBoxImage.Error);
        }
    }

    private static bool IsAccessDenied(Exception ex)
    {
        if (ex is UnauthorizedAccessException)
        {
            return true;
        }

        if (ex is Win32Exception win32 && win32.NativeErrorCode == 5)
        {
            return true;
        }

        return new[] { "access is denied", "アクセスが拒否" }
            .Any(token => ex.Message.Contains(token, StringComparison.OrdinalIgnoreCase));
    }
}

