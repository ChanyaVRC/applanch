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
    public void IsNewer_ReturnsExpected(string candidate, string current, bool expected)
    {
        Assert.Equal(expected, GitHubAppUpdateService.IsNewer(SemanticVersion.Parse(candidate), SemanticVersion.Parse(current)));
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
        var service = new GitHubAppUpdateService(client, SemanticVersion.Parse("1.0.0"));

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAvailableUpdatesAsync_ReturnsStableInstallableVersions_ExcludingCurrentVersion()
    {
        var rid = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;
        var handler = new JsonHttpMessageHandler(JsonSerializer.Serialize(new[]
        {
            new
            {
                tag_name = "v2.0.0",
                html_url = "https://github.com/ChanyaVRC/applanch/releases/tag/v2.0.0",
                prerelease = false,
                assets = new[]
                {
                    new
                    {
                        name = $"applanch-2.0.0-{rid}.zip",
                        browser_download_url = $"https://github.com/ChanyaVRC/applanch/releases/download/v2.0.0/applanch-2.0.0-{rid}.zip",
                    },
                },
            },
            new
            {
                tag_name = "v1.5.0-beta.1",
                html_url = "https://github.com/ChanyaVRC/applanch/releases/tag/v1.5.0-beta.1",
                prerelease = true,
                assets = new[]
                {
                    new
                    {
                        name = $"applanch-1.5.0-beta.1-{rid}.zip",
                        browser_download_url = $"https://github.com/ChanyaVRC/applanch/releases/download/v1.5.0-beta.1/applanch-1.5.0-beta.1-{rid}.zip",
                    },
                },
            },
            new
            {
                tag_name = "v1.0.0",
                html_url = "https://github.com/ChanyaVRC/applanch/releases/tag/v1.0.0",
                prerelease = false,
                assets = new[]
                {
                    new
                    {
                        name = $"applanch-1.0.0-{rid}.zip",
                        browser_download_url = $"https://github.com/ChanyaVRC/applanch/releases/download/v1.0.0/applanch-1.0.0-{rid}.zip",
                    },
                },
            },
        }));
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("test/1.0");
        var service = new GitHubAppUpdateService(client, SemanticVersion.Parse("1.0.0"));

        var result = await service.GetAvailableUpdatesAsync();

        Assert.Collection(
            result,
            update => Assert.Equal(SemanticVersion.Parse("2.0.0"), update.NewVersion));
    }

    [Fact]
    public async Task GetAvailableUpdatesAsync_IncludesPrereleaseVersions_WhenAllowed()
    {
        var rid = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;
        var handler = new JsonHttpMessageHandler(JsonSerializer.Serialize(new[]
        {
            new
            {
                tag_name = "v2.0.0-beta.1",
                html_url = "https://github.com/ChanyaVRC/applanch/releases/tag/v2.0.0-beta.1",
                prerelease = true,
                assets = new[]
                {
                    new
                    {
                        name = $"applanch-2.0.0-beta.1-{rid}.zip",
                        browser_download_url = $"https://github.com/ChanyaVRC/applanch/releases/download/v2.0.0-beta.1/applanch-2.0.0-beta.1-{rid}.zip",
                    },
                },
            },
        }));
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("test/1.0");
        var service = new GitHubAppUpdateService(client, SemanticVersion.Parse("1.0.0"), allowPrereleaseUpdates: true);

        var result = await service.GetAvailableUpdatesAsync();

        Assert.Collection(
            result,
            update => Assert.Equal(SemanticVersion.Parse("2.0.0-beta.1"), update.NewVersion));
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
        var service = new GitHubAppUpdateService(client, SemanticVersion.Parse("1.0.0"));

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.Equal(SemanticVersion.Parse("2.0.0"), result.NewVersion);
        Assert.Equal(SemanticVersion.Parse("1.0.0"), result.CurrentVersion);
        Assert.Equal(new Uri($"https://github.com/ChanyaVRC/applanch/releases/download/v2.0.0/applanch-2.0.0-{rid}.zip"), result.AssetDownloadUrl);
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
        var service = new GitHubAppUpdateService(client, SemanticVersion.Parse("1.0.0"));

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
        var service = new GitHubAppUpdateService(client, SemanticVersion.Parse("1.0.0"), debugUpdate: true);

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.Equal(SemanticVersion.Parse("1.0.0"), result.NewVersion);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsPrerelease_WhenAllowed()
    {
        var rid = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;
        var handler = new JsonHttpMessageHandler(JsonSerializer.Serialize(new[]
        {
            new
            {
                tag_name = "v2.0.0-beta.1",
                html_url = "https://github.com/ChanyaVRC/applanch/releases/tag/v2.0.0-beta.1",
                prerelease = true,
                assets = new[]
                {
                    new
                    {
                        name = $"applanch-2.0.0-beta.1-{rid}.zip",
                        browser_download_url = $"https://github.com/ChanyaVRC/applanch/releases/download/v2.0.0-beta.1/applanch-2.0.0-beta.1-{rid}.zip",
                    },
                },
            },
            new
            {
                tag_name = "v1.9.0",
                html_url = "https://github.com/ChanyaVRC/applanch/releases/tag/v1.9.0",
                prerelease = false,
                assets = new[]
                {
                    new
                    {
                        name = $"applanch-1.9.0-{rid}.zip",
                        browser_download_url = $"https://github.com/ChanyaVRC/applanch/releases/download/v1.9.0/applanch-1.9.0-{rid}.zip",
                    },
                },
            },
        }));
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("test/1.0");
        var service = new GitHubAppUpdateService(client, SemanticVersion.Parse("1.0.0"), allowPrereleaseUpdates: true);

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.Equal(SemanticVersion.Parse("2.0.0-beta.1"), result.NewVersion);
    }

    [Fact]
    public async Task CheckForUpdateAsync_RetriesTransientRequestFailure()
    {
        var rid = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;
        var json = JsonSerializer.Serialize(new
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
        });

        using var client = new HttpClient(new FailsThenJsonHandler(1, json));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("test/1.0");
        var service = new GitHubAppUpdateService(client, SemanticVersion.Parse("1.0.0"));

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.Equal(SemanticVersion.Parse("2.0.0"), result.NewVersion);
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
        var service = new GitHubAppUpdateService(client, SemanticVersion.Parse("1.0.0"));

        using var tempDirectory = TemporaryDirectory.Create("applanch-test");

        // Act
        var extractDir = await service.DownloadAndExtractAsync(new Uri("https://example.com/test.zip"), tempDirectory.Path);

        // Assert
        Assert.True(Directory.Exists(extractDir));
        var extractedFile = Path.Combine(extractDir, "hello.txt");
        Assert.True(File.Exists(extractedFile));
        Assert.Equal("hello world", File.ReadAllText(extractedFile));
    }

    [Fact]
    public async Task DownloadAndExtractAsync_RetriesTransientRequestFailure()
    {
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("hello.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("hello world");
        }

        zipStream.Position = 0;
        using var client = new HttpClient(new FailsThenZipHandler(1, zipStream.ToArray()));
        var service = new GitHubAppUpdateService(client, SemanticVersion.Parse("1.0.0"));

        using var tempDirectory = TemporaryDirectory.Create("applanch-test");
        var extractDir = await service.DownloadAndExtractAsync(new Uri("https://example.com/test.zip"), tempDirectory.Path);

        Assert.True(Directory.Exists(extractDir));
        Assert.True(File.Exists(Path.Combine(extractDir, "hello.txt")));
    }

    [Fact]
    public void BuildUpdateScriptLines_WaitsForOldProcess_AndRestartsOnlyAfterSuccessfulCopy()
    {
        var lines = GitHubAppUpdateService.BuildUpdateScriptLines(
            4321,
            @"C:\Apps\applanch\applanch.exe",
            @"C:\Temp\update\extracted",
            @"C:\Apps\applanch",
            @"C:\Temp\update");

        Assert.Contains(":wait_for_exit", lines);
        Assert.Contains("tasklist /FI \"PID eq 4321\" 2>NUL | find \"4321\" >NUL", lines);
        Assert.Contains("if not errorlevel 1 (", lines);
        Assert.Contains("robocopy \"C:\\Temp\\update\\extracted\" \"C:\\Apps\\applanch\" /e /r:5 /w:1 /nfl /ndl /njh /njs /nc /ns /np > nul", lines);
        Assert.Contains("if errorlevel 8 exit /b %errorlevel%", lines);
        Assert.Contains("start \"\" \"C:\\Apps\\applanch\\applanch.exe\"", lines);
        Assert.Contains("rmdir /s /q \"C:\\Temp\\update\"", lines);
    }

    private sealed class FailsThenJsonHandler(int failuresBeforeSuccess, string responseJson) : HttpMessageHandler
    {
        private int _remainingFailures = failuresBeforeSuccess;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_remainingFailures-- > 0)
            {
                throw new HttpRequestException("transient");
            }

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json"),
            };

            return Task.FromResult(response);
        }
    }

    private sealed class FailsThenZipHandler(int failuresBeforeSuccess, byte[] zipBytes) : HttpMessageHandler
    {
        private int _remainingFailures = failuresBeforeSuccess;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_remainingFailures-- > 0)
            {
                throw new HttpRequestException("transient");
            }

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(zipBytes),
            };

            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            return Task.FromResult(response);
        }
    }
}


