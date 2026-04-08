namespace applanch.Infrastructure.Utilities;

internal sealed record BundledConfigLoadIssue(string FileName, bool IsInvalidFormat);