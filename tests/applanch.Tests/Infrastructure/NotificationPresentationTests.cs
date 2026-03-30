using System.Windows;
using applanch.Infrastructure;
using Xunit;

namespace applanch.Tests.Infrastructure;

public class NotificationPresentationTests
{
    [Theory]
    [InlineData(MessageBoxImage.Information, "Brush.NotificationInfoBackground", "Brush.NotificationInfoBorder")]
    [InlineData(MessageBoxImage.Warning, "Brush.NotificationWarningBackground", "Brush.NotificationWarningBorder")]
    [InlineData(MessageBoxImage.Error, "Brush.NotificationErrorBackground", "Brush.NotificationErrorBorder")]
    public void GetFloatingStyleKeys_ReturnsExpectedResourceKeys(MessageBoxImage icon, string expectedBackgroundKey, string expectedBorderKey)
    {
        var result = NotificationPresentation.GetFloatingStyleKeys(icon);

        Assert.Equal(expectedBackgroundKey, result.BackgroundKey);
        Assert.Equal(expectedBorderKey, result.BorderKey);
    }

    [Theory]
    [InlineData(QuickAddMessageSeverity.Information, "Brush.QuickAddInfoText")]
    [InlineData(QuickAddMessageSeverity.Warning, "Brush.QuickAddWarningText")]
    public void GetQuickAddForegroundKey_ReturnsExpectedResourceKey(QuickAddMessageSeverity severity, string expectedKey)
    {
        var result = NotificationPresentation.GetQuickAddForegroundKey(severity);

        Assert.Equal(expectedKey, result);
    }
}