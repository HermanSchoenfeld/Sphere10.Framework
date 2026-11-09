# Authentication

Modal logon and password-entry dialogs that delegate credential checks and password policy to caller-supplied callbacks.

`LogonDialog` collects a user name and password and passes them to an asynchronous authenticator that returns an `AuthenticationResult`. The dialog closes on `AuthenticationErrorCode.Success` and otherwise lists the error. When the authenticator throws, the dialog reports the exception and records `ServerUnavailable`. `PasswordDialog` asks for a password and validates it against an optional policy callback that returns error messages.

Prefer the static `ShowAsync` helpers. They create the dialog, await native `ShowDialogAsync` and dispose it. `LogonDialog.ShowAsync` returns the dialog's `LogonResult`. The Cancel button records `AuthenticationErrorCode.Aborted`, and closing the window before any attempt leaves the result null. Other modal helpers are in [Dialogs](../Dialogs/README.md) and [Forms](../Forms/README.md).

## Contents

### Forms

| Type | Kind | Purpose |
|---|---|---|
| [`LogonDialog`](Forms/LogonDialog.cs) | class | User name and password dialog that awaits a `Func<string, string, Task<AuthenticationResult>>` authenticator and exposes `LogonResult` |
| [`PasswordDialog`](Forms/PasswordDialog.cs) | class | Password entry dialog with an optional policy validator, returned by `ShowAsync` as `(DialogResult, Password)` |
