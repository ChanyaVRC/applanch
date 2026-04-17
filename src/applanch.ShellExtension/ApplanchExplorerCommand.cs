using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using applanch.ShellExtension.Interop;
using applanch.ShellIntegration;

namespace applanch.ShellExtension;

[ComVisible(true)]
[Guid(ExplorerCommandIds.ClassId)]
[ClassInterface(ClassInterfaceType.None)]
[ComDefaultInterface(typeof(IExplorerCommand))]
public sealed class ApplanchExplorerCommand : IExplorerCommand
{
    private const int EFail = unchecked((int)0x80004005);
    private const int ENotImpl = unchecked((int)0x80004001);
    private const string RegisterArgument = "--register";
    private const string MenuRegistryPath = @"Software\Classes\*\shell\applanch.register";
    private readonly IApplanchExplorerCommandRuntime _runtime;

    public ApplanchExplorerCommand()
        : this(new ApplanchExplorerCommandRuntime())
    {
    }

    internal ApplanchExplorerCommand(IApplanchExplorerCommandRuntime runtime)
    {
        _runtime = runtime;
    }

    public void GetTitle(IShellItemArray? itemArray, out string name)
    {
        name = _runtime.GetMenuText();
    }

    public void GetIcon(IShellItemArray? itemArray, out string icon)
    {
        icon = _runtime.ResolveExecutablePath() ?? string.Empty;
    }

    public void GetToolTip(IShellItemArray? itemArray, out string infoTip)
    {
        infoTip = string.Empty;
    }

    public void GetCanonicalName(out Guid commandName)
    {
        commandName = Guid.Parse(ExplorerCommandIds.CanonicalName);
    }

    public void GetState(IShellItemArray? itemArray, bool okToBeSlow, out ExplorerCommandState commandState)
    {
        commandState = string.IsNullOrWhiteSpace(_runtime.ResolveExecutablePath()) || string.IsNullOrWhiteSpace(_runtime.GetSelectedPath(itemArray))
            ? ExplorerCommandState.Hidden
            : ExplorerCommandState.Enabled;
    }

    public void Invoke(IShellItemArray? itemArray, System.Runtime.InteropServices.ComTypes.IBindCtx? bindContext)
    {
        var executablePath = _runtime.ResolveExecutablePath();
        var selectedPath = _runtime.GetSelectedPath(itemArray);
        if (string.IsNullOrWhiteSpace(executablePath) || string.IsNullOrWhiteSpace(selectedPath))
        {
            throw new COMException("Unable to resolve applanch command target.", EFail);
        }

        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(RegisterArgument);
        startInfo.ArgumentList.Add(selectedPath);

        if (_runtime.StartProcess(startInfo) is null)
        {
            throw new COMException("Unable to launch applanch.", EFail);
        }
    }

    public void GetFlags(out ExplorerCommandFlags flags)
    {
        flags = ExplorerCommandFlags.Default;
    }

    public void EnumSubCommands(out IEnumExplorerCommand? commands)
    {
        commands = null;
        throw new COMException("Subcommands are not supported.", ENotImpl);
    }

    internal static string GetMenuTextFromRegistry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(MenuRegistryPath, writable: false);
        return key?.GetValue(string.Empty) as string ?? string.Empty;
    }

    internal static string? ResolveExecutablePath()
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(ApplanchExplorerCommand).Assembly.Location);
        if (string.IsNullOrWhiteSpace(assemblyDirectory))
        {
            return null;
        }

        var executablePath = Path.Combine(assemblyDirectory, "applanch.exe");
        return File.Exists(executablePath) ? executablePath : null;
    }

    internal static string? GetSelectedPath(IShellItemArray? itemArray)
    {
        if (itemArray is null)
        {
            return null;
        }

        try
        {
            itemArray.GetCount(out var itemCount);
            if (itemCount != 1)
            {
                return null;
            }

            itemArray.GetItemAt(0, out var item);
            if (item is null)
            {
                return null;
            }

            return TryGetDisplayName(item, ShellDisplayNameKind.FileSystemPath)
                ?? TryGetDisplayName(item, ShellDisplayNameKind.DesktopAbsoluteParsing);
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static string? TryGetDisplayName(IShellItem item, ShellDisplayNameKind displayNameKind)
    {
        try
        {
            item.GetDisplayName(displayNameKind, out var path);
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }
        catch (COMException)
        {
            return null;
        }
    }
}