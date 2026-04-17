namespace applanch.Core.Configuration;

public sealed record BundledConfigLoadIssue(string FileName, bool IsInvalidFormat);
