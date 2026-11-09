# Dialogs

Awaitable message, question and exception dialogs with up to five custom buttons and a remembered "always" answer.

`DialogEx` is a message box replacement. Its static `ShowAsync` overloads accept either `MessageBoxButtons` and return a `DialogResult`, or custom button captions and return a `DialogExResult` that names the pressed button. The dialog sizes its message to the owner's monitor and scrolls long text. `ExceptionDialog` shows an error with a **Detail** button that opens the diagnostic text in a `TextEditorForm` from [Forms](../Forms/README.md). `QuestionDialog` adds an "always" check box, and `QuestionDialogSession` uses it to answer repeated questions automatically.

Every helper creates, awaits and disposes its own dialog. Override `OnProcessButtonAsync` to customize a derived dialog's button handling. The project [README](../README.md) describes async modal rules for WinForms.

```csharp
var answer = await DialogEx.ShowAsync(this, SystemIconType.Question, "Delete", "Delete the selected rows?", "&Delete", "&Cancel");
if (answer == DialogExResult.Button1) {
	// Delete the rows.
}
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DialogExResult`](DialogExResult.cs) | enum | Which of the up to five custom `DialogEx` buttons closed the dialog |
| [`QuestionDialogSession`](QuestionDialogSession.cs) | class | Asks questions by `Guid` and returns the stored answer without showing a dialog once the user ticked "always" |

### Forms

| Type | Kind | Purpose |
|---|---|---|
| [`DialogEx`](Forms/DialogEx.cs) | class | Monitor-fitted message dialog with an icon, custom buttons, optional "always" check box and awaitable `ShowAsync` overloads |
| [`ExceptionDialog`](Forms/ExceptionDialog.cs) | class | Error dialog that shows an exception's display text and opens its diagnostic text from **Detail** |
| [`QuestionDialog`](Forms/QuestionDialog.cs) | class | `DialogEx` with the "always" check box enabled, used by `QuestionDialogSession` |
