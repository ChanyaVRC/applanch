using System.Diagnostics.CodeAnalysis;

namespace applanch;

internal interface IAppUpdateService
{
    Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default);
    Task ApplyUpdateAsync(AppUpdateInfo update, CancellationToken cancellationToken = default);
}

internal sealed record AppUpdateInfo(
    string NewVersion,
    string CurrentVersion,
    [property: StringSyntax(StringSyntaxAttribute.Uri)] string AssetDownloadUrl,
    [property: StringSyntax(StringSyntaxAttribute.Uri)] string ReleaseUrl);
