# Themes

applanch ships with three built-in themes and supports fully custom theme palettes.

## Built-in Themes

| Theme | Description |
|-------|-------------|
| Follow system | Automatically switches between Light and Dark based on the Windows setting |
| Light | Light background, dark text |
| Dark | Dark background (navy), light text |

Switch themes in [Settings](settings.md) under **Appearance → Theme**.

## Custom Themes

Built-in themes are loaded from `Config/theme-palette.json`.
Add your own themes under `Config/UserDefined/theme-palette/*.json`.

For the full JSON field reference, see [Theme JSON Format](theme-format.md).

### Adding a Custom Theme

1. Create a JSON file under `Config/UserDefined/theme-palette/` (for example `my-theme.json`).
2. Define your custom themes in that file.
3. Restart applanch.

```json
{
  "themes": [
    {
      "id": "my-theme",
      "displayNames": {
        "en": "My Theme",
        "ja": "マイテーマ"
      },
      "entries": [
        { "key": "Brush.AppBackground", "hex": "#1E1E2E" },
        { "key": "Brush.Surface",       "hex": "#27273A" }
      ]
    }
  ]
}
```

The file is merged with built-in themes, so you only need to define what you want to add or override.

### Referencing Another Theme

Instead of specifying `entries` directly, set `entriesFrom` to inherit all brush values from an existing theme:

- Use a string value when one theme should always inherit from the same source theme.
- Use an object with `light` and `dark` when the source theme should change with the Windows light/dark mode.
- The referenced theme ID can be either a built-in theme such as `light` / `dark`, or a custom theme ID that you defined in another JSON file.

This is useful when you want to add an alias theme or reuse an existing palette without repeating the full `entries` list.

Always inherit from a single theme:

```json
{
  "id": "my-light-copy",
  "displayNames": { "en": "My Light Copy" },
  "entriesFrom": "light"
}
```

Switch the inherited theme based on the Windows color mode:

```json
{
  "id": "my-dark-variant",
  "displayNames": { "en": "Dark Variant" },
  "entriesFrom": {
    "light": "light",
    "dark": "dark"
  }
}
```

When the Windows setting is light, the `light` source theme ID is used; when dark, the `dark` source theme ID is used.
This is how the built-in **Follow system** theme works.
