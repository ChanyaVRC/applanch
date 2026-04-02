using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Integration;

internal sealed class FaviconCacheResolver : IFaviconCacheResolver
{
    private static readonly TimeSpan DiskCacheTtl = TimeSpan.FromDays(14);

    private readonly string _cacheDirectory;

    internal FaviconCacheResolver(string? cacheDirectory = null)
    {
        _cacheDirectory = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "applanch",
            "Cache",
            "Favicons");
    }

    public BitmapFrame? TryLoad(Uri faviconUri, bool acceptExpired)
    {
        try
        {
            var path = GetFilePath(faviconUri);
            if (!File.Exists(path))
            {
                return null;
            }

            var lastWrite = File.GetLastWriteTimeUtc(path);
            if (!acceptExpired && DateTime.UtcNow - lastWrite > DiskCacheTtl)
            {
                return null;
            }

            var bytes = File.ReadAllBytes(path);
            return DecodeImage(bytes);
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Failed to read favicon cache for '{faviconUri}': {ex.Message}");
            return null;
        }
    }

    public void TryWrite(Uri faviconUri, byte[] payload)
    {
        try
        {
            Directory.CreateDirectory(_cacheDirectory);
            var path = GetFilePath(faviconUri);
            var tempPath = path + ".tmp";
            File.WriteAllBytes(tempPath, payload);
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Failed to write favicon cache for '{faviconUri}': {ex.Message}");
        }
    }

    internal static BitmapFrame? DecodeImage(byte[] payload)
    {
        try
        {
            using var stream = new MemoryStream(payload, writable: false);
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

    private string GetFilePath(Uri faviconUri)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(faviconUri.AbsoluteUri)));
        return Path.Combine(_cacheDirectory, $"{hash}.bin");
    }
}
