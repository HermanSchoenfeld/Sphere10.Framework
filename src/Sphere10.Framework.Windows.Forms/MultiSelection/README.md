# Multi-Selection Button

A split button that offers a default action and a menu of alternative actions.

`MultiSelectionButton` implements `IButtonControl`, so a form can use it as its accept or cancel button. Add `SubButtonItem` entries to `ButtonsItems`. With `Mode` set to `InvokeFirstAction`, the main button runs the first item, and the arrow opens a menu of all items. With `ShowContextMenu`, the main button opens the menu. Selecting an item runs its own `Click` handlers and then raises the button's `Click` event with a `SubButtonItemEventArgs` that names the item.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`MultiSelectionButtonMode`](MultiSelectionButtonMode.cs) | enum | Whether the main button runs its first action or opens the menu of actions |
| [`SubButtonItem`](SubButtonItem.cs) | class | One action of the button, with text, optional image, tag and `Click` handler |
| [`SubButtonItemEventArgs`](SubButtonItemEventArgs.cs) | class | Event arguments that identify the selected `SubButtonItem` |
| [`SubButtonItemEventHandler`](SubButtonItem.cs) | delegate | Handler for `MultiSelectionButton.Click` with the selected item |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`MultiSelectionButton`](Controls/MultiSelectionButton.cs) | class | Button with a default action and a drop-down menu of alternative `SubButtonItem` actions |
