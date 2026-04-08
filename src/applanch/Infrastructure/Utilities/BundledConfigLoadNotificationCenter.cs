using System.IO;

namespace applanch.Infrastructure.Utilities;

internal static class BundledConfigLoadNotificationCenter
{
    private static readonly object Gate = new();
    private static readonly List<BundledConfigLoadIssue> PendingIssues = [];
    private static readonly HashSet<BundledConfigLoadIssue> ReportedIssues = [];

    internal static event Action<BundledConfigLoadIssue>? Reported;

    internal static void ReportMissing(string path)
    {
        Report(new BundledConfigLoadIssue(Path.GetFileName(path), IsInvalidFormat: false));
    }

    internal static void ReportInvalidFormat(string path)
    {
        Report(new BundledConfigLoadIssue(Path.GetFileName(path), IsInvalidFormat: true));
    }

    internal static IReadOnlyList<BundledConfigLoadIssue> DrainPending()
    {
        lock (Gate)
        {
            if (PendingIssues.Count == 0)
            {
                return [];
            }

            var issues = PendingIssues.ToArray();
            PendingIssues.Clear();
            return issues;
        }
    }

    internal static void ResetForTests()
    {
        lock (Gate)
        {
            PendingIssues.Clear();
            ReportedIssues.Clear();
        }
    }

    private static void Report(BundledConfigLoadIssue issue)
    {
        lock (Gate)
        {
            if (!ReportedIssues.Add(issue))
            {
                return;
            }

            PendingIssues.Add(issue);
        }

        Reported?.Invoke(issue);
    }
}