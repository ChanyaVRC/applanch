using System.Security.Cryptography;
using System.Text;
using applanch.Infrastructure.Integration;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Integration;

public sealed class FaviconCacheResolverTests
{
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
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(faviconUri.AbsoluteUri)));
        return Path.Combine(cacheDirectory, $"{hash}.bin");
    }
}
