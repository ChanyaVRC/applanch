namespace applanch.Core.Configuration;

internal sealed record BundledConfigLoadIssue(string FileName, bool IsInvalidFormat);