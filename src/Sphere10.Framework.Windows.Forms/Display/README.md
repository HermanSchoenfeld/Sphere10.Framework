# Display

Monitor arrangement control that draws the desktop's displays as selectable tiles, in the style of Windows display settings.

`MonitorLayoutControl` scales the `Bounds` of each `MonitorLayoutItem` into the client area and draws one tile per display. The application supplies each item's key, label, caption, accent color and enabled state. Selection raises `SelectedItemChanged`, and `HitTest` and `GetTileBounds` map between points and tiles. `HostedControlMode` places caller controls under each tile with `SetTileControl`, or one `SharedControl` under the whole arrangement. The control draws only what it is given and does not query monitors itself.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`MonitorLayoutItem`](MonitorLayoutItem.cs) | class | One display tile with key, label, caption, desktop bounds, primary flag, enabled state and accent color |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`MonitorLayoutControl`](Controls/MonitorLayoutControl.cs) | class | Draws `MonitorLayoutItem` tiles to scale with selection, hit testing and hosted controls beneath them |
