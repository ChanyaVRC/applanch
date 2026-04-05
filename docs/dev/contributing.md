# Contributing

Thank you for your interest in contributing to applanch.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 or Windows 11
- A C# IDE (Visual Studio 2022+ or Rider recommended)

## Clone & Build

```powershell
git clone https://github.com/ChanyaVRC/applanch.git
cd applanch
dotnet build
```

## Run Tests

```powershell
dotnet test
```

Focus on tests relevant to changed areas first, then run the full suite before submitting.

## Format Check

All code must pass the formatter before committing:

```powershell
dotnet format applanch.slnx --verify-no-changes --no-restore --verbosity minimal
```

If verification fails, apply formatting automatically:

```powershell
dotnet format applanch.slnx --no-restore --verbosity minimal
```

Then re-run the verification step.

## Branching & Commits

- Work on a feature branch forked from `master`.
- Keep one logical concern per commit.
- Use concise imperative commit messages (e.g., `Fix launch fallback for Riot VALORANT`).
- Do not mix behavior changes with unrelated formatting edits in a single commit.

## Code Conventions

- One type per file.
- Avoid `ref`/`out` parameters; prefer return values.
- All user-facing strings must come from localized resource files (`Properties/Resources.resx` and `Properties/Resources.ja.resx`).
- Do not hard-code UI text in XAML or C#.
- Follow existing naming conventions: `PascalCase` for types and members, `_camelCase` for private fields.

## Adding Tests

- Add or update tests for every bug fix or behavior change.
- Tests that instantiate WPF controls must run on an STA thread.
- Keep control tests isolated from global application resources.

## Project Structure

```
src/applanch/
  App.xaml / App.xaml.cs          # Application entry point
  MainWindow.xaml / .cs           # Main window (host)
  SettingsWindow.xaml / .cs       # Settings window (host)
  Controls/                       # Reusable WPF user controls
  ViewModels/                     # View models (ObservableObject-based)
  Events/                         # AppEvents pub/sub bus
  Infrastructure/
    Dialogs/                      # Dialog abstraction
    Integration/                  # Windows shell / context menu integration
    Items/                        # Item CRUD workflows
    Launch/                       # Launch execution and fallback resolution
    Resolution/                   # App path resolution
    Storage/                      # Settings persistence (JSON)
    Theming/                      # Theme palette loading
    Updates/                      # GitHub Releases update check
    Utilities/                    # Shared helpers
  Properties/                     # Resource files (Resources.resx, Resources.ja.resx)
  Config/                         # Bundled default config files
tests/applanch.Tests/             # xUnit test project (mirrors src/ structure)
src/applanch.ResourceGenerator/   # Source generator for typed resource access
```

Loose conventions:
- **`AppEvents`** is a typed pub/sub bus. Components publish and subscribe using strongly-typed `AppEventKey<T>` keys.
- **`Infrastructure/`** contains all I/O and platform concerns. View models depend on infrastructure through thin interfaces where testability matters.
- **`ViewModels/`** contains no direct I/O — all side effects go through `AppEvents` or injected services.

## Pull Requests

1. Ensure `dotnet build` and `dotnet test` both pass.
2. Ensure `dotnet format --verify-no-changes` passes.
3. Open a pull request against the `master` branch.
4. Describe what changed and why in the PR description.

## Localization

applanch targets English and Japanese.
When adding a user-facing string:

1. Add the key and English value to `Properties/Resources.resx`.
2. Add the Japanese translation to `Properties/Resources.ja.resx`.
3. Reference the key through `LocalizedStrings.Instance` in XAML or C#.

## Documentation

Project documentation lives under `docs/` and is built with MkDocs + Material for MkDocs.

Local preview:

```powershell
python -m pip install -r docs/requirements.txt
python -m mkdocs serve
```

Each documentation page has both an English version (`<page>.md`) and a Japanese version (`<page>.ja.md`).
When adding or updating a page, update both versions.
