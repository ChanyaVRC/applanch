using System.Collections.Concurrent;
using System.Net;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using applanch.Infrastructure.Integration;
using applanch.Infrastructure.Storage;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Integration;

public class LaunchItemIconProviderTests
{
    private static readonly byte[] TinyPngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQImWP4////fwAJ+wP9KobjigAAAABJRU5ErkJggg==");

    [Fact]
    public void GetInitialIcon_HttpUrl_ReturnsGenericWebIcon()
    {
        var provider = CreateProvider();

        var icon = provider.GetInitialIcon("https://example.com/path");

        Assert.NotNull(icon);
    }

    [Fact]
    public void GetInitialIcon_FreshDiskCache_ReturnsCachedBitmap()
    {
        RunInSta(() =>
        {
            using var tempDirectory = TemporaryDirectory.Create("applanch-favicon-cache");
            var provider = CreateProvider(cacheDirectory: tempDirectory.Path);
            provider.ApplySettings(new AppSettings());
            WriteCacheFile(tempDirectory.Path, "https://example.com/favicon.ico", TinyPngBytes, DateTime.UtcNow);

            var icon = provider.GetInitialIcon("https://example.com/page");

            Assert.IsAssignableFrom<BitmapFrame>(icon);
        });
    }

    [Fact]
    public void GetDeferredIconAsync_FetchDisabled_ReturnsNullWithoutRequest()
    {
        RunInSta(async () =>
        {
            var handler = new RecordingHttpMessageHandler(_ => throw new InvalidOperationException("network should not be used"));
            var provider = CreateProvider(httpClient: new HttpClient(handler));
            provider.ApplySettings(new AppSettings { FetchHttpIcons = false });

            var icon = await provider.GetDeferredIconAsync("https://example.com/page");

            Assert.Null(icon);
            Assert.Empty(handler.RequestedUris);
        });
    }

    [Fact]
    public void GetDeferredIconAsync_PrivateLiteralBlocked_ReturnsNullWithoutRequest()
    {
        RunInSta(async () =>
        {
            var handler = new RecordingHttpMessageHandler(_ => throw new InvalidOperationException("network should not be used"));
            var provider = CreateProvider(httpClient: new HttpClient(handler));
            provider.ApplySettings(new AppSettings());

            var icon = await provider.GetDeferredIconAsync("http://127.0.0.1/page");

            Assert.Null(icon);
            Assert.Empty(handler.RequestedUris);
        });
    }

    [Fact]
    public void GetDeferredIconAsync_PrivateResolvedHostBlocked_ReturnsNullWithoutRequest()
    {
        RunInSta(async () =>
        {
            var handler = new RecordingHttpMessageHandler(_ => throw new InvalidOperationException("network should not be used"));
            var provider = CreateProvider(
                httpClient: new HttpClient(handler),
                hostAddressResolver: static (_, _) => Task.FromResult<IReadOnlyList<IPAddress>>([IPAddress.Parse("192.168.1.10")]));
            provider.ApplySettings(new AppSettings());

            var icon = await provider.GetDeferredIconAsync("https://intranet.example/page");

            Assert.Null(icon);
            Assert.Empty(handler.RequestedUris);
        });
    }

    [Fact]
    public void GetDeferredIconAsync_IPv4MappedIPv6PrivateAddress_ReturnsNullWithoutRequest()
    {
        RunInSta(async () =>
        {
            var handler = new RecordingHttpMessageHandler(_ => throw new InvalidOperationException("network should not be used"));
            var provider = CreateProvider(
                httpClient: new HttpClient(handler),
                hostAddressResolver: static (_, _) => Task.FromResult<IReadOnlyList<IPAddress>>([IPAddress.Parse("::ffff:192.168.1.10")]));
            provider.ApplySettings(new AppSettings());

            var icon = await provider.GetDeferredIconAsync("https://intranet.example/page");

            Assert.Null(icon);
            Assert.Empty(handler.RequestedUris);
        });
    }

