using applanch.Infrastructure.Resolution;

namespace applanch.Tests.ViewModels.TestDoubles;

internal sealed class FakeResolver : IAppResolver
{
    public bool ShouldResolve { get; set; }
    public ResolvedApp ResolvedApp { get; set; }
    public int SuggestionsCallCount { get; private set; }
    public IReadOnlyList<string> SuggestionsOverride { get; set; } = [];

    public IReadOnlyList<string> GetSuggestions(string input, int maxResults = 8)
    {
        SuggestionsCallCount++;

        if (SuggestionsOverride.Count > 0)
        {
            return SuggestionsOverride.Take(maxResults).ToList();
        }

        return string.IsNullOrWhiteSpace(input) ? [] : [input + "-s1", input + "-s2"];
    }

    public bool TryResolve(string input, out ResolvedApp resolvedApp)
    {
        resolvedApp = ResolvedApp;
        return ShouldResolve;
    }
}
