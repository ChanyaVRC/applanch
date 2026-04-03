using System.Diagnostics.CodeAnalysis;

namespace applanch.Infrastructure.Updates;

internal sealed record AppUpdateInfo(
    string NewVersion,
    string CurrentVersion,
    [property: StringSyntax(StringSyntaxAttribute.Uri)] Uri AssetDownloadUrl,
    [property: StringSyntax(StringSyntaxAttribute.Uri)] Uri ReleaseUrl);

