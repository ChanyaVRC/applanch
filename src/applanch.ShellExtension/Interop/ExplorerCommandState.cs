namespace applanch.ShellExtension.Interop;

[Flags]
public enum ExplorerCommandState : uint
{
    Enabled = 0,
    Disabled = 0x1,
    Hidden = 0x2,
    Checkbox = 0x4,
    Checked = 0x8,
    RadioCheck = 0x10,
}