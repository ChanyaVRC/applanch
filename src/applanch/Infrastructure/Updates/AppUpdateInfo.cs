using System.Diagnostics.CodeAnalysis;

namespace applanch.Infrastructure.Updates;

internal sealed record AppUpdateInfo(
    string NewVersion,
    string CurrentVersion,
    [property: StringSyntax(StringSyntaxAttribute.Uri)] string AssetDownloadUrl,
    [property: StringSyntax(StringSyntaxAttribute.Uri)] string ReleaseUrl);

