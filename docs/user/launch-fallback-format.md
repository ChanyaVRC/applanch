# Launch Fallback JSON Format

This page describes the JSON structure used for launch fallback rule files.

## Top-Level Structure

Each fallback file contains a single `rules` array:

```json
{
  "rules": [
    {
      "name": "My Game",
      "kind": "uri-template",
      "enabled": true,
      "matchFileNames": ["MyGame.exe"],
      "uriTemplate": "mylauncher://launch/{appId}",
      "appIdSource": "static:12345"
    }
  ]
}
```

## Rule Object Fields

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `name` | string | | Display name used for identification only. |
| `kind` | string | ✓ | `"command-template"` or `"uri-template"`. |
| `enabled` | boolean | | Default: `true`. Set to `false` to disable without deleting. |
| `fallbackTrigger` | string | | `"always"` or `"access-denied"` (default). |
| `matchFileNames` | string[] | | Executable file names that trigger this rule. |
| `pathContains` | string | | Trigger when the full path contains this substring. |
| `fileNameTemplate` | string | | Launcher executable path. Used when `kind` is `"command-template"`. |
| `argumentsTemplate` | string | | Command-line arguments for the launcher. |
| `uriTemplate` | string | | URI to open. Used when `kind` is `"uri-template"`. |
| `appId` | string | | Static app ID value. Takes precedence over `appIdSource`. |
| `appIdSource` | string | | How to resolve `{appId}` at runtime. See [App ID Sources](#app-id-sources). |
| `product` | string | | Value substituted for `{product}` in templates. |
| `patchline` | string | | Value substituted for `{patchline}` in templates. Default: `"live"`. |

### `kind`

| Value | Behavior |
|-------|----------|
| `"command-template"` | Builds a command from `fileNameTemplate` and `argumentsTemplate`, then runs it. |
| `"uri-template"` | Expands `uriTemplate` and opens it via shell execution (like clicking a link). |

### `fallbackTrigger`

| Value | When the rule applies |
|-------|-----------------------|
| `"always"` | Every time the matched executable is launched. The fallback is used instead of the original. |
| `"access-denied"` | Only when the direct launch fails with an access-denied error. This is the default. |

### `matchFileNames` and `pathContains`

These two fields filter which launched executables the rule applies to.
At least one of them must match for the rule to fire.

- `matchFileNames` — list of file names matched case-insensitively (e.g., `["MyGame.exe"]`).
- `pathContains` — substring matched against the full path, with `\` normalized to `/` (e.g., `"steamapps/common/"`).

If both are specified, both must match.

## Template Placeholders

Templates in `fileNameTemplate`, `argumentsTemplate`, and `uriTemplate` support the following placeholders.
Windows environment variables (e.g., `%ProgramFiles%`) are also expanded.

| Placeholder | Value |
|-------------|-------|
| `{appId}` | Resolved app ID (from `appId` field or `appIdSource`). |
| `{product}` | Value of the `product` field. |
| `{patchline}` | Value of the `patchline` field (default: `"live"`). |
| `{launchPath}` | Full path of the launched executable. |
| `{launchDirectory}` | Directory containing the launched executable. |
| `{launchFileName}` | File name of the launched executable (with extension). |
| `{launchFileStem}` | File name of the launched executable (without extension). |
| `{launchPathQuoted}` | `{launchPath}` wrapped in double quotes. |
| `{launchDirectoryQuoted}` | `{launchDirectory}` wrapped in double quotes. |
| `{ancestorPath:Name}` | Path of the nearest ancestor directory named `Name`. |
| `{ancestorPathQuoted:Name}` | `{ancestorPath:Name}` wrapped in double quotes. |

### `{ancestorPath:Name}`

Walks up the directory tree from the launched executable until it finds a directory whose name
matches `Name` (case-insensitive). Returns that directory's full path.

Example — the Riot Games launcher lives in a fixed location relative to the game:

```json
"fileNameTemplate": "{ancestorPath:Riot Games}\\Riot Client\\RiotClientServices.exe"
```

If the game is at `C:\Riot Games\VALORANT\live\VALORANT.exe`, this expands to
`C:\Riot Games\Riot Client\RiotClientServices.exe`.

## App ID Sources

The `appIdSource` field controls how `{appId}` is resolved at runtime.
If `appId` is also set, it takes precedence and `appIdSource` is ignored.

### `steam-manifest`

Reads the Steam `appmanifest_*.acf` file in the same directory as the launched executable
and extracts the Steam App ID from it.

```json
"appIdSource": "steam-manifest"
```

Use this for Steam games where the executable is inside `steamapps/common/<game>/`.

### `registry:<hive>:<keyPath>:<valueName>`

Reads a string value from the Windows Registry.

Format:
```
registry:HIVE:KEY_PATH:VALUE_NAME
```

Supported hives:

| Hive | Description |
|------|-------------|
| `HKEY_LOCAL_MACHINE` | System-wide registry (64-bit view). |
| `HKEY_CURRENT_USER` | Per-user registry. |
| `HKEY_CLASSES_ROOT` | Merged class registrations. |
| `HKEY_USERS` | All user hives. |
| `HKEY_CURRENT_CONFIG` | Hardware profile. |

Example:

```json
"appIdSource": "registry:HKEY_LOCAL_MACHINE:SOFTWARE\\WOW6432Node\\Ubisoft\\Launcher\\Installs\\12345:UplayId"
```

Replace `12345` with the registry sub-key for your game.

### `static:<value>`

Uses a fixed string as the app ID with no runtime lookup.

```json
"appIdSource": "static:MyGameAppId"
```

Alternatively, set the `appId` field directly for the same effect:

```json
"appId": "MyGameAppId"
```
