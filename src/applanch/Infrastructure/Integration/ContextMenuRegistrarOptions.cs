namespace applanch.Infrastructure.Integration;

/// <summary>
/// Overrides for <see cref="ContextMenuRegistrar"/> dependencies, used in tests.
/// Null properties fall back to the real production implementations.
/// </summary>
internal sealed record ContextMenuRegistrarOptions
{
    public Func<string?>? ExecutablePathProvider { get; init; }
    public Func<string, string?>? ShellExtensionComHostPathResolver { get; init; }
    public Action<string, string, string, string, bool>? WriteRegistryCommand { get; init; }
    public Action<string>? RegisterExplorerCommandServer { get; init; }
    public bool EnableLegacyCleanup { get; init; } = true;
    public Action<string>? DeleteRegistrySubKeyTree { get; init; }
    public Func<bool>? IsExplorerCommandAllowed { get; init; }
}
