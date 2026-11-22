# Hex Editor

A hex editor control, window and dialogs over any seekable stream, with bounded caching, pending edits, streaming search and a data inspector.

`HexEditorControl` is the designer-editable editor. Open content with `OpenFile(path, readOnly)`, `OpenStream(stream, ownsStream)` or `SetDocument(document, ownsDocument)`. Assigning `Document` borrows the document. The control derives from `ApplicationControl`, so it also works as a hosted screen component. `HexEditorForm` hosts the control with File, Edit and View menus, and its static `ShowAsync`, `ShowFileAsync` and `ShowBytesAsync` methods show it modally. `Tools.WinForms.Review` creates the control and shows the byte and text review dialogs.

`BytesPerRow` sets how many bytes each row shows, from 1 to 256 with a default of 16. The control's toolbar has a **Bytes per row** list with 8, 16, 24, 32, 48 and 64. For any other number, drag the line between the hex pane and the text pane: the mouse pointer changes over it, the line thickens, and pulling it right or left shows more or fewer bytes in each row as it moves. A number that is not in the list, whether dragged to or set in code, is added to the list while it is in use. `HexEditorView` raises `BytesPerRowRequested` during the drag and changes nothing itself; `HexEditorControl` applies the number. A host of the bare view handles the event and sets the viewport's `BytesPerRow`.

`ShowBytesAsync` edits the supplied array without replacing it or changing its length. It returns `true` when saved bytes have been copied into that array after the window closes, or `false` when nothing was saved. Pending edits use a working copy and only saved changes are copied back; read-only review and token cancellation leave the array unchanged. In this fixed-length mode insertion and deletion are disabled; pasted bytes must fit the remaining buffer and must exactly match an active selection's length. File and ordinary stream documents remain resizable.

The non-visual layer can be used and tested without a message loop. `HexEditorDocument` reads its stream through a `StreamByteCache` and keeps overwrites, insertions and deletions in a piece table until `Save()`. It derives from `SyncDisposable`, keeping stream ownership and cleanup in its synchronous disposal hook; asynchronous disposal uses the Framework worker adapter. `CanResize` controls whether edits may change its length. `HexEditorViewport` holds the visible rows, caret, selection, active pane and typing mode that `HexEditorView` paints. `HexEditorSearch` scans a document in chunks without loading it, and `HexEditorDataInspector` produces `HexEditorInspection` values for the bytes at the caret. Use all control members on the creating UI thread. `HexEditorControl.FindAsync` runs its search on a worker thread.

Saving a length change rewrites the stream from the first inserted or deleted byte to its end, which for a large file is a long write. `HexEditorControl.SaveAsync(cancellationToken)` therefore writes on a worker thread through `HexEditorDocument.SaveAsync`; the toolbar's Save, Ctrl+S and `HexEditorForm` use it. While it runs, `IsSaving` is true on both, the document reads as read-only and refuses edits, the status bar shows **Saving...**, and viewing, scrolling and searching go on: the save takes the document's lock one 64 KiB step at a time, and a read between steps sees the pending content (the rewritten tail comes from the staged copy until the stream holds it). `Changed` and `Saved` are raised in the caller's synchronization context after the flush. A second `SaveAsync` joins the running save, and the control refuses to replace the document until it ends; disposing the document waits for it, so the stream never holds part of the edits. The token is observed until the stream is first written. The synchronous `Save()` remains for small in-memory streams and non-UI callers. The project README describes the keyboard commands, save behaviour and display options in detail.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`HexEditorDataInspector`](HexEditorDataInspector.cs) | class | Reads up to eight bytes at an offset and interprets them in little- or big-endian order |
| [`HexEditorDocument`](HexEditorDocument.cs) | class | Editable stream content with a bounded cache, a piece table of pending edits, `Save()`, `SaveAsync()` and `Revert()` |
| [`HexEditorHitTestResult`](HexEditorHitTestResult.cs) | record | Pane, byte offset and nibble resolved from a pointer position in the view |
| [`HexEditorInspection`](HexEditorInspection.cs) | class | Integer, floating-point, ASCII and UTF-8 interpretations of the bytes at one offset |
| [`HexEditorInspectionEntry`](HexEditorInspectionEntry.cs) | record | One named, formatted interpretation shown in the data inspector |
| [`HexEditorPane`](HexEditorPane.cs) | enum | Identifies whether the hex pane or the text pane owns the caret and keyboard input |
| [`HexEditorSearch`](HexEditorSearch.cs) | class | Forward or backward byte-pattern search over a document, built from hexadecimal pairs or encoded text |
| [`HexEditorViewport`](HexEditorViewport.cs) | class | Non-visual view state with the row window, caret, selection, active pane, insert mode, typing and copy formats |
| [`StreamByteCache`](StreamByteCache.cs) | class | Bounded least-recently-used block cache over a seekable stream, with fetch statistics |
| [`Tools.WinForms.Review`](WindowsFormsEditorUtil.cs) | static class | The `Tools.WinForms.Review` facade that creates a hex editor and shows the byte and text review dialogs |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`HexEditorControl`](Controls/HexEditorControl.cs) | class | Complete hex editor with tool bar, bytes-per-row list, draggable line between the panes, status bar, data inspector, Go To, Find, clipboard commands and save |
| [`HexEditorView`](Controls/HexEditorView.cs) | class | Double-buffered control that paints a viewport as offset, hex and text columns and turns input into viewport operations |

### Forms

| Type | Kind | Purpose |
|---|---|---|
| [`HexEditorFindDialog`](Forms/HexEditorFindDialog.cs) | class | Prompts for a hexadecimal or UTF-8 text pattern and a search direction, and builds the `HexEditorSearch` |
| [`HexEditorForm`](Forms/HexEditorForm.cs) | class | Standalone hex editor window with menus, a modified marker in the title and confirmation before discarding edits |
| [`HexEditorGoToDialog`](Forms/HexEditorGoToDialog.cs) | class | Prompts for a hexadecimal or decimal offset within the document |
