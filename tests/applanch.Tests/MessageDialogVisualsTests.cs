using System.Windows;
using Xunit;

namespace applanch.Tests;

public class MessageDialogVisualsTests
{
    [Fact]
    public void Resolve_Error_ReturnsErrorSymbol()
    {
        var visual = MessageDialogVisuals.Resolve(MessageBoxImage.Error);

        Assert.Equal("X", visual.Symbol);
        Assert.Equal("Brush.SurfaceBorder", visual.BrushResourceKey);
    }

    [Fact]
    public void Resolve_Warning_ReturnsWarningSymbol()
    {
        var visual = MessageDialogVisuals.Resolve(MessageBoxImage.Warning);

        Assert.Equal("!", visual.Symbol);
        Assert.Equal("Brush.SurfaceBorder", visual.BrushResourceKey);
    }

    [Fact]
    public void Resolve_Information_ReturnsInfoSymbol()
    {
        var visual = MessageDialogVisuals.Resolve(MessageBoxImage.Information);

        Assert.Equal("i", visual.Symbol);
        Assert.Equal("Brush.TextSecondary", visual.BrushResourceKey);
    }

    [Fact]
    public void Resolve_Question_ReturnsQuestionSymbol()
    {
        var visual = MessageDialogVisuals.Resolve(MessageBoxImage.Question);

        Assert.Equal("?", visual.Symbol);
        Assert.Equal("Brush.TextSecondary", visual.BrushResourceKey);
    }

    [Fact]
    public void Resolve_None_ReturnsNoSymbol()
    {
        var visual = MessageDialogVisuals.Resolve(MessageBoxImage.None);

        Assert.Equal(string.Empty, visual.Symbol);
        Assert.Equal("Brush.TextSecondary", visual.BrushResourceKey);
    }
}
