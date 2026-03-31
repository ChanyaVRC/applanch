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
    public string FallbackTrigger { get; init; } = "access-denied";
    public string UriTemplate { get; init; } = string.Empty;
    public string AppId { get; init; } = string.Empty;
    public string AppIdSource { get; init; } = string.Empty;
    public string FileNameTemplate { get; init; } = string.Empty;
    public string ArgumentsTemplate { get; init; } = string.Empty;
}
