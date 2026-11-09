# Tool Strips

Tool strip, menu strip and status strip helpers for merging items into an inherited form's strips, keeping toolbar height stable, and choosing a value from a toolbar button.

The Windows Forms designer does not let a derived form edit a strip declared on its base form. `MergeableToolStrip`, `MergeableMenuStrip` and `MergeableStatusStrip` are placed on the derived form, point `InheritedToolStrip` at the base form's strip, and move their items into it with `Merge`. They return the items with `Unmerge` before they are hidden, detached or disposed. `ToolStripVisualInheritanceFixer` is a component that does the same for an ordinary hidden `ToolStrip`. `MergeInto` can also combine top-level menu headings by caption. Explicit `MergeAction` and `MergeIndex` settings on items are respected.

`ToolStripStableHeightBehavior` keeps a horizontal toolbar at a height that fits its full command set, or at a fixed logical height, so hiding commands does not move the content below. Dispose it to restore the original sizing. `ToolStripChoiceSelector` is a drop-down button that shows the selected item of a list as its text and offers the items as checked menu entries. Edits on tool strip inputs are observed for [state events](../StateEvent/README.md) by `ToolStripControlStateEventProvider`.

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
| [`ToolStripChoiceSelector`](Controls/ToolStripChoiceSelector.cs) | class | Non-editable toolbar choice shown as a drop-down button with a checked menu, raising `SelectedIndexChanged`. |
