namespace applanch.Infrastructure.Theming;

internal sealed record SystemDependentEntriesFromSpec(
    IReadOnlyDictionary<SystemThemeMode, string> SourcesByMode) : EntriesFromSpec;
