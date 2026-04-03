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
2. Download the latest `applanch-<version>-<rid>.zip` asset.
3. Extract to any folder.
4. Run `applanch.exe`.

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
- For release/CD signing with OV/EV certificates, use `scripts/sign-msix.ps1` with `MSIX_SIGNING_CERT_BASE64` and `MSIX_SIGNING_CERT_PASSWORD`.
- If signing is unavailable, sparse package registration may fail depending on your machine policy.

## Project Structure

- `src/applanch`: WPF application
- `src/applanch/Infrastructure`: application services by responsibility
- `src/applanch/ViewModels`: UI view models
- `src/applanch/Controls`: custom controls
- `tests/applanch.Tests`: xUnit test project

## Versioning and Release

- Version is derived from Git tags via MinVer (tag prefix: `v`).
- Typical release flow:

```powershell
git tag v0.3.1
git push origin master
git push origin v0.3.1
```

