# Release Notes

Notable user-facing changes per version. For full commit history and downloadable assets, see the [GitHub releases page](https://github.com/ChanyaVRC/applanch/releases).

---

## v0.6.0-rc.1 — April 11, 2026

Release Candidate for the next applanch version.

### New

- **Prerelease update opt-in** — Added a setting to opt in to prerelease update channels.
- **Selectable update targets** — Improved update UX so users can choose from available update versions in Settings.

### Improved

- **Prerelease visibility in UI** — Added clearer prerelease labeling in the main window and refined prerelease badge styling, including better theme awareness.
- **Update metadata fetch workflow** — Simplified release metadata fetch flow and cache behavior to reduce unnecessary refetching after failures.
- **Update HTTP client lifecycle** — Refined shared `HttpClient` usage and initialization behavior for update operations.

### Notes

- This is an `-rc` prerelease build intended for validation before stable release.

## v0.5.7 — April 9, 2026

### Fixed

- **Theme selection localization refresh** — Fixed an issue where the selected theme label in Settings could remain in the previous language after changing app language.

### Improved

- **Theme option update flow** — Simplified theme option refresh behavior and reduced duplication in settings update paths.
- **Theme option lookup performance** — Switched theme option resolution to dictionary-based lookups for more direct O(1) access by theme ID.
- **Internal settings model cleanup** — Consolidated settings draft update paths to improve readability and maintainability.

---

## v0.5.6 — April 8, 2026

### New

- **Version display in UI** — The current app version is now shown in the main header and in the Settings window.

### Fixed

- **Settings window reliability** — Fixed an issue where the Settings window could fail to appear and the app process might not shut down correctly in some flows.
- **Bundled config diagnostics** — Added floating warning notifications when bundled configuration files are missing or invalid at startup.
- **Update restart loop** — Fixed an issue where update application could trigger repeated restart behavior in some environments.

### Improved

- **Theme palette config** — Theme palette loading has been refactored for better maintainability, and user-defined themes now support an optional `"disabled": true` flag.
- **Getting started docs** — The getting started guides now use richer UI screenshots and clearer walkthrough-style explanations.

---

## v0.5.5 — April 8, 2026

### Fixed

- **Config packaging** — All files under `Config/` are now included in build outputs and release artifacts. This fixes missing bundled samples such as the user-defined theme palette sample JSON.

---

## v0.5.4 — April 8, 2026

### New

- **Collapsible category sidebar** — The category sidebar now collapses when unpinned. Hover over the left edge of the window to expand it temporarily; click the pin button to keep it open. The pinned state is saved and restored across app restarts.

### Improved

- **Discord icon** — Apps launched via the Discord Update path are now shown with the Discord icon instead of a generic fallback.
- **Icon-mode toggle** — Refined the size and label of the icon-only mode toggle in the main window header.

---

## v0.5.3 — April 7, 2026

### Fixed

- **Icon-only list header layout** — Fixed alignment issues around the icon-mode toggle and section header in item display settings.

### Improved

- **User documentation** — Added explicit guidance for the icon-only item list setting in both the Settings and Items pages.

---

## v0.5.2 — April 7, 2026

### New

- **Icon-only item list mode** — Added an icon-only display mode for launch items, with a quick toggle in the main header.

### Fixed

- **Icon-mode startup reliability** — Fixed startup hang and initial blank-render issues that could occur when icon mode was enabled.
- **Icon tile layout stability** — Fixed clipping and layout inconsistencies in the icon grid.

### Improved

- **Icon list performance** — Introduced virtualization for the icon-mode launch grid to keep scrolling responsive with larger item counts.

---

## v0.5.1 — April 6, 2026

### Fixed

- **Category edit dialog** — Fixed an issue where the category suggestion ComboBox in the *Change Category* flow could remain hidden in some states.
- **Test stability** — Fixed a race in WPF test host initialization that could intermittently fail CI and local full test runs.

---

## v0.5.0 — April 6, 2026

### New

- **Context menu startup** — Settings → Startup now includes a toggle to add the Windows Explorer *Register with Applanch* context menu entry at startup (enabled by default).
- **Remove context menu** — A *Remove Applanch Context Menu Entries* button in Settings → Startup removes existing Explorer context menu entries immediately.
- **Configurable quick-add limit** — Settings → Item Display lets you control how many suggestions appear while typing in the quick-add box (10, 20, 30, 50 by default, or 100).

### Improved

- **Utility Actions** — Settings now includes *Copy Diagnostics* (copies app version, OS, locale, and log folder path to the clipboard) and *Open Log Folder* (opens the applanch log directory in File Explorer).
- **Installer** — The Windows installer now supports Japanese. Wizard icon updated.
- **Documentation** — Full bilingual (English / Japanese) documentation is available at [docs.applanch.com](https://docs.applanch.com/).

---

## v0.4.4 — April 4, 2026

### New

- **Windows Explorer context menu** — When registered as a sparse package, applanch adds a *Register with Applanch* entry to the Windows 11 right-click context menu for files and folders.
- **Update install behavior** — Settings → Startup includes an *Update Install Behavior* option: *Notify Only*, *Manual* (default), or *Automatically Apply*.
- **Check for Updates** — A *Check for Updates* action in Settings → Utility Actions triggers an immediate update check without restarting.
- **Unregister script** — `unregister-context-menu.cmd` is included in the portable ZIP as a fallback for removing the context menu entry without reinstalling.

---

## v0.4.3 — April 3, 2026

### New

- **Report Bug** — Settings → Utility Actions includes a *Report Bug* button that opens a pre-filled GitHub issue template in your browser.

---

## v0.4.2 — April 3, 2026

### New

- **Favicon cache** — Icons for web items are cached locally. New settings control whether HTTP icon fetching is allowed and whether requests to private IP ranges are permitted.
- **Open Location** — A *Open Location* context menu action on items opens the item's containing folder in File Explorer.
- **Delete missing item** — When a missing item fails to launch, the notification includes a *Delete* button to remove it from the list immediately.

---

## v0.4.1 — April 2, 2026

### New

- **URL items** — Web URLs can be added as launch items. applanch opens them in the default browser.
- **User-defined themes** — Custom color palettes placed in `Config/UserDefined/theme-palette/*.json` appear as selectable themes in Settings.
- **System theme** — The *Follow system* theme option now tracks the Windows light/dark setting in real time.
- **User-defined launch fallbacks** — Custom fallback resolution rules can be placed in `Config/UserDefined/launch-fallbacks/*.json`.

### Improved

- A restoration notification appears when a deleted item is recovered via undo.

---

## v0.3.0 — March 30, 2026

### New

- **Japanese UI** — The application is now available in Japanese. Language defaults to the Windows display language; change it in Settings → Appearance → Language.
- **Themed dialogs** — Confirmation and message dialogs now follow the active color theme.

---

## v0.2.0 — March 30, 2026

### New

- **Automatic updates** — applanch checks GitHub Releases on startup and can apply updates in-app.
- **Settings window** — A dedicated settings window provides centralized configuration.
- **Structured logging** — Detailed logs are written to `%LocalAppData%\applanch\app.log`.
