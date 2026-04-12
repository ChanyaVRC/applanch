using System.Windows;

namespace applanch.Tests.TestSupport;

internal sealed class TestWindow : Window, IDisposable
{
    public void Dispose()
    {
        if (IsVisible)
        {
            Close();
        }
    }
}
