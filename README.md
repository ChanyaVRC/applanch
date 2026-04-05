# applanch

[English](README.md) | [日本語](README.ja.md)

A lightweight Windows launcher for quickly starting apps, files, and folders.

`applanch` is a WPF desktop app focused on fast launch workflows with category management, quick-add suggestions, and built-in update checks.

## Features

- Quick launch for executable files, folders, and registered app paths
- Category-based organization and filtering
- Quick-add with suggestion support
- Editable launch arguments and display names
- Drag-and-drop reordering with persistence
- Light / Dark / System theme support
- Dark-theme-compatible in-app dialogs
- Automatic update check from GitHub Releases
- Optional startup update check toggle
- Windows context menu registration support
- Japanese / English resource-based localization

## Requirements

- Windows 10/11
- .NET 10 runtime (for self-contained behavior, use release artifacts)

## Install (Recommended)

1. Open Releases on GitHub.
2. Choose one of the latest assets:
  - Portable app: `applanch-<version>-<rid>.zip`
  - Installer: `applanch-<version>-<rid>-installer.exe`
3. For ZIP, extract to any folder and run `applanch.exe`.
4. For installer EXE, run it and follow the setup wizard.

## Usage

- Add items from the main window quick-add input.
- Launch by clicking an item.
- Use context menu actions to rename items, edit category, and edit arguments.
- Open Settings to control:
  - Theme (System/Light/Dark)
  - Close on launch
  - Check updates on startup
  - Debug update mode

## Development

### Documentation

Project documentation lives under `docs/` and is published with MkDocs + Material for MkDocs.

Local preview:

```powershell
python -m pip install -r docs/requirements.txt
python -m mkdocs serve
```

The GitHub Pages workflow publishes the site on pushes to `main` / `master`, on published releases, and on manual runs.

### Build

```powershell
dotnet build
```

### Test

```powershell
dotnet test
```

### Format Check

```powershell
dotnet format applanch.slnx --verify-no-changes --no-restore --verbosity minimal
```

### Run

```powershell
dotnet run --project src/applanch/applanch.csproj
```

### Build Sparse MSIX (Context Menu Package)

Use this when you need to register the Windows 11 simplified context menu integration.

1. Build app binaries first.

```powershell
dotnet build applanch.slnx -c Debug
```

2. Build the sparse package.

```powershell
.\scripts\build-sparse-package.ps1
```

3. The output is generated at `artifacts/sparse-package/applanch.msix` by default.

To place the package next to the app executable (for local registration tests), pass `-OutputMsix` explicitly.

```powershell
.\scripts\build-sparse-package.ps1 -OutputMsix .\src\applanch\bin\Debug\net10.0-windows10.0.22000.0\applanch.msix
```

### Set Up MSIX Build From Scratch (New Dev Machine)

If this is your first setup on a machine, follow these steps once.

Quick start (recommended):

```powershell
.\scripts\setup-dev-environment.ps1 -SetupDevSigning -BuildSparseMsix
```

The script checks prerequisites, runs initial Debug build, sets up local dev signing,
and builds a sparse MSIX package.
If needed, it prompts for UAC elevation during the dev-signing step.

1. Install prerequisites.
- .NET SDK 10
- Windows SDK (including `makeappx.exe` and `signtool.exe`)

2. Build once to restore tools and verify the workspace.

```powershell
dotnet build applanch.slnx -c Debug
```

3. Set up a local dev code-signing certificate (run PowerShell as Administrator).

```powershell
.\scripts\setup-dev-signing.ps1
```

4. Build sparse MSIX.

```powershell
.\scripts\build-sparse-package.ps1
```

Notes:
- `build-sparse-package.ps1` automatically signs the package when `CN=applanch` dev cert exists in `CurrentUser\My`.
- Local or external signing can use `scripts/sign-msix.ps1` with `MSIX_SIGNING_CERT_BASE64` and `MSIX_SIGNING_CERT_PASSWORD`. The GitHub Actions release workflow generates a temporary self-signed certificate (`CN=applanch`) for CI signing.
- If signing is unavailable, sparse package registration may fail depending on your machine policy.

### Context Menu Policy Requirements

The Windows 11 simplified context menu requires sparse MSIX registration (`Add-AppxPackage -ExternalLocation`).
This operation is governed by a **system-wide policy** with no per-app exception mechanism.

If registration fails with error `0x80073D2E`, use one of the following approaches:

**Option 1 — Developer Mode (simplest for individual developers)**

Enable via Windows Settings → System → For developers → Developer Mode → On.

**Option 2 — AppModelUnlock registry keys (non-MDM machines, requires admin)**

```powershell
$path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock'
Set-ItemProperty -Path $path -Name AllowAllTrustedApps           -Value 1 -Type DWord
Set-ItemProperty -Path $path -Name AllowDevelopmentWithoutDevLicense -Value 1 -Type DWord
```

This is what Developer Mode sets internally.
On MDM-managed machines the `HKLM:\SOFTWARE\Policies\Microsoft\Windows\Appx` keys override these, so Option 2 will have no effect without IT involvement.

**Option 3 — Group Policy / MDM (corporate/managed machines)**

Ask your IT admin to set these via Intune (or local Group Policy on unmanaged machines):

`Computer Configuration → Administrative Templates → Windows Components → App Package Deployment`

- *Allow all trusted apps to install* → **Enabled**
- *Allows development of Windows Store apps and installing them from an integrated development environment (IDE)* → **Enabled**

**Fallback — Legacy context menu**

If none of the above is possible, the COM-based registration (HKCU registry) still provides
the context menu entry under *Show more options* (classic context menu).

## Project Structure

- `src/applanch`: WPF application
- `src/applanch/Infrastructure`: application services by responsibility
- `src/applanch/ViewModels`: UI view models
- `src/applanch/Controls`: custom controls
- `docs`: MkDocs documentation source
- `tests/applanch.Tests`: xUnit test project

## Versioning and Release

- Version is derived from Git tags via MinVer (tag prefix: `v`).
- Release artifacts are published in two formats per runtime:
  - Portable ZIP package
  - Installer EXE package
- Typical release flow:

```powershell
git tag v0.3.1
git push origin master
git push origin v0.3.1
```

## License

This project is licensed under the MIT License.
See [LICENSE](LICENSE).

