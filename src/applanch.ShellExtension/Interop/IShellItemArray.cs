using System.Runtime.InteropServices;

namespace applanch.ShellExtension.Interop;

[ComImport]
[Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellItemArray
{
    void BindToHandler(IntPtr bindContext, in Guid bindHandler, in Guid requestedInterface, out IntPtr result);
    void GetPropertyStore(uint flags, in Guid requestedInterface, out IntPtr propertyStore);
    void GetPropertyDescriptionList(in Guid propertyKey, in Guid requestedInterface, out IntPtr propertyDescriptionList);
    void GetAttributes(uint attributeFlags, uint attributeMask, out uint attributes);
    void GetCount(out uint itemCount);
    void GetItemAt(uint index, [MarshalAs(UnmanagedType.Interface)] out IShellItem? shellItem);
    void EnumItems(out IntPtr enumShellItems);
}