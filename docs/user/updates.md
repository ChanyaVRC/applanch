# Automatic Updates

applanch can check for new releases on GitHub and apply updates from within the app.

---

## How Updates Work

On startup, applanch queries the GitHub Releases API to compare the running version against the latest published release.
If a newer release is found, the update banner appears in the main window.

The asset downloaded during an update is a self-contained ZIP archive named:

```
applanch-{version}-{runtime-identifier}.zip
```

The runtime identifier (for example `win-x64`) is chosen automatically to match the running build.

---

## Update Banner

When a newer version is available, a banner appears below the header in the main window.
The banner shows the current version and the available version.

If the banner is dismissed, the **update button** in the header remains visible so you can apply the update at any time.

---

## Update Install Behavior

Configure this in **Settings** → **Startup** → **Update Install Behavior**.

| Option | Behavior |
|--------|----------|
| Notify Only | The banner is shown but contains no install button. Open the linked release page to download manually. |
| Manual (default) | The banner shows an **Update** button. Click it to download and apply the update. applanch restarts automatically after the update completes. |
| Automatically Apply | The update is downloaded and applied silently on the next startup. No interaction is required. |

---

## Manual Update Check

To check for a newer version immediately without restarting:

Open **Settings** → click **Check for Updates** in the header.

---

## Check for Updates on Startup

Toggle in **Settings** → **Startup** → **Check For Updates On Startup**.
Enabled by default. Disable this if you prefer to manage updates manually.

---

## Update Failure

If an update fails to apply, a notification appears with a brief description of the reason.

| Cause | Description |
|-------|-------------|
| Network error | Could not reach the GitHub download server |
| IO error | Could not write files during extraction |
| Permission denied | The app does not have write access to its own directory |
| Invalid package | The downloaded ZIP archive is corrupt or incompatible |

On failure, the current version continues to run. Try again later or download the release manually from [GitHub Releases](https://github.com/ChanyaVRC/applanch/releases).

---

## Debug Update Mode

**Settings** → **Startup** → **Debug Update Mode** forces the update check to treat any available release as an available update, even when the versions match.
This is intended for testing the update mechanism during development.
