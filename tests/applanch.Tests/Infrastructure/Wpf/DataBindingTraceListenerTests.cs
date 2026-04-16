using System.Diagnostics;
using Xunit;

namespace applanch.Tests.Infrastructure.Wpf;

public sealed class DataBindingTraceListenerTests
{
    [Theory]
    [InlineData(TraceEventType.Warning, true)]
    [InlineData(TraceEventType.Error, true)]
    [InlineData(TraceEventType.Critical, true)]
    [InlineData(TraceEventType.Information, false)]
    [InlineData(TraceEventType.Verbose, false)]
    public void ShouldLog_UsesWarningAndAbove(TraceEventType eventType, bool expected)
    {
        Assert.Equal(expected, DataBindingTraceListener.ShouldLog(eventType));
    }

    [Theory]
    [InlineData(null, "<empty>")]
    [InlineData("", "<empty>")]
    [InlineData("   ", "<empty>")]
    [InlineData("  binding failed  ", "binding failed")]
    public void NormalizeMessage_TrimsAndNormalizesEmpty(string? input, string expected)
    {
        Assert.Equal(expected, DataBindingTraceListener.NormalizeMessage(input));
    }
}
