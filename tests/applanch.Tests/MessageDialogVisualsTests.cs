using System.Windows;
using Xunit;
using applanch.Infrastructure.Dialogs;

namespace applanch.Tests;

public class MessageDialogVisualsTests
{
    [Fact]
    public void Resolve_Error_ReturnsErrorSymbol()
    {
        var visual = MessageDialogVisuals.Resolve(MessageBoxImage.Error);

        Assert.Equal("\uEA39", visual.Symbol);
        Assert.Equal("Brush.SurfaceBorder", visual.BrushResourceKey);
        Assert.True(visual.ShowIcon);
    }

    [Fact]
    public void Resolve_Warning_ReturnsWarningSymbol()
    {
        var visual = MessageDialogVisuals.Resolve(MessageBoxImage.Warning);

        Assert.Equal("\uE7BA", visual.Symbol);
        Assert.Equal("Brush.SurfaceBorder", visual.BrushResourceKey);
        Assert.True(visual.ShowIcon);
    }

    [Fact]
    public void Resolve_Information_ReturnsInfoSymbol()
    {
        var visual = MessageDialogVisuals.Resolve(MessageBoxImage.Information);

        Assert.Equal("\uE946", visual.Symbol);
        Assert.Equal("Brush.TextSecondary", visual.BrushResourceKey);
        Assert.True(visual.ShowIcon);
    }

    [Fact]
    public void Resolve_Question_ReturnsQuestionSymbol()
    {
        var visual = MessageDialogVisuals.Resolve(MessageBoxImage.Question);

        Assert.Equal("\uE897", visual.Symbol);
        Assert.Equal("Brush.TextSecondary", visual.BrushResourceKey);
        Assert.True(visual.ShowIcon);
    }

    [Fact]
    public void Resolve_None_ReturnsNoSymbol()
    {
        var visual = MessageDialogVisuals.Resolve(MessageBoxImage.None);

        Assert.Equal(string.Empty, visual.Symbol);
        Assert.Equal("Brush.TextSecondary", visual.BrushResourceKey);
        Assert.False(visual.ShowIcon);
    }
}

