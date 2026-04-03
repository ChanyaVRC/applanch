using applanch.Infrastructure.Resolution;
using applanch.Infrastructure.Integration;

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
            return QuickAddResult.Failed(AppResources.Error_QuickAddEmpty, QuickAddMessageSeverity.Information);
        }

        if (input.Length >= 3 && char.IsLetter(input[0]) && input[1] == ':' && input[2] == '/')
        {
            return QuickAddResult.Failed(AppResources.Error_InvalidPathSeparator, QuickAddMessageSeverity.Warning);
        }

        if (!appResolver.TryResolve(input, out var resolvedApp))
        {
            return QuickAddResult.Failed(
                string.Format(AppResources.Error_QuickAddNotFound, input),
                QuickAddMessageSeverity.Warning);
        }

        if (existingItems.Any(item => item.FullPath == resolvedApp.Path))
        {
            return QuickAddResult.Failed(AppResources.Error_AlreadyRegistered, QuickAddMessageSeverity.Information);
        }

        newItem = new LaunchItemViewModel(
            resolvedApp.Path,
            quickAddCategory,
            quickAddArguments,
            resolvedApp.DisplayName,
            iconProvider);

        return QuickAddResult.Success();
    }
}
