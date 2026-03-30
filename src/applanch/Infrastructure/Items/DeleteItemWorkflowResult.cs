namespace applanch.Infrastructure.Items;

internal readonly record struct DeleteItemWorkflowResult(bool IsCancelled, int DeletedIndex)
{
    public static DeleteItemWorkflowResult Cancelled() => new(true, -1);

    public static DeleteItemWorkflowResult Succeeded(int deletedIndex) => new(false, deletedIndex);
}
