# Services

Services of the original component generation for modal dialogs, wizard building, typed event publishing and example endpoint and node data.

`AddSphere10Blazor` registers `IModalService` as `ModalService`, `IGenericEventAggregator` as `BasicGenericEventAggregator` and `IBlazorWizardBuilder<TModel>` as `BlazorWizardBuilder<TModel>`. `ModalService.Initialize` takes the rendered `Components.Modal.ModalHost` from [Components](../Components/README.md) and must run before any dialog is shown. The wizard builder here produces `Components.Wizard` wizards. The current generation's dialog services and wizard builder are in [Modal](../Modal/README.md) and [Wizard](../Wizard/README.md), and both builders share the fluent wizard state of Sphere10.Framework.Application.

`IEndpointManager` and `INodeService` are contracts without an implementation in this library. `ExtendedComponentViewModel` in [ViewModels](../ViewModels/README.md) requires an `IEndpointManager`, and `INodeService` returns the sample `Block` model from [Models](../Models/README.md). `BasicGenericEventAggregator` calls its subscribers synchronously on the publishing thread and returns a completed task.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BasicGenericEventAggregator`](BasicGenericEventAggregator.cs) | class | Thread-safe aggregator that keeps `Action<T>` subscribers per message type and invokes them when a `T` is published. |
| [`BlazorWizardBuilder<TModel>`](BlazorWizardBuilder.cs) | class | Builds a `Components.Wizard.BlazorWizard<TModel>` from step component types with the shared builder state and validation. |
| [`IBlazorWizardBuilder<TModel>`](IBlazorWizardBuilder.cs) | interface | Shared fluent wizard builder contract specialized for this generation's Blazor step components. |
| [`IEndpointManager`](IEndpointManager.cs) | interface | Current and known server endpoint URIs with change and add events and endpoint validation. |
| [`IGenericEventAggregator`](IGenericEventAggregator.cs) | interface | Subscribes, unsubscribes and publishes typed messages. |
| [`IModalService`](IModalService.cs) | interface | Initializes the original modal host and shows a `ModalComponentBase` type or a wizard as a dialog. |
| [`INodeService`](INodeService.cs) | interface | Example data service that streams `Block` items or returns one page of them for an `ItemRequest`. |
| [`ModalService`](ModalService.cs) | class | `IModalService` implementation that shows components and wizards in the initialized `Components.Modal.ModalHost`. |
