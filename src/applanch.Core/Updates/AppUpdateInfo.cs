using System.Diagnostics.CodeAnalysis;

namespace applanch.Updates;

public sealed record AppUpdateInfo(
    SemanticVersion NewVersion,
    SemanticVersion CurrentVersion,
    [property: StringSyntax(StringSyntaxAttribute.Uri)] Uri AssetDownloadUrl,
    [property: StringSyntax(StringSyntaxAttribute.Uri)] Uri ReleaseUrl);
