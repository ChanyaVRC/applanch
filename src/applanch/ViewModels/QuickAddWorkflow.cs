using applanch.Infrastructure.Resolution;
using applanch.Infrastructure.Integration;
using applanch.Infrastructure.Utilities;

namespace applanch.ViewModels;

internal sealed class QuickAddWorkflow(IAppResolver appResolver, ILaunchItemIconProvider? iconProvider = null)
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

        var normalizedResolvedPath = NormalizePath(resolvedApp.Path);

        newItem = new LaunchItemViewModel(
            normalizedResolvedPath,
            quickAddCategory,
            quickAddArguments,
            resolvedApp.DisplayName,
            iconProvider);

        return QuickAddResult.Success();
    }

    private static bool IsSamePath(string left, string right)
    {
        var normalizedLeft = NormalizePath(left);
        var normalizedRight = NormalizePath(right);
        return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        return PathNormalization.TryNormalizePersistablePath(path, out var normalizedPath)
            ? normalizedPath
            : PathNormalization.NormalizeForComparison(path);
    }
}
