using System.Diagnostics;
using applanch.ShellExtension;
using applanch.ShellExtension.Interop;
using Xunit;

namespace applanch.Tests.Infrastructure.Integration;

public class ApplanchExplorerCommandTests
{
    [Fact]
    public void GetTitle_ReturnsProvidedMenuText()
    {
        var sut = new ApplanchExplorerCommand(static () => "Applanch に登録", static () => @"C:\Apps\applanch.exe", static _ => @"C:\Temp\file.txt", static _ => null);

        sut.GetTitle(null, out var name);

        Assert.Equal("Applanch に登録", name);
    }

    [Fact]
    public void GetState_HidesCommand_WhenSelectionIsUnavailable()
    {
        var sut = new ApplanchExplorerCommand(static () => "text", static () => @"C:\Apps\applanch.exe", static _ => null, static _ => null);

        sut.GetState(null, okToBeSlow: false, out var state);

        Assert.Equal(ExplorerCommandState.Hidden, state);
    }

    [Fact]
    public void Invoke_StartsApplanch_WithRegisterArgumentAndSelectedPath()
    {
        ProcessStartInfo? captured = null;
        var sut = new ApplanchExplorerCommand(
            static () => "text",
            static () => @"C:\Apps\applanch.exe",
            static _ => @"C:\Temp\file.txt",
            startInfo =>
            {
                captured = startInfo;
                return Process.GetCurrentProcess();
            });

        sut.Invoke(null, null);

        Assert.NotNull(captured);
        Assert.Equal(@"C:\Apps\applanch.exe", captured!.FileName);
        Assert.Equal(new[] { "--register", @"C:\Temp\file.txt" }, captured.ArgumentList);
        Assert.False(captured.UseShellExecute);
    }
}