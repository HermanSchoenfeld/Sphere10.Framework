# Modal

The current dialog services that show modal components, confirmations, information messages and wizards through `UI.Dialogs.ModalHost`.

`ModalService` and `ViewService` are scoped services that `AddSphere10Blazor` registers. `ModalService` needs a rendered `UI.Dialogs.ModalHost` in the same interactive scope, passed to `Initialize` after the first render, before any dialog is shown. `ViewService` builds on it with `ConfirmDialogAsync`, `DialogAsync` and `WizardDialogAsync`, the last of which shows a wizard built with the [Wizard](../Wizard/README.md) builder. The dialog components themselves are in the `Dialogs` folder of [UI](../UI/README.md).

This generation does not mix with the original `IModalService` and `Components.Modal.ModalHost` from [Components](../Components/README.md). When one layout hosts both, give each host its own `Id` and initialize each service with its own host. `ModalResult` and `ModalResultType` here duplicate the types of the same name in `Components.Modal`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ModalOptions`](ModalOptions.cs) | class | Init-only options of a modal, currently its `ModalSize`, which defaults to `Medium`. |
| [`ModalResult`](ModalResult.cs) | class | Result of a modal interaction with a `ModalResultType` and optional data. |
| [`ModalResultType`](ModalResultType.cs) | enum | `Ok`, `Cancel` or `Exit`. |
| [`ModalService`](ModalService.cs) | class | Shows a `UI.Dialogs.ModalComponent` type with parameters and `ModalOptions` in the initialized `ModalHost` and returns its `ModalResult`. |
| [`ModalSize`](ModalOptions.cs) | enum | `Small`, `Medium` or `Large`. |
| [`ViewService`](ViewService.cs) | class | Shows confirmation, message, button-choice, content, exception and wizard dialogs through `ModalService` and returns their outcome. |
