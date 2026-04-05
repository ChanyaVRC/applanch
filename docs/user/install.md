# Install Guide

## Recommended Options

applanch is distributed in two formats.

- Portable ZIP package
- Installer EXE package

## Portable ZIP

1. Download the ZIP asset from GitHub Releases.
2. Extract it to any folder.
3. Run `applanch.exe`.

## Installer EXE

1. Download the installer asset from GitHub Releases.
2. Launch the installer.
3. Choose per-user or all-users install mode.
4. Follow the setup wizard.

## Data Location

Regardless of installation method, user data is stored in `%LOCALAPPDATA%\applanch\`:

| File | Contents |
|------|----------|
| `settings.json` | Application settings |
| `launch-items.json` | Saved item list |
| `app.log` | Application log |

Config files (themes, launch fallback rules) are stored in `Config\` next to `applanch.exe`.

## Uninstall

**Portable ZIP** — Delete the extracted folder.
User data under `%LOCALAPPDATA%\applanch\` is not removed automatically.
Delete that folder manually to remove all traces.

**Installer EXE** — Open **Settings** → **Apps** in Windows, find **applanch**, and click **Uninstall**.
User data under `%LOCALAPPDATA%\applanch\` is not removed by the uninstaller.
Delete that folder manually if needed.
