namespace applanch.Infrastructure.Updates;

internal sealed class GitHubRelease
{
    public string TagName { get; init; } = string.Empty;
    public string HtmlUrl { get; init; } = string.Empty;
    public bool Prerelease { get; init; }
    public List<GitHubAsset> Assets { get; init; } = [];
}

