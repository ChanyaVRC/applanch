using System.Runtime.ExceptionServices;
using System.Windows.Threading;

namespace applanch.Tests.TestSupport;

internal static class WpfTestHost
{
    private static readonly object AppInitLock = new();

    internal static void RunInSta(Action action)
    {
        RunInSta(action, Timeout.InfiniteTimeSpan);
    }

    internal static void RunInSta(Action action, TimeSpan timeout)
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
        thread.Start();
        bool completed;
        if (timeout == Timeout.InfiniteTimeSpan)
        {
            thread.Join();
            completed = true;
        }
        else
        {
            completed = thread.Join(timeout);
        }

        if (!completed)
        {
            throw new TimeoutException($"STA test execution exceeded timeout of {timeout}.");
        }

        if (captured is not null)
        {
            ExceptionDispatchInfo.Capture(captured).Throw();
        }
    }

    internal static void RunInSta(Func<Task> action)
    {
        RunInSta(() => action().GetAwaiter().GetResult());
    }

    internal static void RunInStaAndDrain(Action action)
    {
        RunInSta(() =>
        {
            action();
            DoEvents();
        });
    }

    internal static void EnsureAppResources()
    {
        lock (AppInitLock)
        {
            if (System.Windows.Application.Current is null)
            {
                _ = new System.Windows.Application
                {
                    ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown,
                };
            }

            if (System.Windows.Application.Current?.Resources["RoundedTextBoxStyle"] is null)
            {
                var dictionary = (System.Windows.ResourceDictionary)System.Windows.Application.LoadComponent(
                    new Uri("/applanch;component/AppResources.xaml", UriKind.Relative));
                System.Windows.Application.Current!.Resources.MergedDictionaries.Add(dictionary);
            }
        }
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
