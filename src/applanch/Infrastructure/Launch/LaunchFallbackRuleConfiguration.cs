namespace applanch.Infrastructure.Launch;

internal sealed class LaunchFallbackRuleConfiguration
{
    public string Name { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public List<string> MatchFileNames { get; init; } = [];
    public string Product { get; init; } = string.Empty;
    public string Patchline { get; init; } = "live";
    public string PathContains { get; init; } = string.Empty;
}
