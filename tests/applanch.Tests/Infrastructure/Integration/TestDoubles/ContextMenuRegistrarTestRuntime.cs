using applanch.Infrastructure.Integration;

namespace applanch.Tests.Infrastructure.Integration.TestDoubles;

internal sealed class ContextMenuRegistrarTestRuntime : IContextMenuRegistrarRuntime
{
    public Func<string?> ExecutablePathProvider { get; init; } = static () => Environment.ProcessPath;
    public Func<string, string?> ShellExtensionComHostPathResolver { get; init; } = static _ => null;
    public Action<string, string, string, string, bool> WriteRegistryCommandAction { get; init; } = static (_, _, _, _, _) => { };
    public Action<string> RegisterExplorerCommandServerAction { get; init; } = static _ => { };
    public Action<string> DeleteRegistrySubKeyTreeAction { get; init; } = static _ => { };
    public Func<bool> IsExplorerCommandAllowedProvider { get; init; } = static () => true;

    public string? GetExecutablePath() => ExecutablePathProvider();

    public string? ResolveShellExtensionComHostPath(string exePath) => ShellExtensionComHostPathResolver(exePath);

    public void WriteRegistryCommand(string keyPath, string menuText, string iconPath, string command, bool enableExplorerCommand)
        => WriteRegistryCommandAction(keyPath, menuText, iconPath, command, enableExplorerCommand);

    public void RegisterExplorerCommandServer(string shellExtensionComHostPath)
        => RegisterExplorerCommandServerAction(shellExtensionComHostPath);

    public void DeleteRegistrySubKeyTree(string keyPath)
        => DeleteRegistrySubKeyTreeAction(keyPath);

    public bool IsExplorerCommandAllowed() => IsExplorerCommandAllowedProvider();
}
