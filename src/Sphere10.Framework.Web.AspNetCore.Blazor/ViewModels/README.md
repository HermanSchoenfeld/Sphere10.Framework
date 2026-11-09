# ViewModels

Base view models for the original component generation, including the modal view model that completes a dialog's result.

A component based on `Components.ComponentWithViewModel<TViewModel>` in [Components](../Components/README.md) injects one of these view models. The component assigns the view model's `StateHasChangedDelegate` and `InvokeAsyncDelegate`, so the view model can request a render and run work on the renderer, and it calls `InitAsync` once. `InitAsync` runs the overridable `InitCoreAsync` and then sets `IsInitialized`. `ModalViewModel` backs the original modal components and completes the task that `ShowAsync` returns when the dialog finishes. `ExtendedComponentViewModel` reruns `InitCoreAsync` whenever its `IEndpointManager` from [Services](../Services/README.md) reports an endpoint change.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ComponentViewModelBase`](ComponentViewModelBase.cs) | abstract class | Base view model with render and dispatch delegates set by its component and an `InitAsync` that runs `InitCoreAsync` and sets `IsInitialized`. |
| [`ExtendedComponentViewModel`](ExtendedComponentViewModel.cs) | abstract class | View model that holds an `IEndpointManager` and reinitializes when the current endpoint changes. |
| [`ModalViewModel`](ModalViewModel.cs) | class | Modal view model whose `ShowAsync` result completes through `Ok`, `OkData`, `Cancel` or `RequestCloseAsync`. |
