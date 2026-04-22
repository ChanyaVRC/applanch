namespace applanch.Theming;

public sealed record SystemDependentEntriesFromSpec(
    IReadOnlyDictionary<SystemThemeMode, string> SourcesByMode) : EntriesFromSpec;
