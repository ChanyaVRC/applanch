using System.Collections.Concurrent;
using System.Net;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using applanch.Infrastructure.Integration;
using Xunit;

namespace applanch.Tests.Infrastructure.Integration;

public class LaunchItemIconProviderTests
{
    private static readonly byte[] TinyPngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQImWP4////fwAJ+wP9KobjigAAAABJRU5ErkJggg==");

    [Fact]
    public void GetInitialIcon_HttpUrl_ReturnsGenericWebIcon()
    {
        var provider = new LaunchItemIconProvider(new HttpClient(new RecordingHttpMessageHandler(_ => throw new InvalidOperationException())));

        var icon = provider.GetInitialIcon("https://example.com/path");

        Assert.NotNull(icon);
    }

    [Fact]
    public void GetDeferredIconAsync_SameHostRedirect_ReturnsDecodedBitmap()
    {
        RunInSta(async () =>
        {
            var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsoluteUri == "http://example.com/favicon.ico")
                {
                    return new HttpResponseMessage(HttpStatusCode.MovedPermanently)
                    {
                        Headers = { Location = new Uri("https://example.com/favicon.ico") },
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(TinyPngBytes),
                };
            });

            var provider = new LaunchItemIconProvider(new HttpClient(handler), new ConcurrentDictionary<string, Lazy<Task<ImageSource?>>>());

            var icon = await provider.GetDeferredIconAsync("http://example.com/page");

            Assert.IsAssignableFrom<BitmapFrame>(icon);
            Assert.Equal(new[]
            {
                "http://example.com/favicon.ico",
                "https://example.com/favicon.ico",
            }, handler.RequestedUris.Select(static uri => uri.AbsoluteUri));
        });
    }

    [Fact]
    public void GetDeferredIconAsync_CrossHostRedirect_ReturnsNull()
    {
        RunInSta(async () =>
        {
            var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                Headers = { Location = new Uri("https://evil.example/favicon.ico") },
            });

            var provider = new LaunchItemIconProvider(new HttpClient(handler), new ConcurrentDictionary<string, Lazy<Task<ImageSource?>>>());

            var icon = await provider.GetDeferredIconAsync("https://example.com/page");

            Assert.Null(icon);
            Assert.Single(handler.RequestedUris);
        });
    }

    [Fact]
    public void GetDeferredIconAsync_OversizedPayload_ReturnsNull()
    {
        RunInSta(async () =>
        {
            var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[300 * 1024]),
            });

            var provider = new LaunchItemIconProvider(new HttpClient(handler), new ConcurrentDictionary<string, Lazy<Task<ImageSource?>>>());

            var icon = await provider.GetDeferredIconAsync("https://example.com/page");

            Assert.Null(icon);
        });
    }

    [Fact]
    public void GetDeferredIconAsync_ReusesCachedResult()
    {
        RunInSta(async () =>
        {
            var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(TinyPngBytes),
            });

            var provider = new LaunchItemIconProvider(new HttpClient(handler), new ConcurrentDictionary<string, Lazy<Task<ImageSource?>>>());

            var first = await provider.GetDeferredIconAsync("https://example.com/a");
            var second = await provider.GetDeferredIconAsync("https://example.com/b");

            Assert.NotNull(first);
            Assert.Same(first, second);
            Assert.Single(handler.RequestedUris);
        });
    }

    private static void RunInSta(Func<Task> action)
    {
        Exception? captured = null;
        var completed = new ManualResetEventSlim(false);
        var thread = new Thread(() =>
        {
            try
            {
                action().GetAwaiter().GetResult();
                DrainDispatcher();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
            finally
            {
                completed.Set();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        completed.Wait();

        if (captured is not null)
        {
            throw new Xunit.Sdk.XunitException($"STA test failed: {captured}");
        }
    }

    private static void DrainDispatcher()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private sealed class RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        internal List<Uri> RequestedUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUris.Add(request.RequestUri!);
            return Task.FromResult(responder(request));
        }
    }
}