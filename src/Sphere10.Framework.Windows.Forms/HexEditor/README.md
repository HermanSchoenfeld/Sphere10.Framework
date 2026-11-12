# Hex Editor

A hex editor control, window and dialogs over any seekable stream, with bounded caching, pending edits, streaming search and a data inspector.

`HexEditorControl` is the designer-editable editor. Open content with `OpenFile(path, readOnly)`, `OpenStream(stream, ownsStream)` or `SetDocument(document, ownsDocument)`. Assigning `Document` borrows the document. The control derives from `ApplicationControl`, so it also works as a hosted screen component. `HexEditorForm` hosts the control with File, Edit and View menus, and its static `ShowAsync`, `ShowFileAsync` and `ShowBytesAsync` methods show it modally. `Tools.WinForms.Review` creates the control and shows the byte and text review dialogs.

The non-visual layer can be used and tested without a message loop. `HexEditorDocument` reads its stream through a `StreamByteCache` and keeps overwrites, insertions and deletions in a piece table until `Save()`. `HexEditorViewport` holds the visible rows, caret, selection, active pane and typing mode that `HexEditorView` paints. `HexEditorSearch` scans a document in chunks without loading it, and `HexEditorDataInspector` produces `HexEditorInspection` values for the bytes at the caret. Use all control members on the creating UI thread. `HexEditorControl.FindAsync` runs its search on a worker thread. The project README describes the keyboard commands, save behaviour and display options in detail.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`HexEditorDataInspector`](HexEditorDataInspector.cs) | class | Reads up to eight bytes at an offset and interprets them in little- or big-endian order |
| [`HexEditorDocument`](HexEditorDocument.cs) | class | Editable stream content with a bounded cache, a piece table of pending edits, `Save()` and `Revert()` |
| [`HexEditorHitTestResult`](SpecialTypes.cs) | record | Pane, byte offset and nibble resolved from a pointer position in the view |
| [`HexEditorInspection`](HexEditorInspection.cs) | class | Integer, floating-point, ASCII and UTF-8 interpretations of the bytes at one offset |
| [`HexEditorInspectionEntry`](SpecialTypes.cs) | record | One named, formatted interpretation shown in the data inspector |
| [`HexEditorPane`](HexEditorPane.cs) | enum | Identifies whether the hex pane or the text pane owns the caret and keyboard input |
| [`HexEditorSearch`](HexEditorSearch.cs) | class | Forward or backward byte-pattern search over a document, built from hexadecimal pairs or encoded text |
| [`HexEditorViewport`](HexEditorViewport.cs) | class | Non-visual view state with the row window, caret, selection, active pane, insert mode, typing and copy formats |
| [`StreamByteCache`](StreamByteCache.cs) | class | Bounded least-recently-used block cache over a seekable stream, with fetch statistics |
| [`Tools.WinForms.Review`](WindowsFormsEditorUtil.cs) | static class | The `Tools.WinForms.Review` facade that creates a hex editor and shows the byte and text review dialogs |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`HexEditorControl`](Controls/HexEditorControl.cs) | class | Complete hex editor with tool bar, status bar, data inspector, Go To, Find, clipboard commands and save |
| [`HexEditorView`](Controls/HexEditorView.cs) | class | Double-buffered control that paints a viewport as offset, hex and text columns and turns input into viewport operations |

### Forms

| Type | Kind | Purpose |
|---|---|---|
| [`HexEditorFindDialog`](Forms/HexEditorFindDialog.cs) | class | Prompts for a hexadecimal or UTF-8 text pattern and a search direction, and builds the `HexEditorSearch` |
| [`HexEditorForm`](Forms/HexEditorForm.cs) | class | Standalone hex editor window with menus, a modified marker in the title and confirmation before discarding edits |
| [`HexEditorGoToDialog`](Forms/HexEditorGoToDialog.cs) | class | Prompts for a hexadecimal or decimal offset within the document |
