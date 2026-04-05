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

## Versioning

applanch uses [MinVer](https://github.com/adamralph/minver) for automatic version calculation from Git tags.
Tags follow the `v` prefix convention (e.g., `v0.3.1`).

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

GitHub Pages publication:

- The documentation site is built and deployed by `.github/workflows/docs.yml`.
- The workflow runs on pushes to `master`/`main`, release publication, and manual dispatch.

Each documentation page has both an English version (`<page>.md`) and a Japanese version (`<page>.ja.md`).
When adding or updating a page, update both versions.
