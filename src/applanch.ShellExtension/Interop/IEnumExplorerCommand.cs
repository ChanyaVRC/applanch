using System.Runtime.InteropServices;

namespace applanch.ShellExtension.Interop;

[ComVisible(true)]
[Guid("A88826F8-186F-4987-AADE-EA0CEF8FBFE8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IEnumExplorerCommand
{
    void Next(uint commandCount, [MarshalAs(UnmanagedType.Interface)] out IExplorerCommand? command, out uint fetchedCount);
    void Skip(uint commandCount);
    void Reset();
    void Clone([MarshalAs(UnmanagedType.Interface)] out IEnumExplorerCommand? commands);
}