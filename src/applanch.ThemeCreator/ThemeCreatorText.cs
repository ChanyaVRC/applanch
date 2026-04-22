using System.Globalization;
using System.Resources;

namespace applanch.ThemeCreator;

internal static class ThemeCreatorText
{
    private static readonly ResourceManager ResourceManager = new("applanch.ThemeCreator.ThemeCreatorStrings", typeof(ThemeCreatorText).Assembly);

    public static string WindowTitle => Get("WindowTitle");
    public static string SourcePaletteLabel => Get("SourcePaletteLabel");
    public static string BaseThemeLabel => Get("BaseThemeLabel");
    public static string ThemeIdLabel => Get("ThemeIdLabel");
    public static string NameEnLabel => Get("NameEnLabel");
    public static string NameJaLabel => Get("NameJaLabel");
    public static string OutputPathLabel => Get("OutputPathLabel");
    public static string IncludeEntriesLabel => Get("IncludeEntriesLabel");
    public static string PreviewLabel => Get("PreviewLabel");
    public static string BrowseButton => Get("BrowseButton");
    public static string ReloadButton => Get("ReloadButton");
    public static string CreateButton => Get("CreateButton");
    public static string SourceFileFilter => Get("SourceFileFilter");
    public static string OutputFileFilter => Get("OutputFileFilter");
    public static string SuccessCaption => Get("SuccessCaption");
    public static string ErrorCaption => Get("ErrorCaption");
    public static string GuiSuccessMessageFormat => Get("GuiSuccessMessageFormat");
    public static string GuiThemeIdRequired => Get("GuiThemeIdRequired");
    public static string GuiOutputRequired => Get("GuiOutputRequired");
    public static string GuiSourceNotFoundFormat => Get("GuiSourceNotFoundFormat");
    public static string GuiPreviewUnavailable => Get("GuiPreviewUnavailable");
    public static string CliUsageHeader => Get("CliUsageHeader");
    public static string CliUsageCommand => Get("CliUsageCommand");
    public static string CliOptionsHeader => Get("CliOptionsHeader");
    public static string CliOptionBase => Get("CliOptionBase");
    public static string CliOptionSource => Get("CliOptionSource");
    public static string CliOptionNameEn => Get("CliOptionNameEn");
    public static string CliOptionNameJa => Get("CliOptionNameJa");
    public static string CliOptionNoEntries => Get("CliOptionNoEntries");
    public static string CliCreatedMessageFormat => Get("CliCreatedMessageFormat");
    public static string CliFailedMessageFormat => Get("CliFailedMessageFormat");
    public static string CliUnknownArgumentFormat => Get("CliUnknownArgumentFormat");
    public static string CliMissingArgumentValueFormat => Get("CliMissingArgumentValueFormat");
    public static string CliThemeIdRequired => Get("CliThemeIdRequired");
    public static string CliOutputRequired => Get("CliOutputRequired");

    private static string Get(string name)
        => ResourceManager.GetString(name, CultureInfo.CurrentUICulture)
            ?? throw new InvalidOperationException($"Missing theme creator resource '{name}'.");
}
