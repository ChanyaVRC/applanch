using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace applanch.ShellExtension.Interop;

[ComVisible(true)]
[Guid("A08CE4D0-FA25-44AB-B57C-C7B1C323E0B9")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IExplorerCommand
{
    void GetTitle(IShellItemArray? itemArray, [MarshalAs(UnmanagedType.LPWStr)] out string name);
    void GetIcon(IShellItemArray? itemArray, [MarshalAs(UnmanagedType.LPWStr)] out string icon);
    void GetToolTip(IShellItemArray? itemArray, [MarshalAs(UnmanagedType.LPWStr)] out string infoTip);
    void GetCanonicalName(out Guid commandName);
    void GetState(IShellItemArray? itemArray, [MarshalAs(UnmanagedType.Bool)] bool okToBeSlow, out ExplorerCommandState commandState);
    void Invoke(IShellItemArray? itemArray, IBindCtx? bindContext);
    void GetFlags(out ExplorerCommandFlags flags);
    void EnumSubCommands([MarshalAs(UnmanagedType.Interface)] out IEnumExplorerCommand? commands);
}