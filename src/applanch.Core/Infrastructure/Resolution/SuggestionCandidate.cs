namespace applanch.Core.Infrastructure.Resolution;

internal readonly record struct SuggestionCandidate(string Text, int Score, int SourcePriority);

