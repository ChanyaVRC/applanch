namespace applanch.Infrastructure.Integration;

internal sealed class LaunchItemIconPathMappingRuleConfiguration
{
    public string Name { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public List<string> MatchFileNames { get; init; } = [];
    public string ParentDirectoryName { get; init; } = string.Empty;
    public string PathContains { get; init; } = string.Empty;
    public string IconPathTemplate { get; init; } = string.Empty;
}
