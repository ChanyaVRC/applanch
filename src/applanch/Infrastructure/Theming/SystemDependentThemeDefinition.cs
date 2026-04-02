namespace applanch.Infrastructure.Theming;

internal sealed class SystemDependentThemeDefinition(
    string id,
    LocalizedText displayName,
    IReadOnlyDictionary<SystemThemeMode, string> sourcesByMode) : ThemeDefinition(id, displayName)
{
    private readonly IReadOnlyDictionary<SystemThemeMode, string> _sourcesByMode = sourcesByMode;

    internal IReadOnlyDictionary<SystemThemeMode, string> SourcesByMode => _sourcesByMode;

    internal bool TryResolveSystemSource(SystemThemeMode mode, out string sourceThemeId)
    {
        return _sourcesByMode.TryGetValue(mode, out sourceThemeId!);
    }
}
