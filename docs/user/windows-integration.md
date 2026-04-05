# Windows Integration

applanch integrates with Windows in two ways: a right-click context menu entry in File Explorer, and an optional startup registration so it launches automatically with Windows.

---

## Context Menu

When applanch runs for the first time, it registers a **"Register to applanch"** verb in the Windows Explorer context menu.
Right-clicking any file, folder, or executable shows this entry, which adds the target directly to your applanch list.

### Registration targets

| Target | Context |
|--------|---------|
| Files (any extension) | Right-click on a file |
| Executables (`.exe`) | Right-click on a program |
| Folders | Right-click on a folder |
| Directory background | Right-click inside a folder (empty area) |

### Windows 11 simplified context menu

On Windows 11, Explorer shows a combined context menu that only includes entries from apps with **package identity**.
applanch registers a sparse MSIX package on first launch to gain package identity and appear in this menu.

!!! note
    The Windows 11 simplified menu entry requires the sparse package registration to succeed.
    If the app is started as a portable installation without write access to the package store, the verb falls back to the legacy (classic) context menu and remains accessible via **Show more options**.

### Re-registering the context menu

If the context menu entry disappears after moving the `applanch.exe` file, re-run the registration:

```
applanch.exe --register
```

### Removing the context menu entry

To remove the context menu entry:

```
applanch.exe --unregister-context-menu
```

---

## Launch at Windows Startup

When enabled, applanch starts automatically when you sign in to Windows.

**To enable:**
Open **Settings** → **Startup** → enable **Launch At Windows Startup**.

This writes an entry to:

```
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

Disabling the setting removes that entry.
No installer or administrator rights are needed — it is a per-user startup registration.
