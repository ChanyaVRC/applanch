namespace applanch.ViewModels;

public readonly record struct QuickAddResult(bool IsSuccess, string Message, QuickAddMessageSeverity Severity)
{
    public static QuickAddResult Success() => new(true, string.Empty, QuickAddMessageSeverity.Information);

    public static QuickAddResult Failed(string message, QuickAddMessageSeverity severity) =>
        new(false, message, severity);
}
