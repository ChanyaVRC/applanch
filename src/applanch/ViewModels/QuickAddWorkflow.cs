using System.IO;
using applanch.Infrastructure.Resolution;

namespace applanch;

internal sealed class QuickAddWorkflow(IAppResolver appResolver)
{
    internal IReadOnlyList<string> GetSuggestions(string input, int maxResults)
    {
        return appResolver.GetSuggestions(input, maxResults);
    }

    internal QuickAddResult TryCreateLaunchItem(
        string quickAddNameOrPath,
        string quickAddCategory,
        string quickAddArguments,
        IEnumerable<LaunchItemViewModel> existingItems,
        out LaunchItemViewModel? newItem)
    {
        newItem = null;

        var input = quickAddNameOrPath.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            return QuickAddResult.Failed(Properties.Resources.Error_QuickAddEmpty, QuickAddMessageSeverity.Information);
        }

        if (input.Length >= 3 && char.IsLetter(input[0]) && input[1] == ':' && input[2] == '/')
        {
            return QuickAddResult.Failed(Properties.Resources.Error_InvalidPathSeparator, QuickAddMessageSeverity.Warning);
        }

        if (!appResolver.TryResolve(input, out var resolvedApp))
        {
            return QuickAddResult.Failed(
                string.Format(Properties.Resources.Error_QuickAddNotFound, input),
                QuickAddMessageSeverity.Warning);
        }

        if (existingItems.Any(item => IsSamePath(item.FullPath, resolvedApp.Path)))
        {
            return QuickAddResult.Failed(Properties.Resources.Error_AlreadyRegistered, QuickAddMessageSeverity.Information);
        }

        newItem = new LaunchItemViewModel(
            resolvedApp.Path,
            quickAddCategory,
            quickAddArguments,
            resolvedApp.DisplayName);

        return QuickAddResult.Success();
    }

    private static bool IsSamePath(string left, string right)
    {
        var normalizedLeft = NormalizePathForComparison(left);
        var normalizedRight = NormalizePathForComparison(right);
        return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePathForComparison(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            var root = Path.GetPathRoot(fullPath);
            if (!string.IsNullOrWhiteSpace(root) &&
                string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath;
            }

            return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch (Exception)
        {
            return path.Trim();
        }
    }
}
