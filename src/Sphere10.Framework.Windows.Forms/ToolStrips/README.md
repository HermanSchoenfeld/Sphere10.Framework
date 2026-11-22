# Tool Strips

Tool strip, menu strip and status strip helpers for merging items into an inherited form's strips, keeping toolbar height stable, choosing a value from a toolbar button, adjusting a number with a toolbar slider, turning an option on and off with a toolbar check box, starting or stopping an operation from one command, and offering toolbar commands from menus.

The Windows Forms designer does not let a derived form edit a strip declared on its base form. `MergeableToolStrip`, `MergeableMenuStrip` and `MergeableStatusStrip` are placed on the derived form, point `InheritedToolStrip` at the base form's strip, and move their items into it with `Merge`. They return the items with `Unmerge` before they are hidden, detached or disposed. `ToolStripVisualInheritanceFixer` is a component that does the same for an ordinary hidden `ToolStrip`. `MergeInto` can also combine top-level menu headings by caption. Explicit `MergeAction` and `MergeIndex` settings on items are respected.

`ToolStripStableHeightBehavior` keeps a horizontal toolbar at a height that fits its full command set, or at a fixed logical height, so hiding commands does not move the content below. Dispose it to restore the original sizing. `ToolStripChoiceSelector` is a drop-down button that shows the selected item of a list as its text and offers the items as checked menu entries. It sizes itself to its widest choice, so the toolbar keeps its layout when the selection changes and its height follows the font at any display scale; leave it auto-sized and give a list of unbounded names a `MaximumWidth` in logical pixels. A designer's fixed item sizes are scaled to the display with `ToolStrip.ScaleFixedItemSizes()` (see [Extensions](../Extensions/README.md)). Edits on tool strip inputs are observed for [state events](../StateEvent/README.md) by `ToolStripControlStateEventProvider`.

`ToolStripTrackBar` is a slider for a tool strip or status strip, for a whole-number setting the user adjusts by dragging, such as a zoom level or a number of columns. It hosts a standard track bar without tick marks. Set `Minimum`, `Maximum` and `LargeChange`, read or assign `Value`, and follow `ValueChanged`. An assigned value outside the range is brought to its nearer end, and assigning the value the slider already has raises nothing.

```csharp
var bytesPerRow = new ToolStripTrackBar { Minimum = 1, Maximum = 64, LargeChange = 8, Value = 16 };
bytesPerRow.ValueChanged += () => _editor.BytesPerRow = bytesPerRow.Value;
_toolStrip.Items.Add(bytesPerRow);
```

`ToolStripCheckBox` is a real check box on a tool strip or status strip, for a view option the user turns on and off, such as "Flatten Window Hierarchy". It hosts a standard `CheckBox` (exposed as `CheckBox`) with its caption beside the box on the strip's own background. Set `Text`, read or assign `Checked`, and follow `CheckedChanged`. Assigning the state the box already has raises nothing.

```csharp
var flatten = new ToolStripCheckBox { Text = "Flatten Window Hierarchy" };
flatten.CheckedChanged += () => _windows.Flatten = flatten.Checked;
_toolStrip.Items.Add(flatten);
```

`ToolStripStartStopButton` is one toolbar command for an operation that is either stopped or running. It shows a green start triangle with `StartText` while stopped and a red stop square with `StopText` while running, with `StartToolTipText` or `StopToolTipText` as its tool tip, and a click raises `StartRequested` or `StopRequested`. The owner sets `Running` when the operation's state changes. A click only requests the change, so a declined or failed start leaves the button as it was.

`ToolStripCommandMenu` is a menu whose entries mirror toolbar commands as they are when the menu opens, so each command keeps one owner for its caption, image, state and handler. Add the borrowed commands with `AddCommands`, `AddCommand` (optionally under a caption of its own) and `AddSeparator`. Buttons become entries, drop-down buttons become submenus of their own items, and labels and hosted controls are left out. A hidden command has no entry and a disabled command has a disabled one. Choosing an entry clicks the command it mirrors. While no command is available the menu shows the disabled `EmptyText` entry.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ToolStripStableHeightBehavior`](ToolStripStableHeightBehavior.cs) | class | Disposable behaviour that sizes a horizontal tool strip for all its commands, or a fixed logical height, across DPI changes. |
| [`ToolStripVisualInheritanceFixer`](ToolStripVisualInheritanceFixer.cs) | class | Component that merges a hidden tool strip's items into an inherited strip and unmerges them on dispose. |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`MergeableMenuStrip`](Controls/MergeableMenuStrip.cs) | class | Menu strip whose items merge into a base form's `MenuStrip`, optionally matching menu headings by text. |
| [`MergeableStatusStrip`](Controls/MergeableStatusStrip.cs) | class | Status strip whose items merge into a base form's `StatusStrip`. |
| [`MergeableToolStrip`](Controls/MergeableToolStrip.cs) | class | Tool strip whose items and hosted controls merge into a base form's `ToolStrip`. |
| [`ToolStripCheckBox`](Controls/ToolStripCheckBox.cs) | class | Check box hosted on a tool strip or status strip for an on/off view option, with `Checked`, the hosted `CheckBox` and the `CheckedChanged` event. |
| [`ToolStripChoiceSelector`](Controls/ToolStripChoiceSelector.cs) | class | Non-editable toolbar choice shown as a drop-down button with a checked menu, raising `SelectedIndexChanged`. |
| [`ToolStripCommandMenu`](Controls/ToolStripCommandMenu.cs) | class | Menu whose entries mirror borrowed toolbar commands as they are when it opens, and click the mirrored command when chosen. |
| [`ToolStripStartStopButton`](Controls/ToolStripStartStopButton.cs) | class | One toolbar command for an operation that is stopped or running, showing a green start triangle with `StartText` or a red stop square with `StopText` and raising `StartRequested` or `StopRequested`. |
| [`ToolStripTrackBar`](Controls/ToolStripTrackBar.cs) | class | Slider hosted on a tool strip or status strip for a whole-number setting, with `Minimum`, `Maximum`, `Value`, `LargeChange` and the `ValueChanged` event. |
