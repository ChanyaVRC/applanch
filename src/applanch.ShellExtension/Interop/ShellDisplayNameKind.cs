namespace applanch.ShellExtension.Interop;

public enum ShellDisplayNameKind : uint
{
    NormalDisplay = 0,
    ParentRelativeParsing = 0x80018001,
    DesktopAbsoluteParsing = 0x80028000,
    ParentRelativeEditing = 0x80031001,
    DesktopAbsoluteEditing = 0x8004c000,
    FileSystemPath = 0x80058000,
    Url = 0x80068000,
    ParentRelativeForAddressBar = 0x8007c001,
    ParentRelative = 0x80080001,
    ParentRelativeForUI = 0x80094001,
}