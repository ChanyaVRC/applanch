namespace applanch;

internal interface IAppResolver
{
    bool TryResolve(string input, out ResolvedApp resolvedApp);
    IReadOnlyList<string> GetSuggestions(string input, int maxResults = 8);
}
