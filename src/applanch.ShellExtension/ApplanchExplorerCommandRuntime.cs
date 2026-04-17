using System.Diagnostics;
using applanch.ShellExtension.Interop;

namespace applanch.ShellExtension;

internal sealed class ApplanchExplorerCommandRuntime : IApplanchExplorerCommandRuntime
{
    public string GetMenuText() => ApplanchExplorerCommand.GetMenuTextFromRegistry();

    public string? ResolveExecutablePath() => ApplanchExplorerCommand.ResolveExecutablePath();

    public string? GetSelectedPath(IShellItemArray? itemArray) => ApplanchExplorerCommand.GetSelectedPath(itemArray);

    public Process? StartProcess(ProcessStartInfo startInfo) => Process.Start(startInfo);
}
