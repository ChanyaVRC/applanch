# Theme JSON Format

This page describes the JSON structure used for custom themes.

## Top-Level Structure

Theme files use a `themes` array:

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
        { "key": "Brush.AppBackground", "hex": "#1E1E2E" }
      ]
    }
  ]
}
```

## Theme Object Fields

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `id` | string | ✓ | Theme ID used internally and by `entriesFrom`. It is normalized to lowercase. Reusing an existing ID overrides that theme. |
| `displayNames` | `{ [languageCode]: string }` | | Labels shown in Settings. |
| `entries` | array | | Brush values defined directly on this theme. |
| `entriesFrom` | `string` or `{ light?: string, dark?: string }` | | Inherits brush values from another theme. |

### `id`

- Use a unique string for new themes.
- Built-in IDs include `system`, `light`, and `dark`.
- If you reuse `light` or `dark`, your file overrides that built-in theme.

### `displayNames`

Use an object whose keys are language codes and whose values are display strings:

```json
"displayNames": {
  "en": "My Theme",
  "ja": "マイテーマ"
}
```

Shape:

```ts
{
  [languageCode: string]: string
}
```

- Currently supported `languageCode` values:
  - `en`
  - `ja`
- If omitted, built-in themes use their built-in localized names.
- Custom themes fall back to a title-cased name derived from `id`.

### `entries`

`entries` is an array of objects with this shape:

```json
{ "key": "Brush.AppBackground", "hex": "#1E1E2E" }
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `key` | string | ✓ | Brush resource name used by the UI. |
| `hex` | string | ✓ | Color in `#RRGGBB` or `#AARRGGBB` format. |

Missing brush keys fall back to inherited or built-in values.

## `entriesFrom`

Use `entriesFrom` when you want to reuse another theme instead of redefining every brush.

Always inherit from one theme:

```json
{
  "id": "my-light-copy",
  "entriesFrom": "light"
}
```

Switch inherited themes by Windows color mode:

```json
{
  "id": "my-dark-variant",
  "entriesFrom": {
    "light": "light",
    "dark": "dark"
  }
}
```

| Form | Type | Meaning |
|------|------|---------|
| `"light"` | string | Always inherit from one theme ID. |
| `{ "light": "...", "dark": "..." }` | `{ light?: string, dark?: string }` | Choose the source theme based on Windows light/dark mode. |

Object shape:

```ts
{
  light?: string;
  dark?: string;
}
```

Use `light` and/or `dark` as property names. Their values must be theme IDs.

If `entries` and `entriesFrom` are both present, `entries` provides this theme's own colors and `entriesFrom` fills in the remaining keys.

## Available Brush Keys

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