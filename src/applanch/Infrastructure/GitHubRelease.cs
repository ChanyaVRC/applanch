namespace applanch;

internal sealed class GitHubRelease
{
    public string TagName { get; init; } = string.Empty;
    public string HtmlUrl { get; init; } = string.Empty;
    public List<GitHubAsset> Assets { get; init; } = [];
}
