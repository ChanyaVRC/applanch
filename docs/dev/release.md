# Release Guide

## Versioning

applanch uses [MinVer](https://github.com/adamralph/minver) for automatic version calculation from Git tags.
Tags must follow the format `vMAJOR.MINOR.PATCH` or `vMAJOR.MINOR.PATCH-suffix` (e.g., `v0.3.1`, `v1.0.0-rc1`).

Tags with a pre-release suffix (e.g., `-rc1`, `-beta.1`) are published as pre-release on GitHub Releases.

## Typical Release Flow

```powershell
git tag v0.3.1
git push origin master
git push origin v0.3.1
```

Pushing the tag triggers the CD Release workflow (`.github/workflows/cd-release.yml`) automatically.

## Release Workflow Overview

The CD workflow runs four jobs:

| Job | Runner | What it does |
|-----|--------|-------------|
| `metadata` | ubuntu-latest | Validates the tag format, resolves version string and pre-release flag. |
| `verify` | windows-latest | Restores, runs the full test suite against the tagged commit. |
| `publish-assets` | windows-latest (matrix) | Publishes, builds installers and sparse MSIX, archives assets — once per RID. |
| `release` | ubuntu-latest | Creates the GitHub Release and uploads all artifacts. |

`publish-assets` runs in parallel for `win-x64` and `win-x86`.
The release is not created until all matrix legs pass.

## Release Artifacts

Each RID produces four files:

| File | Description |
|------|-------------|
| `applanch-<version>-<rid>.zip` | Portable self-contained single-file executable + config |
| `applanch-<version>-<rid>.zip.sha256` | SHA-256 checksum of the ZIP |
| `applanch-<version>-<rid>-installer.exe` | Inno Setup installer |
| `applanch-<version>-<rid>-installer.exe.sha256` | SHA-256 checksum of the installer |

Supported RIDs: `win-x64`, `win-x86`.

## Manual Trigger

The workflow can also be triggered manually without pushing a tag:

1. Go to **Actions → CD Release → Run workflow**.
2. Enter the tag in the form `v1.2.3` (the tag must already exist in the repository).
3. Click **Run workflow**.

This is useful for re-running a release after fixing a workflow issue.

## CI Workflow

The CI workflow (`.github/workflows/ci.yml`) runs on pushes to `master` and on pull requests.
It runs three jobs in parallel after a shared build step: `format`, `test`, and `lint`.
All three must pass before a PR can be merged.

## Documentation Publishing

The documentation site is deployed to GitHub Pages by `.github/workflows/docs.yml`.
It is updated on pushes to `master`, on published releases, and on manual dispatch.
