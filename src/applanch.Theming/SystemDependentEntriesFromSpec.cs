namespace applanch.Theming;

internal sealed record SystemDependentEntriesFromSpec(
    IReadOnlyDictionary<SystemThemeMode, string> SourcesByMode) : EntriesFromSpec;
