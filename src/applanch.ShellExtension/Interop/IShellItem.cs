using System.Runtime.InteropServices;

namespace applanch.ShellExtension.Interop;

[ComImport]
[Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellItem
{
    void BindToHandler(IntPtr bindContext, in Guid bindHandler, in Guid requestedInterface, out IntPtr result);
    void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem? parent);
    void GetDisplayName(ShellDisplayNameKind displayNameKind, [MarshalAs(UnmanagedType.LPWStr)] out string name);
    void GetAttributes(uint attributeMask, out uint attributes);
    void Compare([MarshalAs(UnmanagedType.Interface)] IShellItem? otherItem, uint hint, out int order);
}