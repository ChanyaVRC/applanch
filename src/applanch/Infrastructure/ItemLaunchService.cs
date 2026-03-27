using System.Diagnostics;
using System.IO;
using System.Windows;

namespace applanch;

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
                $"ファイルまたはフォルダが見つかりません。\n{path}",
                MessageBoxImage.Warning);
        }

        var startInfo = isDirectory
            ? new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            }
            : new ProcessStartInfo
            {
                FileName = path,
                Arguments = item.Arguments,
                UseShellExecute = true
            };

        try
        {
            var process = _startProcess(startInfo);
            return process is null
                ? LaunchExecutionResult.Failed("起動に失敗しました。", MessageBoxImage.Error)
                : LaunchExecutionResult.Success();
        }
        catch (Exception ex)
        {
            return LaunchExecutionResult.Failed($"起動に失敗しました。\n{ex.Message}", MessageBoxImage.Error);
        }
    }
}
