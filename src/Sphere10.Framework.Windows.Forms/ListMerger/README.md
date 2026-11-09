# List Merger

A two-list control whose buttons move or copy the selected items between a left and a right list.

`ListMerger` shows `LeftItems` and `RightItems` in two list boxes with optional headers, `DisplayMember` and `ValueMember`. `MergeMode` decides what the buttons do. `Move` transfers the selected items to the other list. `CopyLeftToRight` copies left items to the right and lets the left button remove right items, and `CopyRightToLeft` works the other way round. Every transfer raises `ItemsMoved` with the source side, target side, mode and items, and a selection raises `ItemSelected`. Derived controls can override the protected move, copy and remove methods.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ListMergeMode`](ListMergeMode.cs) | enum | Whether the buttons move items, copy them from left to right or copy them from right to left |
| [`ListMergerSide`](ListMergerSide.cs) | enum | Identifies the left or right list of a `ListMerger` |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`ItemSelectedHandler`](Controls/ListMerger.cs) | delegate | Handler for `ListMerger.ItemSelected`, with the side and the selected item |
| [`ItemsMovedHandler`](Controls/ListMerger.cs) | delegate | Handler for `ListMerger.ItemsMoved`, with the source side, target side, mode and items |
| [`ListMerger`](Controls/ListMerger.cs) | class | Two list boxes with buttons that move or copy the selected items between them according to `MergeMode` |
