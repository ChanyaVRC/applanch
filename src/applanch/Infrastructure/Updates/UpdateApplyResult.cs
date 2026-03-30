namespace applanch.Infrastructure.Updates;

internal readonly record struct UpdateApplyResult(bool IsSuccess, string ErrorMessage)
{
    public static UpdateApplyResult Success() => new(true, string.Empty);

    public static UpdateApplyResult Failed(string errorMessage) =>
        new(false, errorMessage);
}