    [Fact]
    public void GetDeferredIconAsync_AllowedPrivateRequest_FetchesIcon()
    {
        RunInSta(async () =>
        {
            using var tempDirectory = TemporaryDirectory.Create("applanch-favicon-private-allowed");
            var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(TinyPngBytes),
            });
            var provider = CreateProvider(httpClient: new HttpClient(handler), cacheDirectory: tempDirectory.Path);
            provider.ApplySettings(new AppSettings { AllowPrivateNetworkHttpIconRequests = true });

            var icon = await provider.GetDeferredIconAsync("http://127.0.0.1/page");

            Assert.IsAssignableFrom<BitmapFrame>(icon);
            Assert.Single(handler.RequestedUris);
        });
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

            using var tempDirectory = TemporaryDirectory.Create("applanch-favicon-redirect");
            var provider = CreateProvider(httpClient: new HttpClient(handler), cacheDirectory: tempDirectory.Path);
            provider.ApplySettings(new AppSettings());

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
            using var tempDirectory = TemporaryDirectory.Create("applanch-favicon-cross-host");
            var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                Headers = { Location = new Uri("https://evil.example/favicon.ico") },
            });

            var provider = CreateProvider(httpClient: new HttpClient(handler), cacheDirectory: tempDirectory.Path);
            provider.ApplySettings(new AppSettings());

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

            var provider = CreateProvider(httpClient: new HttpClient(handler));
            provider.ApplySettings(new AppSettings());

            var icon = await provider.GetDeferredIconAsync("https://example.com/page");

            Assert.Null(icon);
        });
    }

    [Fact]
    public void GetDeferredIconAsync_RefreshesOnce_AndUsesCacheOnSecondCall()
    {
        RunInSta(async () =>
        {
            using var tempDirectory = TemporaryDirectory.Create("applanch-favicon-cache-reuse");
            var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(TinyPngBytes),
            });

            var provider = CreateProvider(httpClient: new HttpClient(handler), cacheDirectory: tempDirectory.Path);
            provider.ApplySettings(new AppSettings());

            var first = await provider.GetDeferredIconAsync("https://example.com/a");
            var second = await provider.GetDeferredIconAsync("https://example.com/b");

            Assert.NotNull(first);
            Assert.Null(second);
            Assert.Single(handler.RequestedUris);
        });
    }

    [Fact]
    public void GetDeferredIconAsync_FreshDiskCache_SkipsNetworkRequest()
    {
        RunInSta(async () =>
        {
            using var tempDirectory = TemporaryDirectory.Create("applanch-favicon-cache-fresh");
            var handler = new RecordingHttpMessageHandler(_ => throw new InvalidOperationException("network should not be used"));
            var provider = CreateProvider(httpClient: new HttpClient(handler), cacheDirectory: tempDirectory.Path);
            provider.ApplySettings(new AppSettings());
            WriteCacheFile(tempDirectory.Path, "https://example.com/favicon.ico", TinyPngBytes, DateTime.UtcNow);

            var icon = await provider.GetDeferredIconAsync("https://example.com/page");

            Assert.Null(icon);
            Assert.Empty(handler.RequestedUris);
        });
    }

    [Fact]
    public void GetDeferredIconAsync_StaleDiskCache_RefreshesAndWritesNewFile()
    {
        RunInSta(async () =>
        {
            using var tempDirectory = TemporaryDirectory.Create("applanch-favicon-cache-stale");
            WriteCacheFile(tempDirectory.Path, "https://example.com/favicon.ico", TinyPngBytes, DateTime.UtcNow.AddDays(-30));
            var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(TinyPngBytes),
            });
            var provider = CreateProvider(httpClient: new HttpClient(handler), cacheDirectory: tempDirectory.Path);
            provider.ApplySettings(new AppSettings());

            var icon = await provider.GetDeferredIconAsync("https://example.com/page");

            Assert.NotNull(icon);
            Assert.Single(handler.RequestedUris);
            var cachePath = GetCacheFilePath(tempDirectory.Path, "https://example.com/favicon.ico");
            Assert.True(File.GetLastWriteTimeUtc(cachePath) > DateTime.UtcNow.AddDays(-1));
        });
    }

    private static LaunchItemIconProvider CreateProvider(
        HttpClient? httpClient = null,
        Func<string, CancellationToken, Task<IReadOnlyList<IPAddress>>>? hostAddressResolver = null,
        string? cacheDirectory = null)
    {
        return new LaunchItemIconProvider(
            httpClient,
            new ConcurrentDictionary<string, Lazy<Task<ImageSource?>>>(StringComparer.OrdinalIgnoreCase),
            hostAddressResolver,
            cacheDirectory);
    }

    private static void WriteCacheFile(string cacheDirectory, string faviconUri, byte[] bytes, DateTime lastWriteUtc)
    {
        Directory.CreateDirectory(cacheDirectory);
        var path = GetCacheFilePath(cacheDirectory, faviconUri);
        File.WriteAllBytes(path, bytes);
        File.SetLastWriteTimeUtc(path, lastWriteUtc);
    }

    private static string GetCacheFilePath(string cacheDirectory, string faviconUri)
    {
        var hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(faviconUri)));
        return Path.Combine(cacheDirectory, $"{hash}.bin");
    }

    private static void RunInSta(Action action)
    {
        Exception? captured = null;
        var completed = new ManualResetEventSlim(false);
        var thread = new Thread(() =>
        {
            try
            {
                action();
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

    private static void RunInSta(Func<Task> action)
    {
        RunInSta(() => action().GetAwaiter().GetResult());
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
