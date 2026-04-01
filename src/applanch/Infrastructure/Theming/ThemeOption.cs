namespace applanch.Infrastructure.Theming;

internal sealed record ThemeOption(
    string ThemeId,
    string DisplayName,
    bool IsSystemOption = false);
