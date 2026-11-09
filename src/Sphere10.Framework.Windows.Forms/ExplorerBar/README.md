# Explorer Bar

Windows XP style explorer bar with collapsible, animated task groups and link-style task items, plus its theme settings and serialization surrogates.

This domain adapts Mathew Hall's BSD-licensed XPExplorerBar, and its source files keep that notice. A `TaskPane` is a scrollable container of `Expando` groups. Each `Expando` has a title bar that collapses or expands its content with animation, and holds `TaskItem` links or other controls. `BlockMainForm` in [Application](../Application/README.md) builds its navigation pane from these controls.

Colors, fonts, borders, padding and images come from an `ExplorerBarInfo` theme made of `TaskPaneInfo`, `ExpandoInfo`, `HeaderInfo` and `TaskItemInfo`. `ExplorerBarInfo.Default` loads the bundled Luna theme, and `TaskPane.UseCustomTheme` applies another one. The `*Surrogate` classes are serializable copies of the theme and control state, because the live objects hold resources that do not serialize. `Border`, `Margin` and `PaddingEx` are the mutable edge-size classes these settings use.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Border`](Border.cs) | class | Mutable left, top, right and bottom border widths |
| [`ExpandoEventArgs`](ExpandoEventArgs.cs) | class | Event data with the affected `Expando` and whether it is collapsed |
| [`ExpandoInfo`](ExpandoInfo.cs) | class | Theme settings for normal and special `Expando` back colors, back images, borders, padding and watermark alignment |
| [`ExpandoInfoSurrogate`](ExpandoInfoSurrogate.cs) | class | Serializable copy of an `ExpandoInfo` |
| [`ExpandoSurrogate`](ExpandoSurrogate.cs) | class | Serializable copy of an `Expando`'s state, settings and child items |
| [`ExplorerBarInfo`](ExplorerBarInfo.cs) | class | Complete explorer bar theme with task pane, expando, header and task item settings and a default Luna theme |
| [`ExplorerBarInfoSurrogate`](ExplorerBarInfoSurrogate.cs) | class | Serializable copy of an `ExplorerBarInfo` theme |
| [`FocusStates`](FocusStates.cs) | enum | Whether the mouse is over an `Expando` title bar |
| [`HeaderInfo`](HeaderInfo.cs) | class | Theme settings for `Expando` title bars, including fonts, colors, gradients, images and arrows |
| [`HeaderInfoSurrogate`](HeaderInfoSurrogate.cs) | class | Serializable copy of a `HeaderInfo` |
| [`ImageStretchMode`](ImageStretchMode.cs) | enum | How a theme image fills an object, such as tiled, stretched or ARGB |
| [`Margin`](Margin.cs) | class | Mutable left, top, right and bottom margins |
| [`PaddingEx`](PaddingEx.cs) | class | Mutable left, top, right and bottom padding |
| [`TaskItemInfo`](TaskItemInfo.cs) | class | Theme settings for `TaskItem` margin, padding, link colors and font decoration |
| [`TaskItemInfoSurrogate`](TaskItemInfoSurrogate.cs) | class | Serializable copy of a `TaskItemInfo` |
| [`TaskItemSurrogate`](TaskItemSurrogate.cs) | class | Serializable copy of a `TaskItem`'s state and settings |
| [`TaskPaneInfo`](TaskPaneInfo.cs) | class | Theme settings for `TaskPane` gradient, background image and stretch mode, watermark and padding |
| [`TaskPaneInfoSurrogate`](TaskPaneInfoSurrogate.cs) | class | Serializable copy of a `TaskPaneInfo` |
| [`TaskPaneSurrogate`](TaskPaneSurrogate.cs) | class | Serializable copy of a `TaskPane`'s state, settings and expandos |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`Expando`](Controls/Expando.cs) | class | Collapsible, animated group with a themed title bar that holds task items and other controls |
| [`ExpandoEventHandler`](Controls/Expando.cs) | delegate | Handler for `Expando` state changes and `TaskPane` expando added and removed events |
| [`RoundedPanel`](Controls/RoundedPanel.cs) | class | Panel with rounded corners, optional border, drop shadow, gradient and image |
| [`TaskItem`](Controls/TaskItem.cs) | class | Link-style button that shows text and an image inside an `Expando` |
| [`TaskPane`](Controls/TaskPane.cs) | class | Scrollable container of `Expando` groups that applies an explorer bar theme |
