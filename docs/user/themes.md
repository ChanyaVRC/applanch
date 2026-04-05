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

Custom themes are defined in `Config/theme-palette.json` inside the applanch installation folder.

### Adding a Custom Theme

Add a new entry to the `themes` array:

```json
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
```

Restart applanch to see the new theme in the Settings dropdown.

### Available Brush Keys

| Key | Purpose |
|-----|---------|
| `Brush.AppBackground` | Main window background |
| `Brush.Surface` | Cards, panels, dialog backgrounds |
| `Brush.SurfaceBorder` | Borders of surface elements |
| `Brush.TextPrimary` | Primary text color |
| `Brush.TextSecondary` | Secondary / muted text |
| `Brush.TextTertiary` | Disabled text, scrollbar thumbs |
| `Brush.ItemBackground` | Individual item row background |
| `Brush.ItemBorder` | Individual item row border |
| `Brush.IconBackground` | Icon placeholder background |
| `Brush.NotificationInfoBackground` | Info notification background |
| `Brush.NotificationInfoBorder` | Info notification border |
| `Brush.NotificationWarningBackground` | Warning notification background |
| `Brush.NotificationWarningBorder` | Warning notification border |
| `Brush.NotificationErrorBackground` | Error notification background |
| `Brush.NotificationErrorBorder` | Error notification border |
| `Brush.NotificationProgressTrack` | Progress bar track |
| `Brush.NotificationProgressValue` | Progress bar fill |
| `Brush.QuickAddInfoText` | Quick-add informational message text |
| `Brush.QuickAddWarningText` | Quick-add warning message text |

### Referencing Another Theme

Instead of specifying `entries` directly, set `entriesFrom` to inherit all brush values from an existing theme:

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

When the Windows setting is light, the `light` theme id is used; when dark, the `dark` id is used.
This is how the built-in **Follow system** theme works.
