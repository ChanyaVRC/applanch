using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Threading;
using WpfApplication = System.Windows.Application;

namespace applanch.Tests.ThemeCreator.TestSupport;

[SupportedOSPlatform("windows")]
internal static class WpfTestHost
{
    internal static void RunInSta(Action action)
    {
        Exception? captured = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            ExceptionDispatchInfo.Capture(captured).Throw();
        }
    }

    internal static void EnsureApplication()
    {
        if (WpfApplication.Current is null)
        {
            _ = new WpfApplication
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown,
            };
        }
    }

    internal static void ShowOffscreen(Window window)
    {
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -10000;
        window.Top = -10000;
        window.ShowInTaskbar = false;
        window.ShowActivated = false;
        window.Opacity = 0;
        window.Show();
        DoEvents();
    }

    internal static void DoEvents()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
}