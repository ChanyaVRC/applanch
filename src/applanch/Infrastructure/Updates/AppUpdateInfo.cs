using System.Diagnostics.CodeAnalysis;

namespace applanch.Infrastructure.Updates;

internal sealed record AppUpdateInfo(
    SemanticVersion NewVersion,
    SemanticVersion CurrentVersion,
    [property: StringSyntax(StringSyntaxAttribute.Uri)] Uri AssetDownloadUrl,
    [property: StringSyntax(StringSyntaxAttribute.Uri)] Uri ReleaseUrl);

