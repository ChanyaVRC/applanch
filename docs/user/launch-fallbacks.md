# Launch Fallbacks

When a registered executable cannot be launched directly (for example because a game uses a separate launcher), applanch can automatically redirect the launch through the correct launcher instead.

Built-in fallback rules are in `Config/launch-fallbacks.json`.
Custom rules should be added under `Config/UserDefined/launch-fallbacks/*.json`.

## Built-in Rules

### Enabled by Default

| Rule | Trigger | Method |
|------|---------|--------|
| Riot VALORANT | Always | Launches via `RiotClientServices.exe` |
| Riot League of Legends | Always | Launches via `RiotClientServices.exe` |
| Steam library executable | Always | Launches via `steam://rungameid/{appId}` URI |

### Samples (Disabled by Default)

The following rules are included as examples. Enable them and replace placeholder values to use them.

| Rule | Launcher | URI Template |
|------|---------|-------------|
| Epic Games sample | Epic Games Launcher | `com.epicgames.launcher://apps/{appId}?action=launch&silent=true` |
| Ubisoft Connect sample | Ubisoft Connect | `uplay://launch/{appId}/0` |
| EA app sample | EA app | `ea://launchgame/{appId}` |
| Battle.net sample | Battle.net | `battlenet://{appId}` |

## Rule Structure

Each rule object in the fallback config files has the following fields:

| Field | Description |
|-------|-------------|
| `name` | Display name (for identification only) |
| `kind` | `command-template` — launches an executable; `uri-template` — opens a URI |
| `enabled` | Set to `true` to activate the rule |
| `fallbackTrigger` | `always` — always use fallback; `access-denied` — only on access denied error |
| `matchFileNames` | List of executable file names this rule applies to |
| `pathContains` | Alternatively match by path substring (e.g., `steamapps/common/`) |
| `fileNameTemplate` | Path template for the launcher executable (`command-template` kind) |
| `argumentsTemplate` | Command-line arguments template |
| `uriTemplate` | URI template to open (`uri-template` kind) |
| `appIdSource` | How to resolve `{appId}`: `steam-manifest` or a registry path |
| `product` | Product identifier used in `{product}` template placeholder |
| `patchline` | Patchline identifier used in `{patchline}` template placeholder |

## Adding a Custom Rule

To add a rule for a game launcher not covered by the built-in entries:

1. Create a JSON file under `Config/UserDefined/launch-fallbacks/` (for example `ubisoft.json`).
2. Add your rule(s) under the `rules` array.
3. Set `"enabled": true` for active rules.
4. Fill in the appropriate `matchFileNames` and template fields.
5. Restart applanch.

**Example — Ubisoft Connect (enabled):**

```json
{
  "rules": [
    {
      "name": "My Ubisoft Game",
      "kind": "uri-template",
      "enabled": true,
      "matchFileNames": ["MyGame.exe"],
      "uriTemplate": "uplay://launch/{appId}/0",
      "appIdSource": "registry:HKEY_LOCAL_MACHINE:SOFTWARE\\WOW6432Node\\Ubisoft\\Launcher\\Installs\\12345:UplayId"
    }
  ]
}
```

Replace `12345` with the registry key for your specific game.
