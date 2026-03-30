namespace applanch;

internal static class DragReorderIndexCalculator
{
    public static int Calculate(int oldIndex, int desiredInsertIndex, int count)
    {
        if (count <= 1)
        {
            return oldIndex;
        }

        var newIndex = desiredInsertIndex;
        if (newIndex > oldIndex)
        {
            newIndex--;
        }

        return Math.Clamp(newIndex, 0, count - 1);
    }
}
