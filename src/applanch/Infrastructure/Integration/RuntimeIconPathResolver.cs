namespace applanch.Infrastructure.Integration;

internal sealed class RuntimeIconPathResolver : IIconPathResolver
{
    public string Resolve(string launchPath) => LaunchItemIconPathResolver.ResolveForRuntime(launchPath);
}
