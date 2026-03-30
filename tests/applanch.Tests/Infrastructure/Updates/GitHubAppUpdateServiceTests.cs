using System.IO.Compression;
using System.Text.Json;
using Xunit;
using applanch.Infrastructure.Updates;
using applanch.Tests.Infrastructure.Updates.TestDoubles;
using applanch.Tests.TestSupport;

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
        var handler = new JsonHttpMessageHandler(JsonSerializer.Serialize(new
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
        var handler = new JsonHttpMessageHandler(JsonSerializer.Serialize(new
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
        var handler = new JsonHttpMessageHandler(JsonSerializer.Serialize(new
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
        var handler = new JsonHttpMessageHandler(JsonSerializer.Serialize(new
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

        var handler = new ZipHttpMessageHandler(zipStream.ToArray());
        using var client = new HttpClient(handler);
        var service = new GitHubAppUpdateService(client, "1.0.0");

        using var tempDirectory = TemporaryDirectory.Create("applanch-test");

        // Act
        var extractDir = await service.DownloadAndExtractAsync("https://example.com/test.zip", tempDirectory.Path);

        // Assert
        Assert.True(Directory.Exists(extractDir));
        var extractedFile = Path.Combine(extractDir, "hello.txt");
        Assert.True(File.Exists(extractedFile));
        Assert.Equal("hello world", File.ReadAllText(extractedFile));
    }
}


