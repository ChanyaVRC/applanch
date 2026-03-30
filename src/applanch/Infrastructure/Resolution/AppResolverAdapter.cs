namespace applanch.Infrastructure.Resolution;

internal sealed class AppResolverAdapter : IAppResolver
{
    public bool TryResolve(string input, out ResolvedApp resolvedApp) =>
        AppResolver.TryResolve(input, out resolvedApp);

    public IReadOnlyList<string> GetSuggestions(string input, int maxResults = 8) =>
        AppResolver.GetSuggestions(input, maxResults);
}

