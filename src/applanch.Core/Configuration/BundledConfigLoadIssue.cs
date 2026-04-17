namespace applanch.Configuration;

public sealed record BundledConfigLoadIssue(string FileName, bool IsInvalidFormat);
