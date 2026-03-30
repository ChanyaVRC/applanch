using applanch.Infrastructure.Utilities;
using Xunit;

namespace applanch.Tests.Infrastructure.Utilities;

public class FloatingNotificationProgressStateTests
{
    [Theory]
    [InlineData(1.0, 1.0)]
    [InlineData(0.75, 0.75)]
    [InlineData(0.0, 0.0)]
    [InlineData(-0.2, 0.0)]
    [InlineData(1.4, 1.0)]
    public void CaptureVisibleScale_ClampsToVisibleRange(double input, double expected)
    {
        var result = FloatingNotificationProgressState.CaptureVisibleScale(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void CaptureVisibleScale_WithInvalidValue_ReturnsZero(double input)
    {
        var result = FloatingNotificationProgressState.CaptureVisibleScale(input);

        Assert.Equal(0, result);
    }
}
