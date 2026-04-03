namespace applanch.ShellExtension.Interop;

[Flags]
public enum ExplorerCommandFlags : uint
{
    Default = 0,
    HasSubcommands = 0x1,
    HasSplitButton = 0x2,
    HideLabel = 0x4,
    IsSeparator = 0x8,
    HasLuaShield = 0x10,
    SeparatorBefore = 0x20,
    SeparatorAfter = 0x40,
    IsDropDown = 0x80,
    Toggleable = 0x100,
    AutoMenuIcons = 0x200,
}