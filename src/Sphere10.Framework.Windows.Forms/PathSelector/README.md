# Path Selector

A text box with a browse button for choosing a file or folder path.

`PathSelectorControl` opens an open-file, save-file, generic file or folder dialog according to `Mode`, and raises `PathChanged` when `Path` changes. `ForcePathExists` makes the dialogs require an existing path and checks a typed path when the text box loses focus. The check runs on a worker, because a path on a slow or unreachable share can take many seconds to answer, and the focus moves on meanwhile. `PathChanged` follows once the path is found. A path that does not exist sets `IsPathMissing`, does not raise `PathChanged`, and returns the focus to the box while its form is active; leaving the box again checks it again. `ValidationCompletion` completes when the latest check has been applied, and setting `Path` from code clears `IsPathMissing` and discards a check still running. `OverwritePrompt` controls the save dialog's overwrite question, and `Editable` allows typing the path. `Filter` sets the file dialogs' file types, `DialogTitle` the dialog's caption and `InitialDirectory` the folder a dialog starts in while the box is empty; with a path in the box, a file dialog starts in its folder. `Browse(owner)` opens the same dialog as the browse button, over the given window or the control, puts the chosen path in the box and returns whether one was chosen. `PlaceHolderText` sets the text box's placeholder. The nested `PathSelectorControl.StateEventProvider` reports `PathChanged` as the control's state change to the library's control state notifications.

A settings or entity property that holds a path is marked with `[PathSelector(mode)]`, which carries the same `ForcePathExists`, `OverwritePrompt`, `Filter` and `DialogTitle` as the control. The [entity and settings editors](../VisualEntityEditor/README.md) then give its property grid row the selector's browse button through `PathSelectorEditor`, a type editor whose "..." button opens the dialog of a `PathSelectorControl` configured from the attribute and holding the row's path. The path can still be typed into the row; a property grid hosts only type editors, so the row's own text box takes the typed text and the editor does not check it for existence. On a `string[]` or other string collection, each entry gets the browse button and the collection keeps its own editor for adding and removing entries. `PathSelectorEditor.CreateSelector` and `Browse` are the overridable steps.

```csharp
[UserEditable]
public class CaptureSettings : SettingsObject {
	[PathSelector(PathSelectionMode.Folder, DialogTitle = "Capture Folder")]
	public string OutputDirectory { get; set; } = string.Empty;

	[PathSelector(PathSelectionMode.Folder, DialogTitle = "Symbol Folder")]
	public string[] SymbolDirectories { get; set; } = Array.Empty<string>();
}
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`PathSelectionMode`](PathSelectionMode.cs) | enum | Chooses the open-file, save-file, generic file or folder dialog the selector opens |
| [`PathSelectorAttribute`](PathSelectorAttribute.cs) | attribute | Marks a string property, or each string of a string collection, as a path chosen with the selector's dialog, with its mode, filter, caption and existence rule |
| [`PathSelectorEditor`](PathSelectorEditor.cs) | class | Property grid type editor whose "..." button opens the dialog of a `PathSelectorControl` configured from a `PathSelectorAttribute` |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`PathSelectorControl`](Controls/PathSelectorControl.cs) | class | Path text box and browse button with selection mode, existence checks on a worker (`IsPathMissing`, `ValidationCompletion`), dialog filter, caption and initial folder, overwrite prompt, `Browse` and `PathChanged` |
