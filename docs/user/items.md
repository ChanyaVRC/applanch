# Items & Categories

## Adding Items

Type a path or app name into the quick-add input at the top of the main window.
applanch shows up to 10 suggestions based on installed applications and recent input.
Press **Enter**, click **Add**, or select a suggestion to add the item.

You can also drag and drop a file or folder directly onto the main window.

## Launching Items

Click an item to launch it. The post-launch behavior (close, minimize, or keep open) is configurable in [Settings](settings.md).

## Context Menu Actions

Right-click an item (or use the action buttons that appear on hover) to access:

| Action | Description |
|--------|-------------|
| Rename | Change the display name shown in the list |
| Edit Category | Assign or change the category |
| Edit Arguments | Set command-line arguments passed at launch |
| Run as Administrator | Launch this item elevated (once) |
| Open Location | Open the item's parent folder in File Explorer |
| Delete | Remove the item from the list |

## Arguments

Use **Edit Arguments** to pass command-line arguments to an item when it launches.

Examples:

| Item | Arguments | Effect |
|------|-----------|--------|
| Chrome | `--incognito` | Opens in incognito mode |
| Notepad | `C:\logs\app.log` | Opens a specific file |
| PowerShell | `-NoProfile -Command "Get-Date"` | Runs a command directly |
| Custom tool | `--env production --verbose` | Sets flags for the tool |

Arguments are appended after the path when the item is launched, exactly as entered.

## Categories

Categories appear in the left panel of the main window.
Click a category to filter the item list.
Click **All** to show all items regardless of category.

The default category for new items is **General**.

## Reordering Items

Drag items in the list to reorder them.
The new order is saved automatically.

## Deleting Items

Delete an item via the context menu or the delete button that appears on hover.

After deletion, a notification appears at the bottom of the window.
Click **Undo** in the notification to immediately restore the item to its original position.

If [Confirm Before Delete](settings.md#confirm-before-delete) is enabled in settings, a confirmation dialog appears before the item is removed.

## Sort Modes

Sort behavior can be configured in [Settings](settings.md):

**Category Sort Mode**

| Mode | Behavior |
|------|----------|
| Alphabetical (default) | Categories sorted A–Z |
| As Added | Categories appear in the order they were first used |

**App List Sort Mode**

| Mode | Behavior |
|------|----------|
| Manual (default) | Items ordered by drag-and-drop position |
| Name | Items sorted alphabetically by display name |
| Category Then Name | Items sorted by category first, then by name |
