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

