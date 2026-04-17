namespace applanch.Infrastructure.Integration;

internal interface IContextMenuRegistrarRuntime
{
    string? GetExecutablePath();

    string? ResolveShellExtensionComHostPath(string exePath);

    void WriteRegistryCommand(string keyPath, string menuText, string iconPath, string command, bool enableExplorerCommand);

    void RegisterExplorerCommandServer(string shellExtensionComHostPath);

    void DeleteRegistrySubKeyTree(string keyPath);

    bool IsExplorerCommandAllowed();
}
