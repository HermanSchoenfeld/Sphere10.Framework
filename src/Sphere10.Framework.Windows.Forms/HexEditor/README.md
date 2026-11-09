# Hex Editor

A hex editor control, window and dialogs over any seekable stream, with bounded caching, pending edits, streaming search and a data inspector.

`HexEditorControl` is the designer-editable editor. Open content with `OpenFile(path, readOnly)`, `OpenStream(stream, ownsStream)` or `SetDocument(document, ownsDocument)`. Assigning `Document` borrows the document. The control derives from `ApplicationControl`, so it also works as a hosted screen component. `HexEditorForm` hosts the control with File, Edit and View menus, and its static `ShowAsync`, `ShowFileAsync` and `ShowBytesAsync` methods show it modally. `Tools.Windows.Forms.Review` (`WindowsFormsEditorUtil`) creates the control and shows the byte and text review dialogs.

The non-visual layer can be used and tested without a message loop. `HexEditorDocument` reads its stream through a `StreamByteCache` and keeps overwrites, insertions and deletions in a piece table until `Save()`. `HexEditorViewport` holds the visible rows, caret, selection, active pane and typing mode that `HexEditorView` paints. `HexEditorSearch` scans a document in chunks without loading it, and `HexEditorDataInspector` produces `HexEditorInspection` values for the bytes at the caret. Use all control members on the creating UI thread. `HexEditorControl.FindAsync` runs its search on a worker thread. The project README describes the keyboard commands, save behaviour and display options in detail.

## Contents

| Type | Kind | Purpose |
|---|---|---|

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`HexEditorControl`](Controls/HexEditorControl.cs) | class | Complete hex editor with tool bar, status bar, data inspector, Go To, Find, clipboard commands and save |

### Forms

| Type | Kind | Purpose |
|---|---|---|
