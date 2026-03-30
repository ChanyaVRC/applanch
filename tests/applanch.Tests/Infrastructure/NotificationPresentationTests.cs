using System.Windows;
using applanch.Infrastructure;
using Xunit;

namespace applanch.Tests.Infrastructure;

public class NotificationPresentationTests
{
    [Theory]
    [InlineData(MessageBoxImage.Information, "#EE1F2937", "#FF334155")]
    [InlineData(MessageBoxImage.Warning, "#EEF59E0B", "#FFD97706")]
    [InlineData(MessageBoxImage.Error, "#EEB91C1C", "#FFEF4444")]
    public void GetFloatingStyle_ReturnsExpectedBrushes(MessageBoxImage icon, string expectedBackground, string expectedBorder)
    {
        var result = NotificationPresentation.GetFloatingStyle(icon);

        Assert.Equal(expectedBackground, result.Background.ToString());
        Assert.Equal(expectedBorder, result.BorderBrush.ToString());
    }

    [Theory]
    [InlineData(QuickAddMessageSeverity.Information, "#FFDC2626")]
    [InlineData(QuickAddMessageSeverity.Warning, "#FFD97706")]
    public void GetQuickAddForeground_ReturnsExpectedBrush(QuickAddMessageSeverity severity, string expectedForeground)
    {
        var result = NotificationPresentation.GetQuickAddForeground(severity);

        Assert.Equal(expectedForeground, result.ToString());
    }
}