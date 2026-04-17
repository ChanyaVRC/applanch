using applanch.Infrastructure.Registry;

namespace applanch.Infrastructure.Integration;

internal sealed class ContextMenuRegistrarRuntime : IContextMenuRegistrarRuntime
{
    private readonly IRegistryRuntime _registry;

    public ContextMenuRegistrarRuntime()
        : this(new RegistryRuntime())
    {
    }

    internal ContextMenuRegistrarRuntime(IRegistryRuntime registry)
    {
        _registry = registry;
    }

    public string? GetExecutablePath() => Environment.ProcessPath;

    public string? ResolveShellExtensionComHostPath(string exePath)
        => ContextMenuRegistrar.ResolveShellExtensionComHostPath(exePath);

    public void WriteRegistryCommand(string keyPath, string menuText, string iconPath, string command, bool enableExplorerCommand)
        => ContextMenuRegistrar.WriteRegistryCommand(keyPath, menuText, iconPath, command, enableExplorerCommand, _registry);

    public void RegisterExplorerCommandServer(string shellExtensionComHostPath)
        => ContextMenuRegistrar.RegisterExplorerCommandServer(shellExtensionComHostPath, _registry);

    public void DeleteRegistrySubKeyTree(string keyPath)
        => _registry.DeleteSubKeyTree(WinRegistry.CurrentUser, keyPath, throwOnMissingSubKey: false);

    public bool IsExplorerCommandAllowed() => SparsePackageRegistrar.IsPackageRegistered();
}
