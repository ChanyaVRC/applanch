using System.Diagnostics;
using System.IO;
using System.Windows;
using applanch.Properties;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Launch;

internal sealed class ItemLaunchService : IItemLaunchService
{
    private readonly Func<ProcessStartInfo, Process?> _startProcess;

    public ItemLaunchService()
        : this(Process.Start)
    {
    }

    internal ItemLaunchService(Func<ProcessStartInfo, Process?> startProcess)
    {
        _startProcess = startProcess;
    }

    public LaunchExecutionResult TryLaunch(LaunchItemViewModel item)
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

        try
        {
            var process = _startProcess(startInfo);
            if (process is null)
                return LaunchExecutionResult.Failed(Resources.Error_LaunchFailed, MessageBoxImage.Error);

            return LaunchExecutionResult.Success();
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, $"Failed to launch: {path}");
            return LaunchExecutionResult.Failed(string.Format(Resources.Error_LaunchFailedWithMessage, ex.Message), MessageBoxImage.Error);
        }
    }
}

