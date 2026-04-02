using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Integration;

internal sealed class LaunchItemIconProvider : ILaunchItemIconProvider
{
    private const int ShellIconSize = 32;
    private const int MaxRedirectCount = 3;
    private const int MaxFaviconBytes = 256 * 1024;
    private static readonly TimeSpan FaviconTimeout = TimeSpan.FromSeconds(3);
    private static readonly DrawingImage GenericWebIcon = CreateGenericWebIcon();
    private static readonly HttpClient SharedHttpClient = CreateHttpClient();

    internal static ILaunchItemIconProvider Shared { get; } = new LaunchItemIconProvider();

    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, Lazy<Task<ImageSource?>>> _faviconCache;
    private readonly INetworkPolicyResolver _networkPolicy;
    private readonly IFaviconCacheResolver _diskCache;

    internal LaunchItemIconProvider(
        HttpClient? httpClient = null,
        ConcurrentDictionary<string, Lazy<Task<ImageSource?>>>? faviconCache = null,
        INetworkPolicyResolver? networkPolicy = null,
        IFaviconCacheResolver? diskCache = null)
    {
        _httpClient = httpClient ?? SharedHttpClient;
        _faviconCache = faviconCache ?? new ConcurrentDictionary<string, Lazy<Task<ImageSource?>>>(StringComparer.OrdinalIgnoreCase);
        _networkPolicy = networkPolicy ?? new NetworkPolicyResolver();
        _diskCache = diskCache ?? new FaviconCacheResolver();
    }

    public void ApplySettings(AppSettings settings)
    {
        _networkPolicy.ApplySettings(settings);
    }

    public ImageSource? GetInitialIcon(string fullPath)
    {
        if (!TryGetHttpPageUri(fullPath, out var pageUri))
        {
            return GetShellIcon(fullPath);
        }

        if (!_networkPolicy.ShouldRequestFavicon(pageUri))
        {
            return GenericWebIcon;
        }

        var faviconUri = CreateFaviconUri(pageUri);
        return (ImageSource?)_diskCache.TryLoad(faviconUri, acceptExpired: true)
            ?? GenericWebIcon;
    }

    public async ValueTask<ImageSource?> GetDeferredIconAsync(string fullPath)
    {
        if (!TryGetHttpPageUri(fullPath, out var pageUri) || !_networkPolicy.ShouldRequestFavicon(pageUri))
        {
            return null;
        }

        if (!await _networkPolicy.IsDestinationAllowedAsync(pageUri).ConfigureAwait(false))
        {
            return null;
        }

        var faviconUri = CreateFaviconUri(pageUri);
        if (_diskCache.TryLoad(faviconUri, acceptExpired: false) is not null)
        {
            return null;
        }

        var cacheEntry = _faviconCache.GetOrAdd(
            faviconUri.AbsoluteUri,
            key => new Lazy<Task<ImageSource?>>(
                () => LoadAndCacheFaviconAsync(new Uri(key, UriKind.Absolute)),
                LazyThreadSafetyMode.ExecutionAndPublication));

        var icon = await cacheEntry.Value.ConfigureAwait(false);
        if (icon is null)
        {
            _faviconCache.TryRemove(faviconUri.AbsoluteUri, out _);
        }

        return icon;
    }

    private static bool TryGetHttpPageUri(string fullPath, out Uri pageUri)
    {
        pageUri = default!;
        var pathType = PathNormalization.GetPathType(fullPath, out var parsedUri);
        if (pathType is not PathType.HttpUrl || parsedUri is null)
        {
            return false;
        }

        pageUri = parsedUri;
        return true;
    }

    internal static Uri CreateFaviconUri(Uri pageUri)
    {
        return new UriBuilder(pageUri.Scheme, pageUri.Host, pageUri.IsDefaultPort ? -1 : pageUri.Port)
        {
            Path = "/favicon.ico",
            Query = string.Empty,
            Fragment = string.Empty,
        }.Uri;
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            UseCookies = false,
        };

        var client = new HttpClient(handler)
        {
            Timeout = FaviconTimeout,
        };

