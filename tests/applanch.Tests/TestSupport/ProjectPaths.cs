namespace applanch.Tests.TestSupport;

internal static class ProjectPaths
{
    internal static string Root { get; } = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
