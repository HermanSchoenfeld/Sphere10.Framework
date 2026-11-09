# Resources

Icon, image, cursor and theme files that the library embeds through its `Resources` class.

The folder holds no source code. `Properties/Resources.resx` links many of these files, and the generated public `Resources` class in [Properties/Resources.Designer.cs](../Properties/Resources.Designer.cs) exposes them as typed properties in the `Sphere10.Framework.Windows.Forms` namespace. For example, `SortIndicator` draws `Resources.IEW_SortUp` and `Resources.IEW_SortDown`, and `ExplorerBarInfo` reads its default theme from `Resources.LunaExplorerBarTheme`. The project file also includes `Add.ico`, `ArrowDown.ICO`, `ArrowUp.ICO`, `Delete.ICO` and `Refresh.ico` as content items.

## Contents

| Files | Resource properties | Purpose |
|---|---|---|
| `normalgroup*.png`, `specialgroup*.png` | `NORMALGROUP*`, `SPECIALGROUP*` | Expand and collapse glyphs, with hot variants, for explorer bar groups |
| `LunaExplorerBarTheme.xml` | `LunaExplorerBarTheme` | Default explorer bar theme |
| `sortup.ico`, `sortdown.ico`, `dropdown.ico` | `IEW_SortUp`, `IEW_SortDown`, `IEW_DropDown` | Sort indicator and drop-down glyphs |
| `left.cur`, `right.cur` | `CursorLeftArrow`, `CursorRightArrow` | Left and right arrow cursors |
| `LArrow.gif`, `RArrow.gif`, `MinArrow.png`, `MaxArrow.png`, `Plus.png`, `Minus.png` | `LArrow`, `RArrow`, `MinArrow`, `MaxArrow`, `Plus`, `Minus` | Small arrow, minimise, maximise, add and remove glyphs |
| `PadLockClosed.png`, `PadLockOpen.png` | `PadLockClosed`, `PadLockOpen` | Locked and unlocked images |
| `1377684292_cross.png`, `1377684316_tick.png`, `1377684165_5233.ico`, `1377684154_12514.ico` | `Cross`, `Tick`, `CrossIcon`, `TickIcon_24x24` | Cross and tick status images |
| `SmallEditIcon.png`, `SmallSearchIcon.png`, `Clipboard2.ico`, `Copy2.ico` | `SmallEditIcon`, `SmallSearchIcon`, `Clipboard_16x16`, `Copy_16x16` | Edit, search, clipboard and copy command glyphs |
| `grid.bmp`, `gridvirtual.bmp` | `Grid`, `GridVirtual` | Grid images, such as the `VisualGrid` page mode button image |
| `Help_16x16x32.ico`, `Help and Support.png`, `Shopping cart.png`, `sphere10_16x16x32.png` | `Help_16x16x32`, `Help_256x256`, `Purchase_16x16x32`, `Sphere10_16x16x32` | Help, purchase and product icons |
| `53.ico`, `104.ico`, `111.ico` to `115.ico`, `121.ico`, `122.ico`, `129.ico` | `*_128x128` | 128-pixel dialog icons for error, cross, information, question, success, shield and warning |
| Other numbered `.ico` files, `Clipboard.ico`, `Clipboard1.ico`, `Copy.ico`, `Copy1.ico`, `Buy_Now_Set.gif`, `Shopping-cart-64.png` | none | Files not linked by the resource file |
