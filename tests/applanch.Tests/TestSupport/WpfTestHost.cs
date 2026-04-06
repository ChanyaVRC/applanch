using System.Runtime.ExceptionServices;
using System.Windows.Threading;

namespace applanch.Tests.TestSupport;

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
        thread.Start();
        thread.Join();

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
        if (System.Windows.Application.Current is null)
        {
            var app = new applanch.App();
            app.InitializeComponent();
        }

        if (System.Windows.Application.Current?.Resources["RoundedTextBoxStyle"] is null)
        {
            var dictionary = (System.Windows.ResourceDictionary)System.Windows.Application.LoadComponent(
                new Uri("/applanch;component/App.xaml", UriKind.Relative));
            System.Windows.Application.Current!.Resources.MergedDictionaries.Add(dictionary);
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
