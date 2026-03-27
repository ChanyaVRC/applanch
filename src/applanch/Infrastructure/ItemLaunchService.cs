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
                return LaunchExecutionResult.Failed("起動に失敗しました。", MessageBoxImage.Error);

            return LaunchExecutionResult.Success();
        }
        catch (Exception ex)
        {
            return LaunchExecutionResult.Failed($"起動に失敗しました。\n{ex.Message}", MessageBoxImage.Error);
        }
    }
}
