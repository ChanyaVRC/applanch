namespace applanch;

internal interface IAppResolver
{
    bool TryResolve(string input, out ResolvedApp resolvedApp);
    IReadOnlyList<string> GetSuggestions(string input, int maxResults = 8);
}

internal sealed class AppResolverAdapter : IAppResolver
{
    public bool TryResolve(string input, out ResolvedApp resolvedApp) =>
        AppResolver.TryResolve(input, out resolvedApp);

    public IReadOnlyList<string> GetSuggestions(string input, int maxResults = 8) =>
        AppResolver.GetSuggestions(input, maxResults);
}
