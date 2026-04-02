using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

    internal LaunchItemIconProvider(
        HttpClient? httpClient = null,
        ConcurrentDictionary<string, Lazy<Task<ImageSource?>>>? faviconCache = null)
    {
        _httpClient = httpClient ?? SharedHttpClient;
        _faviconCache = faviconCache ?? new ConcurrentDictionary<string, Lazy<Task<ImageSource?>>>(StringComparer.OrdinalIgnoreCase);
    }

    public ImageSource? GetInitialIcon(string fullPath)
    {
        return PathNormalization.IsHttpUrl(fullPath)
            ? GenericWebIcon
            : GetShellIcon(fullPath);
    }

    public ValueTask<ImageSource?> GetDeferredIconAsync(string fullPath)
    {
        if (!PathNormalization.TryParseHttpUrl(fullPath, out var pageUri))
        {
            return ValueTask.FromResult<ImageSource?>(null);
        }

        var faviconUri = CreateFaviconUri(pageUri);
        var cacheEntry = _faviconCache.GetOrAdd(
            faviconUri.AbsoluteUri,
            key => new Lazy<Task<ImageSource?>>(
                () => LoadFaviconAsync(new Uri(key, UriKind.Absolute)),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return new ValueTask<ImageSource?>(cacheEntry.Value);
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

    private async Task<ImageSource?> LoadFaviconAsync(Uri faviconUri)
    {
        try
        {
            var currentUri = faviconUri;
            for (var redirectCount = 0; redirectCount <= MaxRedirectCount; redirectCount++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                if (IsRedirect(response.StatusCode))
                {
                    if (!TryResolveRedirectTarget(faviconUri, currentUri, response.Headers.Location, out var redirectedUri))
                    {
                        return null;
                    }

                    currentUri = redirectedUri;
                    continue;
                }

                if (!response.IsSuccessStatusCode || IsSvg(response.Content.Headers.ContentType?.MediaType))
                {
                    return null;
                }

                if (response.Content.Headers.ContentLength is > MaxFaviconBytes)
                {
                    return null;
                }

                await using var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var buffer = new MemoryStream();
                var readBuffer = new byte[81920];
                while (true)
                {
                    var bytesRead = await responseStream.ReadAsync(readBuffer).ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    buffer.Write(readBuffer, 0, bytesRead);
                    if (buffer.Length > MaxFaviconBytes)
                    {
                        return null;
                    }
                }

                buffer.Position = 0;
                return DecodeImage(buffer);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Failed to load favicon from '{faviconUri}': {ex.Message}");
        }

        return null;
    }

    private static BitmapFrame? DecodeImage(Stream stream)
    {
        try
        {
            var bitmap = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Failed to decode favicon: {ex.Message}");
            return null;
        }
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;
    }

    private static bool IsSvg(string? mediaType)
    {
        return mediaType?.Contains("svg", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool TryResolveRedirectTarget(Uri originalUri, Uri currentUri, Uri? location, out Uri redirectedUri)
    {
        redirectedUri = null!;
        if (location is null)
        {
            return false;
        }

        redirectedUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
        if (!string.Equals(redirectedUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(redirectedUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(redirectedUri.IdnHost, originalUri.IdnHost, StringComparison.OrdinalIgnoreCase);
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