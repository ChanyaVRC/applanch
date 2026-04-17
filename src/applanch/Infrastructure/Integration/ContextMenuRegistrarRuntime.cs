using Microsoft.Win32;

namespace applanch.Infrastructure.Integration;

internal sealed class ContextMenuRegistrarRuntime : IContextMenuRegistrarRuntime
{
    public string? GetExecutablePath() => Environment.ProcessPath;

    public string? ResolveShellExtensionComHostPath(string exePath)
        => ContextMenuRegistrar.ResolveShellExtensionComHostPath(exePath);

    public void WriteRegistryCommand(string keyPath, string menuText, string iconPath, string command, bool enableExplorerCommand)
        => ContextMenuRegistrar.WriteRegistryCommand(keyPath, menuText, iconPath, command, enableExplorerCommand);

    public void RegisterExplorerCommandServer(string shellExtensionComHostPath)
        => ContextMenuRegistrar.RegisterExplorerCommandServer(shellExtensionComHostPath);

    public void DeleteRegistrySubKeyTree(string keyPath)
        => Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);

    public bool IsExplorerCommandAllowed() => SparsePackageRegistrar.IsPackageRegistered();
}
