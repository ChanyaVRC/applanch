using System.Diagnostics;
using applanch.ShellExtension.Interop;

namespace applanch.ShellExtension;

internal interface IApplanchExplorerCommandRuntime
{
    string GetMenuText();

    string? ResolveExecutablePath();

    string? GetSelectedPath(IShellItemArray? itemArray);

    Process? StartProcess(ProcessStartInfo startInfo);
}
