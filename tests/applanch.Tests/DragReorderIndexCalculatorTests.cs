using Xunit;
using applanch.Infrastructure.Utilities;

namespace applanch.Tests;

public class DragReorderIndexCalculatorTests
{
    [Fact]
    public void Calculate_WhenDesiredAfterOld_AdjustsIndex()
    {
        var index = DragReorderIndexCalculator.Calculate(oldIndex: 1, desiredInsertIndex: 4, count: 6);

        Assert.Equal(3, index);
    }

    [Fact]
    public void Calculate_WhenDesiredBeforeOld_KeepsIndex()
    {
        var index = DragReorderIndexCalculator.Calculate(oldIndex: 4, desiredInsertIndex: 1, count: 6);

        Assert.Equal(1, index);
    }

    [Fact]
    public void Calculate_ClampsRange()
    {
        Assert.Equal(0, DragReorderIndexCalculator.Calculate(oldIndex: 2, desiredInsertIndex: -20, count: 6));
        Assert.Equal(5, DragReorderIndexCalculator.Calculate(oldIndex: 2, desiredInsertIndex: 20, count: 6));
    }

    [Fact]
    public void Calculate_WhenCountIsOne_ReturnsOldIndex()
    {
        var index = DragReorderIndexCalculator.Calculate(oldIndex: 0, desiredInsertIndex: 1, count: 1);

        Assert.Equal(0, index);
    }

    [Fact]
    public void Calculate_WhenOldIndexOutOfRange_ClampsUsingCountRange()
    {
        var index = DragReorderIndexCalculator.Calculate(oldIndex: 10, desiredInsertIndex: 2, count: 5);

        Assert.Equal(2, index);
    }
}

