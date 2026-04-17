using System.Diagnostics;
using applanch.ShellExtension.Interop;
using applanch.ShellExtension.Tests.TestDoubles;
using Xunit;

namespace applanch.ShellExtension.Tests;

public class ApplanchExplorerCommandTests
{
    [Fact]
    public void GetTitle_ReturnsProvidedMenuText()
    {
        var sut = new ApplanchExplorerCommand(new ApplanchExplorerCommandTestRuntime
        {
            MenuTextProvider = static () => "Applanch に登録",
            ExecutablePathProvider = static () => @"C:\Apps\applanch.exe",
            SelectedPathProvider = static _ => @"C:\Temp\file.txt",
            StartProcessHandler = static _ => null
        });

        sut.GetTitle(null, out var name);

        Assert.Equal("Applanch に登録", name);
    }

    [Fact]
    public void GetState_HidesCommand_WhenSelectionIsUnavailable()
    {
        var sut = new ApplanchExplorerCommand(new ApplanchExplorerCommandTestRuntime
        {
            MenuTextProvider = static () => "text",
            ExecutablePathProvider = static () => @"C:\Apps\applanch.exe",
            SelectedPathProvider = static _ => null,
            StartProcessHandler = static _ => null
        });

        sut.GetState(null, okToBeSlow: false, out var state);

        Assert.Equal(ExplorerCommandState.Hidden, state);
    }

    [Fact]
    public void Invoke_StartsApplanch_WithRegisterArgumentAndSelectedPath()
    {
        ProcessStartInfo? captured = null;
        var sut = new ApplanchExplorerCommand(new ApplanchExplorerCommandTestRuntime
        {
            MenuTextProvider = static () => "text",
            ExecutablePathProvider = static () => @"C:\Apps\applanch.exe",
            SelectedPathProvider = static _ => @"C:\Temp\file.txt",
            StartProcessHandler = startInfo =>
            {
                captured = startInfo;
                return Process.GetCurrentProcess();
            }
        });

        sut.Invoke(null, null);

        Assert.NotNull(captured);
        Assert.Equal(@"C:\Apps\applanch.exe", captured!.FileName);
        Assert.Equal(new[] { "--register", @"C:\Temp\file.txt" }, captured.ArgumentList);
        Assert.False(captured.UseShellExecute);
    }
}
