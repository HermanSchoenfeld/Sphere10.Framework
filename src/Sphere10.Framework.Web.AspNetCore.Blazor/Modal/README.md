# Modal

The current dialog services that show modal components, confirmations, information messages and wizards through `UI.Dialogs.ModalHost`.

`ModalService` and `ViewService` are scoped services that `AddSphere10Blazor` registers. `ModalService` needs a rendered `UI.Dialogs.ModalHost` in the same interactive scope, passed to `Initialize` after the first render, before any dialog is shown. `ViewService` builds on it with `ConfirmDialogAsync`, `DialogAsync` and `WizardDialogAsync`, the last of which shows a wizard built with the [Wizard](../Wizard/README.md) builder. The dialog components themselves are in the `Dialogs` folder of [UI](../UI/README.md).

A modal's close button always closes it. `ModalOptions.BackdropDismissal` decides what a click outside the modal, or the Escape key, does. `Close` closes it as the close button does, `Confirm` first asks "Are you sure you want to close?" with Yes and No above the modal, and `Ignore` leaves it open. When the caller sets no value, a wizard dialog uses `Confirm` and every other modal uses `Close`. Yes closes a wizard through the same cancellation as its close button, so its `IsCancellable` flag and `OnCancelled` callback still apply. A wizard that cannot be cancelled has no close button, is not asked about and stays open. No returns to the modal with its state and focus as they were.

```csharp
// A wizard that closes on a click outside without asking, and a confirmation that ignores one.
var result = await Views.WizardDialogAsync(wizard, new ModalOptions { Size = ModalSize.Large, BackdropDismissal = ModalBackdropDismissal.Close });
var parameters = ParameterView.FromDictionary(new Dictionary<string, object> { [nameof(Confirm.Message)] = "Delete this record?" });
var answer = await Modals.ShowAsync<Confirm>(parameters, new ModalOptions { BackdropDismissal = ModalBackdropDismissal.Ignore });
```

This generation does not mix with the original `IModalService` and `Components.Modal.ModalHost` from [Components](../Components/README.md). When one layout hosts both, give each host its own `Id` and initialize each service with its own host. `ModalResult` and `ModalResultType` here duplicate the types of the same name in `Components.Modal`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ModalBackdropDismissal`](ModalOptions.cs) | enum | `Close`, `Confirm` or `Ignore`, which is what a click outside a modal or the Escape key does. |
| [`ModalOptions`](ModalOptions.cs) | class | Init-only options of a modal. `Size` defaults to `Medium`, and `BackdropDismissal` is unset by default so that the modal's own default applies. |
| [`ModalResult`](ModalResult.cs) | class | Result of a modal interaction with a `ModalResultType` and optional data. |
| [`ModalResultType`](ModalResultType.cs) | enum | `Ok`, `Cancel` or `Exit`. |
| [`ModalService`](ModalService.cs) | class | Shows a `UI.Dialogs.ModalComponent` type with parameters and `ModalOptions` in the initialized `ModalHost` and returns its `ModalResult`; `IsInitialized` reports whether a host has been supplied. |
| [`ModalSize`](ModalOptions.cs) | enum | `Small`, `Medium` or `Large`. |
| [`ViewService`](ViewService.cs) | class | Shows confirmation, message, button-choice, content, exception and wizard dialogs through `ModalService` and returns their outcome. |
