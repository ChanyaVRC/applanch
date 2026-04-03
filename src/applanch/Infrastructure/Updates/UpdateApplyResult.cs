namespace applanch.Infrastructure.Updates;

internal readonly record struct UpdateApplyResult(bool IsSuccess, UpdateApplyFailureReason FailureReason, string ErrorMessage)
{
    public static UpdateApplyResult Success() => new(true, UpdateApplyFailureReason.None, string.Empty);

    public static UpdateApplyResult Failed(UpdateApplyFailureReason failureReason, string errorMessage) =>
        new(false, failureReason, errorMessage);
}
