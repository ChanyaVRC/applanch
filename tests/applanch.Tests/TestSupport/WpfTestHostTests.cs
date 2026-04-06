using Xunit;

namespace applanch.Tests.TestSupport;

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
}