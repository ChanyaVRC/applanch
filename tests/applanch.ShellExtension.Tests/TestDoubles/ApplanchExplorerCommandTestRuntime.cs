using System.Diagnostics;
using applanch.ShellExtension.Interop;

namespace applanch.ShellExtension.Tests.TestDoubles;

internal sealed class ApplanchExplorerCommandTestRuntime : IApplanchExplorerCommandRuntime
{
    public Func<string> MenuTextProvider { get; init; } = static () => string.Empty;
    public Func<string?> ExecutablePathProvider { get; init; } = static () => null;
    public Func<IShellItemArray?, string?> SelectedPathProvider { get; init; } = static _ => null;
    public Func<ProcessStartInfo, Process?> StartProcessHandler { get; init; } = static _ => null;

    public string GetMenuText() => MenuTextProvider();

    public string? ResolveExecutablePath() => ExecutablePathProvider();

    public string? GetSelectedPath(IShellItemArray? itemArray) => SelectedPathProvider(itemArray);

    public Process? StartProcess(ProcessStartInfo startInfo) => StartProcessHandler(startInfo);
}
