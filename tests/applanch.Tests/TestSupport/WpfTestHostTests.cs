using Xunit;

namespace applanch.Tests.TestSupport;

[Collection("WpfTests")]
public sealed class WpfTestHostTests
{
    [Fact]
    public void EnsureAppResources_CreatesPlainApplicationWithoutMainWindow()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            Assert.NotNull(System.Windows.Application.Current);
            Assert.IsType<System.Windows.Application>(System.Windows.Application.Current);
            Assert.IsNotType<applanch.App>(System.Windows.Application.Current);
            Assert.NotNull(System.Windows.Application.Current.Resources["RoundedTextBoxStyle"]);
        });
    }

    [Fact]
    public void RunInSta_WhenActionExceedsTimeout_ThrowsTimeoutException()
    {
        using var blocker = new ManualResetEventSlim(false);
        try
        {
            Assert.Throws<TimeoutException>(() =>
                WpfTestHost.RunInSta(() => blocker.Wait(), TimeSpan.FromMilliseconds(200)));
        }
        finally
        {
            blocker.Set();
        }
    }

    [Fact]
    public void RunInSta_WhenTimeoutExceeded_SetsThreadAsBackgroundSoItDoesNotBlockProcessExit()
    {
        Thread? staThread = null;
        using var ready = new ManualResetEventSlim(false);
        using var blocker = new ManualResetEventSlim(false);
        try
        {
            Assert.Throws<TimeoutException>(() =>
                WpfTestHost.RunInSta(() =>
                {
                    staThread = Thread.CurrentThread;
                    ready.Set();
                    blocker.Wait();
                }, TimeSpan.FromMilliseconds(200)));
        }
        finally
        {
            blocker.Set();
        }

        Assert.True(ready.IsSet);
        Assert.NotNull(staThread);
        Assert.True(staThread!.IsBackground);
    }
}