        client.DefaultRequestHeaders.Accept.ParseAdd("image/*,*/*;q=0.1");
        return client;
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;
    }

    private static bool IsSvg(string? mediaType)
    {
        return mediaType?.Contains("svg", StringComparison.OrdinalIgnoreCase) == true;
    }

    private async Task<ImageSource?> LoadAndCacheFaviconAsync(Uri faviconUri)
    {
        try
        {
            var payload = await FetchFaviconPayloadAsync(faviconUri).ConfigureAwait(false);
            if (payload is null)
            {
                return null;
            }

            var decoded = FaviconCacheResolver.DecodeImage(payload);
            if (decoded is null)
            {
                return null;
            }

            _diskCache.TryWrite(faviconUri, payload);
            return decoded;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Failed to load favicon from '{faviconUri}': {ex.Message}");
        }

        return null;
    }

    private async Task<byte[]?> FetchFaviconPayloadAsync(Uri faviconUri)
    {
        var currentUri = faviconUri;
        for (var redirectCount = 0; redirectCount <= MaxRedirectCount; redirectCount++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (IsRedirect(response.StatusCode))
            {
                var redirected = await _networkPolicy.ResolveAllowedRedirectAsync(faviconUri, currentUri, response.Headers.Location).ConfigureAwait(false);
                if (redirected is null)
                {
                    return null;
                }

                currentUri = redirected;
                continue;
            }

            if (!response.IsSuccessStatusCode
                || IsSvg(response.Content.Headers.ContentType?.MediaType)
                || response.Content.Headers.ContentLength > MaxFaviconBytes)
            {
                return null;
            }

            return await ReadLimitedContentAsync(response.Content).ConfigureAwait(false);
        }

        return null;
    }

    private static async Task<byte[]?> ReadLimitedContentAsync(HttpContent content)
    {
        await using var responseStream = await content.ReadAsStreamAsync().ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var readBuffer = new byte[81920];
        while (true)
        {
            var bytesRead = await responseStream.ReadAsync(readBuffer).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                return buffer.ToArray();
            }

            buffer.Write(readBuffer, 0, bytesRead);
            if (buffer.Length > MaxFaviconBytes)
            {
                return null;
            }
        }
    }

    private static BitmapSource? GetShellIcon(string fullPath)
    {
        var shfi = new NativeMethods.SHFILEINFO();

        try
        {
            var result = NativeMethods.SHGetFileInfo(fullPath, 0, ref shfi,
                (uint)Marshal.SizeOf<NativeMethods.SHFILEINFO>(),
                NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON);

            if (result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero)
            {
                if (Path.Exists(fullPath))
                {
                    AppLogger.Instance.Warn($"Icon was not found for '{fullPath}'.");
                }

                return null;
            }

            var bitmap = Imaging.CreateBitmapSourceFromHIcon(
                shfi.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(ShellIconSize, ShellIconSize));
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, $"Failed to load icon for '{fullPath}'");
            return null;
        }
        finally
        {
            if (shfi.hIcon != IntPtr.Zero)
            {
                NativeMethods.DestroyIcon(shfi.hIcon);
            }
        }
    }

    private static DrawingImage CreateGenericWebIcon()
    {
        var strokeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F6B6D")!);
        strokeBrush.Freeze();

        var outerCircle = new EllipseGeometry(new Point(16, 16), 11.5, 11.5);
        var innerVertical = new EllipseGeometry(new Point(16, 16), 4.5, 11.5);
        var innerHorizontal = Geometry.Parse("M4.5,16 L27.5,16 M6.5,10.5 L25.5,10.5 M6.5,21.5 L25.5,21.5");
        outerCircle.Freeze();
        innerVertical.Freeze();
        innerHorizontal.Freeze();

        var group = new DrawingGroup();
        using (var context = group.Open())
        {
            var pen = new Pen(strokeBrush, 1.6);
            pen.Freeze();
            context.DrawGeometry(null, pen, outerCircle);
            context.DrawGeometry(null, pen, innerVertical);
            context.DrawGeometry(null, pen, innerHorizontal);
        }

        group.Freeze();
        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }
}
