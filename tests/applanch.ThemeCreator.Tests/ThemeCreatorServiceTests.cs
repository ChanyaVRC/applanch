using System.Text.Json.Nodes;
using applanch.Localization;
using applanch.ThemeCreator;
using applanch.Theming;
using Xunit;

namespace applanch.Tests.ThemeCreator;

public class ThemeCreatorServiceTests
{
    [Fact]
    public void GetThemeIds_ReturnsThemeIdsInOrder()
    {
        var service = new ThemeCreatorService();

        var ids = service.GetThemeIds(SourcePalette);

        Assert.Equal(["light", "dark"], ids);
    }

    [Fact]
    public void GetThemeIds_AllowsCommentsAndTrailingCommas()
    {
        var service = new ThemeCreatorService();

        var ids = service.GetThemeIds(SourcePaletteWithComments);

        Assert.Equal(["light", "dark"], ids);
    }

    [Fact]
    public void GetEntries_ReturnsClonedEntriesForSelectedBaseTheme()
    {
        var service = new ThemeCreatorService();

        var entries = service.GetEntries(SourcePalette, "light");

        Assert.Equal(2, entries.Count);
        Assert.Equal("Brush.AppBackground", entries[0].Key);
        Assert.Equal("#FFFFFF", entries[0].Hex.Hex);
    }

    [Fact]
    public void CreateThemeJson_CopiesEntriesFromBaseTheme()
    {
        var service = new ThemeCreatorService();
        var options = new ThemeCreatorOptions(
            "ocean",
            "ignored.json",
            "light",
            "ignored-source.json",
          new Dictionary<LanguageOption, string>
          {
              [LanguageOption.English] = "Ocean",
              [LanguageOption.Japanese] = "オーシャン",
          },
            IncludeEntries: true);

        var json = service.CreateThemeJson(SourcePalette, options);

        var root = JsonNode.Parse(json)!.AsObject();
        var theme = root["themes"]!.AsArray()[0]!.AsObject();

        Assert.Equal("ocean", theme["id"]!.GetValue<string>());
        Assert.Equal("light", theme["entriesFrom"]!.GetValue<string>());
        Assert.Equal("Ocean", theme["displayNames"]!["en"]!.GetValue<string>());
        Assert.Equal("オーシャン", theme["displayNames"]!["ja"]!.GetValue<string>());
        Assert.NotNull(theme["entries"]);
        Assert.Equal(2, theme["entries"]!.AsArray().Count);
    }

    [Fact]
    public void CreateThemeJson_NoEntriesOption_OmitsEntries()
    {
        var service = new ThemeCreatorService();
        var options = new ThemeCreatorOptions(
            "mono",
            "ignored.json",
            "dark",
            "ignored-source.json",
            null,
            IncludeEntries: false);

        var json = service.CreateThemeJson(SourcePalette, options);

        var root = JsonNode.Parse(json)!.AsObject();
        var theme = root["themes"]!.AsArray()[0]!.AsObject();

        Assert.Equal("mono", theme["id"]!.GetValue<string>());
        Assert.Equal("dark", theme["entriesFrom"]!.GetValue<string>());
        Assert.Null(theme["entries"]);
        Assert.Null(theme["displayNames"]);
    }

    [Fact]
    public void CreateThemeJson_NoBaseTheme_OmitsEntriesFrom()
    {
        var service = new ThemeCreatorService();
        var options = new ThemeCreatorOptions(
          "standalone",
          "ignored.json",
          null,
          "ignored-source.json",
          null,
          IncludeEntries: false);

        var json = service.CreateThemeJson(SourcePalette, options);

        var root = JsonNode.Parse(json)!.AsObject();
        var theme = root["themes"]!.AsArray()[0]!.AsObject();

        Assert.Equal("standalone", theme["id"]!.GetValue<string>());
        Assert.Null(theme["entriesFrom"]);
        Assert.Null(theme["entries"]);
    }

    [Fact]
    public void CreateThemeJson_UsesEditedEntries_WhenProvided()
    {
        var service = new ThemeCreatorService();
        var options = new ThemeCreatorOptions(
          "customized",
          "ignored.json",
          "light",
          "ignored-source.json",
          null,
          IncludeEntries: true,
          Entries:
          [
            new ThemeEntryDto("Brush.AppBackground", ThemeColor.Parse("#ABCDEF"))
          ]);

        var json = service.CreateThemeJson(SourcePalette, options);

        var root = JsonNode.Parse(json)!.AsObject();
        var entries = root["themes"]!.AsArray()[0]!["entries"]!.AsArray();

        Assert.Single(entries);
        Assert.Equal("#ABCDEF", entries[0]!["hex"]!.GetValue<string>());
    }

    [Fact]
    public void CreateThemeJson_UnknownBaseTheme_Throws()
    {
        var service = new ThemeCreatorService();
        var options = new ThemeCreatorOptions(
            "mono",
            "ignored.json",
            "unknown",
            "ignored-source.json",
            null,
            IncludeEntries: true);

        var ex = Assert.Throws<InvalidOperationException>(() => service.CreateThemeJson(SourcePalette, options));

        Assert.Contains("unknown", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateThemeJsonFromFile_NoSourcePalette_CreatesStandaloneTheme()
    {
        var service = new ThemeCreatorService();
        var options = new ThemeCreatorOptions(
          "scratch",
          "ignored.json",
          null,
          null,
          null,
          IncludeEntries: false);

        var json = service.CreateThemeJsonFromFile(options);

        var root = JsonNode.Parse(json)!.AsObject();
        var theme = root["themes"]!.AsArray()[0]!.AsObject();
        Assert.Equal("scratch", theme["id"]!.GetValue<string>());
        Assert.Null(theme["entriesFrom"]);
        Assert.Null(theme["entries"]);
    }

    [Fact]
    public void CreateThemeJsonFromFile_NoSourcePalette_WithBaseTheme_Throws()
    {
        var service = new ThemeCreatorService();
        var options = new ThemeCreatorOptions(
          "scratch",
          "ignored.json",
          "light",
          null,
          null,
          IncludeEntries: false);

        var ex = Assert.Throws<InvalidOperationException>(() => service.CreateThemeJsonFromFile(options));

        Assert.Contains("light", ex.Message, StringComparison.Ordinal);
    }

    private const string SourcePalette = """
{
  "themes": [
    {
      "id": "light",
      "entries": [
        { "key": "Brush.AppBackground", "hex": "#FFFFFF" },
        { "key": "Brush.TextPrimary", "hex": "#000000" }
      ]
    },
    {
      "id": "dark",
      "entries": [
        { "key": "Brush.AppBackground", "hex": "#111111" }
      ]
    }
  ]
}
""";

    private const string SourcePaletteWithComments = """
{
  "themes": [
    {
      "id": "light",
      "entries": [
        { "key": "Brush.AppBackground", "hex": "#FFFFFF" },
      ]
    },
    // theme comment
    {
      "id": "dark",
      "entries": [
        { "key": "Brush.AppBackground", "hex": "#111111" }
      ]
    },
  ]
}
""";
}
