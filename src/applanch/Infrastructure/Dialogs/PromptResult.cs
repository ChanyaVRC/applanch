namespace applanch.Infrastructure.Dialogs;

public readonly record struct PromptResult<T>(string Text, T? SelectedItem);