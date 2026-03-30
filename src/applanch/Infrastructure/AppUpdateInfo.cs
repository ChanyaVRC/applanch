using System.Diagnostics.CodeAnalysis;

namespace applanch;

internal sealed record AppUpdateInfo(
    string NewVersion,
    string CurrentVersion,
    [property: StringSyntax(StringSyntaxAttribute.Uri)] string AssetDownloadUrl,
    [property: StringSyntax(StringSyntaxAttribute.Uri)] string ReleaseUrl);
