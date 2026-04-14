using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using applanch.Infrastructure.Integration;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Integration;

public sealed class FaviconCacheResolverTests
{
    [Fact]
    public void TryLoad_WhenCachePayloadIsCorrupted_QuarantinesBadFile()
    {
        using var tempDirectory = TemporaryDirectory.Create("applanch-favicon-cache-tests");
        var resolver = new FaviconCacheResolver(tempDirectory.Path);
        var faviconUri = new Uri("https://example.com/favicon.ico");

        var cachePath = GetCacheFilePath(tempDirectory.Path, faviconUri);
        Directory.CreateDirectory(tempDirectory.Path);
        File.WriteAllBytes(cachePath, [0, 1, 2, 3, 4]);

        var loaded = resolver.TryLoad(faviconUri, acceptExpired: true);

        Assert.Null(loaded);
        Assert.False(File.Exists(cachePath));
        Assert.True(File.Exists(cachePath + ".bad"));
    }

    [Fact]
    public void TryWrite_WhenDestinationFileIsLocked_DoesNotLeaveTempFile()
    {
        using var tempDirectory = TemporaryDirectory.Create("applanch-favicon-cache-tests");
        var resolver = new FaviconCacheResolver(tempDirectory.Path);
        var faviconUri = new Uri("https://example.com/favicon.ico");

        var cachePath = GetCacheFilePath(tempDirectory.Path, faviconUri);
        Directory.CreateDirectory(tempDirectory.Path);
        File.WriteAllBytes(cachePath, [1, 2, 3]);

        using var destinationLock = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        resolver.TryWrite(faviconUri, [9, 8, 7]);

        Assert.False(File.Exists(cachePath + ".tmp"));
    }

    private static string GetCacheFilePath(string cacheDirectory, Uri faviconUri)
    {
        var byteCount = Encoding.UTF8.GetByteCount(faviconUri.AbsoluteUri);
        var rentedBytes = ArrayPool<byte>.Shared.Rent(byteCount);
        string hash;

        try
        {
            var writtenByteCount = Encoding.UTF8.GetBytes(faviconUri.AbsoluteUri.AsSpan(), rentedBytes);
            Span<byte> hashBytes = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(rentedBytes.AsSpan(0, writtenByteCount), hashBytes);
            hash = Convert.ToHexStringLower(hashBytes);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBytes);
        }

        return Path.Combine(cacheDirectory, $"{hash}.bin");
    }
}
