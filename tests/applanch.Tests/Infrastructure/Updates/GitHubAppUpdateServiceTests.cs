using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using applanch.Infrastructure.Updates;

namespace applanch.Tests.Infrastructure.Updates;

public class GitHubAppUpdateServiceTests
{
    [Theory]
    [InlineData("1.0.1", "1.0.0", true)]
    [InlineData("2.0.0", "1.9.9", true)]
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("0.9.0", "1.0.0", false)]
    [InlineData("1.0.0-rc1", "1.0.0", false)]
    [InlineData("1.0.0", "1.0.0-rc1", true)]
    [InlineData("invalid", "1.0.0", false)]
    [InlineData("1.0.0", "invalid", false)]
    public void IsNewer_ReturnsExpected(string candidate, string current, bool expected)
    {
        Assert.Equal(expected, GitHubAppUpdateService.IsNewer(candidate, current));
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsNull_WhenCurrentIsLatest()
    {
        var handler = new FakeHandler(JsonSerializer.Serialize(new
        {
            tag_name = "v1.0.0",
            html_url = "https://github.com/ChanyaVRC/applanch/releases/tag/v1.0.0",
            assets = Array.Empty<object>(),
        }));
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("test/1.0");
        var service = new GitHubAppUpdateService(client, "1.0.0");

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsUpdate_WhenNewerVersionAvailable()
    {
        var rid = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;
        var handler = new FakeHandler(JsonSerializer.Serialize(new
        {
            tag_name = "v2.0.0",
            html_url = "https://github.com/ChanyaVRC/applanch/releases/tag/v2.0.0",
            assets = new[]
            {
                new
                {
                    name = $"applanch-2.0.0-{rid}.zip",
                    browser_download_url = $"https://github.com/ChanyaVRC/applanch/releases/download/v2.0.0/applanch-2.0.0-{rid}.zip",
                },
            },
        }));
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("test/1.0");
        var service = new GitHubAppUpdateService(client, "1.0.0");

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.Equal("2.0.0", result.NewVersion);
        Assert.Equal("1.0.0", result.CurrentVersion);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsNull_WhenNoMatchingAsset()
    {
        var handler = new FakeHandler(JsonSerializer.Serialize(new
        {
            tag_name = "v2.0.0",
            html_url = "https://github.com/ChanyaVRC/applanch/releases/tag/v2.0.0",
            assets = new[]
            {
                new
                {
                    name = "applanch-2.0.0-linux-x64.zip",
                    browser_download_url = "https://example.com/download",
                },
            },
        }));
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("test/1.0");
        var service = new GitHubAppUpdateService(client, "1.0.0");

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsUpdate_WhenDebugUpdateEnabled_EvenIfSameVersion()
    {
        var rid = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;
        var handler = new FakeHandler(JsonSerializer.Serialize(new
        {
            tag_name = "v1.0.0",
            html_url = "https://github.com/ChanyaVRC/applanch/releases/tag/v1.0.0",
            assets = new[]
            {
                new
                {
                    name = $"applanch-1.0.0-{rid}.zip",
                    browser_download_url = $"https://github.com/ChanyaVRC/applanch/releases/download/v1.0.0/applanch-1.0.0-{rid}.zip",
                },
            },
        }));
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("test/1.0");
        var service = new GitHubAppUpdateService(client, "1.0.0", debugUpdate: true);

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.Equal("1.0.0", result.NewVersion);
    }

    [Fact]
    public async Task DownloadAndExtractAsync_ExtractsFilesFromZip()
    {
        // Arrange: create a valid ZIP in memory
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("hello.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("hello world");
        }
        zipStream.Position = 0;

        var handler = new ZipHandler(zipStream.ToArray());
        using var client = new HttpClient(handler);
        var service = new GitHubAppUpdateService(client, "1.0.0");

        var tempDir = Path.Combine(Path.GetTempPath(), $"applanch-test-{Guid.NewGuid():N}");
        try
        {
            // Act
            var extractDir = await service.DownloadAndExtractAsync("https://example.com/test.zip", tempDir);

            // Assert
            Assert.True(Directory.Exists(extractDir));
            var extractedFile = Path.Combine(extractDir, "hello.txt");
            Assert.True(File.Exists(extractedFile));
            Assert.Equal("hello world", File.ReadAllText(extractedFile));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    private sealed class FakeHandler(string responseJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }

    private sealed class ZipHandler(byte[] zipBytes) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(zipBytes),
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            return Task.FromResult(response);
        }
    }
}


