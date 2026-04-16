namespace applanch.Core.Infrastructure.Resolution;

public interface IAppResolver
{
    bool TryResolve(string input, out ResolvedApp resolvedApp);
    IReadOnlyList<string> GetSuggestions(string input, int maxResults = 8);
}

