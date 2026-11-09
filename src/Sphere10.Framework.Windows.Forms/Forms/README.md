# Forms

General-purpose dialogs and forms for entering text, reviewing text before saving, editing an object in a property grid and viewing text.

`EnterTextDialog.ShowAsync` asks for one line of text and returns `(bool Accepted, string UserInput)`. `ReviewTextDialog.ShowAsync` previews generated text read-only, offers **Copy** and **Save**, and returns the saved path or null. Its save writes a temporary copy before replacing the destination. `GenericEditorForm.ShowFormAsync` edits any object in a property grid, optionally read-only. `TextEditorForm` shows text in a text box and backs the detail view of `ExceptionDialog`.

The async helpers create, await and dispose their dialogs. Message and question dialogs live in [Dialogs](../Dialogs/README.md), and credential dialogs in [Authentication](../Authentication/README.md). The project [README](../README.md) describes `ReviewTextDialog` cancellation and save behaviour in detail.

```csharp
var (accepted, userInput) = await EnterTextDialog.ShowAsync(this, "Rename", "New name", "Current name");
if (accepted)
	await DialogEx.ShowAsync(this, SystemIconType.Information, "Renamed", userInput, "OK");
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`EnterTextDialog`](EnterTextDialog.cs) | class | Single text input dialog with instructions and prefill, returned by `ShowAsync` as `(Accepted, UserInput)` |
| [`GenericEditorForm`](GenericEditorForm.cs) | class | Property grid form that edits or views any object, shown with `ShowFormAsync` |
| [`ReviewTextDialog`](ReviewTextDialog.cs) | class | Read-only text preview with Copy and Save that writes through a temporary file and returns the saved path |
| [`TextEditorForm`](TextEditorForm.cs) | class | Form that shows supplied text in a text box |
