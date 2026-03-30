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

        if (!appResolver.TryResolve(input, out var resolvedApp))
        {
            return QuickAddResult.Failed(
                string.Format(Properties.Resources.Error_QuickAddNotFound, input),
                QuickAddMessageSeverity.Warning);
        }

        if (existingItems.Any(item => string.Equals(item.FullPath, resolvedApp.Path, StringComparison.OrdinalIgnoreCase)))
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
}
