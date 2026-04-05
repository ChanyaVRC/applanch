# Settings Reference

Open Settings from the button in the top-right area of the main window.

---

## Appearance

### Theme

Controls the color scheme of the application.

| Option | Behavior |
|--------|----------|
| Follow system (default) | Matches the Windows light/dark setting |
| Light | Always use the light palette |
| Dark | Always use the dark palette |

Custom themes from `Config/UserDefined/theme-palette/*.json` also appear here.
See the [Themes](themes.md) page for details.

### Language

| Option | Behavior |
|--------|----------|
| Follow system (default) | Uses the Windows display language |
| English | Always use English |
| 日本語 | Always use Japanese |

---

## Launch Behavior

### Post-Launch Behavior

What applanch does after an item is launched.

| Option | Behavior |
|--------|----------|
| Close App (default) | The application window closes after launch |
| Minimize Window | The window minimizes to the taskbar |
| Keep Open | The window stays open and active |

### Confirm Before Launch

When enabled, a confirmation dialog appears each time you launch an item.

### Run As Administrator

When enabled, all items are launched with administrator (elevated) privileges.

### Confirm Before Delete

When enabled, a confirmation dialog appears before removing an item from the list.

---

## Startup

### Check For Updates On Startup

When enabled (default), applanch checks GitHub Releases for a new version each time it starts.

### Update Install Behavior

Controls what happens when an update is detected.

| Option | Behavior |
|--------|----------|
| Notify Only | Shows a banner only |
| Manual (default) | User manually triggers the update from the banner |
| Automatically Apply | Update is applied automatically |

### Debug Update Mode

Enables testing of the update mechanism regardless of actual version differences.
Intended for development use only.

### Start Minimized On Launch

When enabled, the main window starts minimized instead of visible.

### Launch At Windows Startup

When enabled, applanch is registered to start automatically with Windows.

### Add to Explorer Context Menu On Startup

When enabled (default), applanch adds its "Register with Applanch" entry to Explorer context menus at startup.

### Remove Applanch Context Menu Entries

Click this button to remove existing Applanch Explorer context menu entries immediately.

---

## Item Display

### Category Sort Mode

| Option | Behavior |
|--------|----------|
| Alphabetical (default) | Categories sorted A–Z |
| As Added | Categories appear in the order created |

### App List Sort Mode

| Option | Behavior |
|--------|----------|
| Manual (default) | Ordered by drag-and-drop position |
| Name | Alphabetical by display name |
| Category Then Name | Grouped by category, then alphabetical |

### Quick Add Suggestion Limit

Controls how many suggestions are shown while typing in the quick-add box.

| Option | Behavior |
|--------|----------|
| 10 | Compact list with fewer suggestions |
| 20 | Balanced list size |
| 30 | Larger list with more choices |
| 50 (default) | Recommended general-use setting |
| 100 | Maximum list size in settings |

---

## Icons & Network

### Fetch HTTP Icons

When enabled (default), applanch downloads icons from web URLs associated with items.

### Allow Private Network HTTP Icon Requests

When enabled, icon downloads from private IP ranges (e.g., 192.168.x.x) are permitted.
Disabled by default.

---

## Utility Actions

| Action | Description |
|--------|-------------|
| Reset to Defaults | Restores all settings to their default values |
| Copy Diagnostics | Copies a diagnostics summary to the clipboard. Includes app version, OS, .NET version, locale, log folder path, and current update settings. Useful when reporting a bug. |
| Open Log Folder | Opens the folder where applanch writes its log files in File Explorer. |
| Report Bug | Opens a pre-filled GitHub issue template in your browser where you can describe the problem. |
