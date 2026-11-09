# Components

The original view-model component generation with route search, a loading overlay, modal dialogs, display tables and a wizard host.

Components here derive from `ComponentWithViewModel<TViewModel>`. It injects a view model based on `ComponentViewModelBase` from [ViewModels](../ViewModels/README.md), starts its `InitAsync` once and dispatches its state-change notifications onto the renderer. `AddSphere10Blazor` registers this generation's services. It also calls `AddViewModelsFromAssembly`, which registers every exported, non-abstract class of an assembly whose name contains "ViewModel" as a transient service. The dialog API of this generation is `IModalService` in [Services](../Services/README.md), which needs a rendered `Components.Modal.ModalHost` passed to `Initialize`.

The current application workspace uses the generation in [UI](../UI/README.md), with the dialog services in [Modal](../Modal/README.md) and wizards in [Wizard](../Wizard/README.md). Both generations declare names such as `ModalHost`, `PagedTable` and `BlazorWizardHost`, so import one namespace locally or use fully qualified component tags. `SearchInput` in this folder is the one that the README's header-search example uses, and it returns `Models.SearchResult` links from [Models](../Models/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ComponentWithViewModel<TViewModel>`](ComponentWithViewModel.cs) | abstract class | Base component that injects its view model, initializes it once and marshals its notifications onto the renderer. |
| [`SearchInput`](SearchInput.razor.cs) | class | Route search box that calls a `SearchProvider` delegate as the user types and renders the results as keyboard-accessible links. |

### Modal

| Type | Kind | Purpose |
|---|---|---|
| [`BlazorWizardModal`](Modal/BlazorWizardModal.razor.cs) | class | Modal component that shows a `Components.Wizard` wizard in a dialog, with configurable button classes. |
| [`BlazorWizardModalViewModel`](Modal/BlazorWizardModalViewModel.cs) | class | View model of `BlazorWizardModal` that holds the wizard and its host and decides whether the dialog can close. |
| [`ModalComponent<TViewModel>`](Modal/ModalComponent.razor.cs) | abstract class | Base modal content component bound to a `ModalViewModel`, whose `ShowAsync` completes with the modal result. |
| [`ModalComponentBase`](Modal/ModalComponent.razor.cs) | abstract class | Non-generic modal content base that signals when it has rendered and exposes `ShowAsync` and `OnCloseAsync`. |
| [`ModalHost`](Modal/ModalHost.razor.cs) | class | Hosts one `ModalComponentBase` at a time until its interaction finishes. |
| [`ModalHostBase<TComponent, TResult>`](Modal/ModalHostBase.cs) | abstract class | Renders modal content and owns the isolated JavaScript module that shows and hides the host element, shared by both modal hosts. |
| [`ModalResult`](Modal/ModalResult.cs) | class | Result of a modal interaction with a `ModalResultType` and optional data. |
| [`ModalResultType`](Modal/ModalResultType.cs) | enum | `Ok`, `Cancel` or `Exit`. |

### Tables

| Type | Kind | Purpose |
|---|---|---|
| [`IPagedCollectionViewModel`](Tables/IPagedCollectionViewModel.cs) | interface | Page size, current and total pages and next and previous page operations that `Pagination` binds to. |
| [`PageSizeSelector`](Tables/PageSizeSelector.razor) | class | Two-way bound page-size dropdown with configurable options. |
| [`PagedTable<TItem>`](Tables/PagedTable.razor.cs) | class | Table that pages an in-memory collection through `PagedTableViewModel<TItem>`. |
| [`PagedTableViewModel<TItem>`](Tables/PagedTableViewModel.cs) | class | View model that slices an item collection into pages and tracks the current page. |
| [`RapidTable<TItem>`](Tables/RapidTable.razor.cs) | class | Table that renders items from an asynchronous stream up to an item limit. |
| [`RapidTableViewModel<TItem>`](Tables/RapidTableViewModel.cs) | class | Keeps a bounded list of streamed items until cancellation or disposal. |
| [`VirtualPagedTable<TItem>`](Tables/VirtualPagedTable.razor.cs) | class | Table that requests each page from an asynchronous items provider. |
| [`VirtualPagedTableViewModel<TItem>`](Tables/VirtualPagedTableViewModel.cs) | class | View model that requests pages from an `ItemRequest` provider and tracks total items and pages. |

### Wizard

| Type | Kind | Purpose |
|---|---|---|
| [`BlazorWizard<TModel>`](Wizard/BlazorWizard.cs) | class | Shared wizard engine whose steps are Blazor component types, for this component generation. |
| [`BlazorWizardHost`](Wizard/BlazorWizardHost.razor.cs) | class | Renders the current wizard step with next, back, cancel and finish buttons and raises finished, cancelled and step-change callbacks. |
| [`BlazorWizardHostViewModel`](Wizard/BlazorWizardHostViewModel.cs) | class | View model of `BlazorWizardHost` that performs navigation, validation, finishing and cancellation and collects error messages. |
| [`BlazorWizardStep<TModel, TViewModel>`](Wizard/BlazorWizardStep.razor.cs) | abstract class | Base step component bound to a `BlazorWizardStepViewModelBase<TModel>` view model. |
| [`BlazorWizardStepBase`](Wizard/BlazorWizardStep.razor.cs) | abstract class | Non-generic step base that implements the shared `IWizardStep` traits, title, button texts and next and previous callbacks. |
| [`BlazorWizardStepViewModelBase<TModel>`](Wizard/BlazorWizardStepViewModelBase.cs) | abstract class | Base step view model with access to the wizard and its model and overridable next and previous handlers. |
| [`IBlazorWizard`](Wizard/IBlazorWizard.cs) | interface | Shared `IWizard<Type>` contract rendered by this component generation. |
| [`IBlazorWizard<TModel>`](Wizard/IBlazorWizard.cs) | interface | Typed wizard contract of this generation with a model of type `TModel`. |
