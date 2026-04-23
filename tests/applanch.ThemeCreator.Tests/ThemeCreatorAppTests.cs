using applanch.ThemeCreator;
using System.Text.Json.Nodes;
using Xunit;

namespace applanch.Tests.ThemeCreator;

public class ThemeCreatorAppTests
{
    [Fact]
    public void Run_CliMode_WritesLogAndCreatesThemeFile()
    {
        var logDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(logDirectory, "source.json");
        var outputPath = Path.Combine(logDirectory, "generated-theme.json");
        var output = new StringWriter();
        var error = new StringWriter();

        Directory.CreateDirectory(logDirectory);
        File.WriteAllText(sourcePath, SourcePalette);

        try
        {
            var exitCode = ThemeCreatorApp.Run(
                [
                    "--id", "ocean",
                    "--base", "light",
                    "--source", sourcePath,
                    "--output", outputPath,
                    "--name-en", "Ocean"
                ],
                output,
                error);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.Contains(outputPath, output.ToString(), StringComparison.Ordinal);
            Assert.True(File.Exists(outputPath));

            var logPath = ThemeCreatorLogger.LogFilePath;
            Assert.True(File.Exists(logPath));

            using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            var logContents = reader.ReadToEnd();
            Assert.Contains("Theme creator started. Mode=CLI", logContents, StringComparison.Ordinal);
            Assert.Contains("CLI theme created successfully.", logContents, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(logDirectory))
            {
                Directory.Delete(logDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void Run_CliMode_AcceptsDynamicNameArguments()
    {
        var logDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(logDirectory, "source.json");
        var outputPath = Path.Combine(logDirectory, "generated-theme.json");
        var output = new StringWriter();
        var error = new StringWriter();

        Directory.CreateDirectory(logDirectory);
        File.WriteAllText(sourcePath, SourcePalette);

        try
        {
            var exitCode = ThemeCreatorApp.Run(
              [
                "--id", "ocean",
            "--base", "light",
            "--source", sourcePath,
            "--output", outputPath,
            "--name-en", "Ocean",
            "--name-ja", "オーシャン"
              ],
              output,
              error);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());

            var root = JsonNode.Parse(File.ReadAllText(outputPath))!.AsObject();
            var theme = root["themes"]!.AsArray()[0]!.AsObject();
            Assert.Equal("Ocean", theme["displayNames"]!["en"]!.GetValue<string>());
            Assert.Equal("オーシャン", theme["displayNames"]!["ja"]!.GetValue<string>());
        }
        finally
        {
            if (Directory.Exists(logDirectory))
            {
                Directory.Delete(logDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void Run_CliMode_RejectsUnknownNameLanguage()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = ThemeCreatorApp.Run(
          [
            "--id", "ocean",
          "--output", "theme.json",
          "--name-zz", "Unknown"
          ],
          output,
          error);

        Assert.Equal(1, exitCode);
        Assert.Contains("--name-zz", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Run_CliMode_BaseNone_OmitsEntriesFrom()
    {
        var logDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(logDirectory, "source.json");
        var outputPath = Path.Combine(logDirectory, "generated-theme.json");
        var output = new StringWriter();
        var error = new StringWriter();

        Directory.CreateDirectory(logDirectory);
        File.WriteAllText(sourcePath, SourcePalette);

        try
        {
            var exitCode = ThemeCreatorApp.Run(
              [
                "--id", "plain",
              "--base", "none",
              "--source", sourcePath,
              "--output", outputPath,
              "--no-entries"
              ],
              output,
              error);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());

            var root = JsonNode.Parse(File.ReadAllText(outputPath))!.AsObject();
            var theme = root["themes"]!.AsArray()[0]!.AsObject();
            Assert.Null(theme["entriesFrom"]);
            Assert.Null(theme["entries"]);
        }
        finally
        {
            if (Directory.Exists(logDirectory))
            {
                Directory.Delete(logDirectory, recursive: true);
            }
        }
    }

    private const string SourcePalette = """
{
  "themes": [
    {
      "id": "light",
      "entries": [
        { "key": "Brush.AppBackground", "hex": "#FFFFFF" }
      ]
    }
  ]
}
""";
}