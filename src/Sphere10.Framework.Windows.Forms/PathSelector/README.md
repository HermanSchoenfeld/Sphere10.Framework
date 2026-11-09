# Path Selector

A text box with a browse button for choosing a file or folder path.

`PathSelectorControl` opens an open-file, save-file, generic file or folder dialog according to `Mode`, and raises `PathChanged` when `Path` changes. `ForcePathExists` makes the dialogs require an existing path and rejects a typed path that does not exist when the text box loses focus. `OverwritePrompt` controls the save dialog's overwrite question, and `Editable` allows typing the path. `PlaceHolderText` sets the text box's placeholder. The nested `PathSelectorControl.StateEventProvider` reports `PathChanged` as the control's state change to the library's control state notifications.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`PathSelectionMode`](PathSelectionMode.cs) | enum | Chooses the open-file, save-file, generic file or folder dialog the selector opens |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`PathSelectorControl`](Controls/PathSelectorControl.cs) | class | Path text box and browse button with selection mode, existence checks, overwrite prompt and `PathChanged` |
