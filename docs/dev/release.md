# Release Guide

## Versioning

- applanch uses Git tags with the `v` prefix.
- Version calculation is handled by MinVer.

## Typical Release Flow

```powershell
git tag v0.3.1
git push origin master
git push origin v0.3.1
```

## Release Artifacts

Each runtime is published in two formats.

- Portable ZIP package
- Installer EXE package

## Documentation Publishing

The documentation site is deployed to GitHub Pages by GitHub Actions.
It is updated on pushes to `main` / `master`, on published releases, and on manual dispatch.
