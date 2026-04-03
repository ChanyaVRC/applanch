using applanch.Tests.TestSupport;
using applanch.Events;
using applanch.Infrastructure.Storage;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.Application;

public class SettingsWindowBugReportTests
{
    [Fact]
    public void CreateReportBugStartInfo_ReturnsGitHubIssueUrlWithPrefilledBody()
    {
        var startInfo = SettingsWindow.CreateReportBugStartInfo();

        Assert.StartsWith("https://github.com/ChanyaVRC/applanch/issues/new?title=", startInfo.FileName);
        Assert.Contains("body=", startInfo.FileName);
        Assert.True(startInfo.UseShellExecute);
    }

    [Theory]
    [InlineData("en", "## Summary", "- App version:")]
    [InlineData("ja", "## 概要", "- アプリバージョン:")]
    public void CreateReportBugBody_UsesLocalizedTemplate(string cultureName, string expectedSection, string expectedVersionLabel)
    {
        using var cultureScope = new CultureScope(cultureName);

        var body = SettingsWindow.CreateReportBugBody();

        Assert.Contains(expectedSection, body);
        Assert.Contains("## ", body);
        Assert.Contains(expectedVersionLabel, body);
        Assert.Contains("- .NET:", body);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("ja")]
    public void CreateReportBugIssueUri_UsesLocalizedTitle(string cultureName)
    {
        using var cultureScope = new CultureScope(cultureName);

        var uri = SettingsWindow.CreateReportBugIssueUri();
        var expectedEncodedTitle = Uri.EscapeDataString(AppResources.BugReport_IssueTitle);

        Assert.Contains($"title={expectedEncodedTitle}", uri.AbsoluteUri);
    }

    [Fact]
    public void CreateOpenLogFolderStartInfo_OpensExplorerWithLogDirectory()
    {
        var startInfo = SettingsWindow.CreateOpenLogFolderStartInfo();

        Assert.Equal("explorer.exe", startInfo.FileName);
        Assert.StartsWith("/select,\"", startInfo.Arguments);
        Assert.Contains("app.log\"", startInfo.Arguments);
        Assert.True(startInfo.UseShellExecute);
    }

    [Fact]
    public void CreateDiagnosticsText_ContainsExpectedFields()
    {
        var vm = new SettingsWindowViewModel(new AppSettings(), new AppEvent());

        var text = vm.CreateDiagnosticsText();

        Assert.Contains("App version:", text);
        Assert.Contains("OS:", text);
        Assert.Contains(".NET:", text);
        Assert.Contains("Log folder:", text);
        Assert.Contains("Update install behavior:", text);
    }
}
