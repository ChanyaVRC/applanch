using System.Text.Json;
using applanch.Serialization;
using Xunit;

namespace applanch.Tests.Serialization;

public class JsonConfigLoaderTests
{
    [Fact]
    public void Deserialize_UsesDefaultOptions_ForCommentsAndTrailingCommas()
    {
        var json = """
{
  // comment
  "name": "sample",
}
""";

        var value = JsonConfigLoader.Deserialize<SampleConfig>(json);

        Assert.Equal("sample", value.Name);
    }

    [Fact]
    public void SerializeFile_CreatesDirectory_And_WritesJson()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "nested");
        var filePath = Path.Combine(directoryPath, "sample.json");

        try
        {
            JsonConfigLoader.SerializeFile(
                filePath,
                new SampleConfig { Name = "sample" },
                new JsonSerializerOptions(JsonConfigLoader.DefaultSerializerOptions)
                {
                    WriteIndented = true
                });

            Assert.True(File.Exists(filePath));

            var value = JsonConfigLoader.DeserializeFile<SampleConfig>(filePath);
            Assert.Equal("sample", value.Name);
        }
        finally
        {
            var rootDirectory = Path.GetDirectoryName(Path.GetDirectoryName(filePath)!);
            if (!string.IsNullOrWhiteSpace(rootDirectory) && Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    private sealed class SampleConfig
    {
        public string? Name { get; init; }
    }
}