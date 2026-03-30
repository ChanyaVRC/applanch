namespace applanch;

internal sealed class GitHubRelease
{
    public string TagName { get; init; } = string.Empty;
    public string HtmlUrl { get; init; } = string.Empty;
    public List<GitHubAsset> Assets { get; init; } = [];
}

internal sealed class GitHubAsset
{
    public string Name { get; init; } = string.Empty;
    public string BrowserDownloadUrl { get; init; } = string.Empty;
}
