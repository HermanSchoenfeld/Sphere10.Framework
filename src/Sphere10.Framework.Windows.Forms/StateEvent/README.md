# State Events

Model-aware `FormEx` and `UserControlEx` base classes and the per-control observers that turn native edits into one `StateChanged` notification.

`FormEx` and `UserControlEx` follow one template. On first load they call the priming hook (`PopulatePrimingData` or `InitializeUIPrimingData`) and `CopyModelToUI` inside an update scope. After that, any observed edit of a child control raises `StateChanged`, and `CopyUIToModel` runs first when `UpdateModelOnStateChanged` is `true`. `EnterUpdateScope` returns an `UpdateScope` that suppresses notifications while code changes the UI or model, and on dispose finishes the update according to `FinishedUpdateBehaviour`. Nested scopes do nothing, so only the outermost scope completes the update. `FormEx.CloseAction` turns the close button into hide or minimize. `UserControlEx.Disposables` holds resources released with the control.

Child observation is done by `IControlStateEventProvider` implementations. `ControlStateEventProviderManager.Instance` resolves a fresh provider for a control type by walking its base types, preferring providers registered in the Framework service provider under the control's full type name and falling back to the built-in table. Built-in providers cover standard input controls, containers, split and tab containers, tool strips, SourceGrid grids, charts, ExplorerBar groups, the padlock button and `CrudComboBox`. Providers borrow the control and only detach their listeners on `Clear` or dispose. [VisualGrid](../VisualGrid/README.md) and [ToolStrips](../ToolStrips/README.md) controls are observed through these providers.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ChartControlStateEventProvider`](ChartControlStateEventProvider.cs) | class | Observes completed selection and axis-view changes of a chart, ignoring data, painting and hover. |
| [`CommonControlStateEventProvider`](CommonControlStateEventProvider.cs) | class | Observes completed values and choices of standard editable controls such as text boxes, combo boxes, check boxes and pickers. |
| [`ContainerControlStateEventProvider`](ContainerControlStateEventProvider.cs) | class | Container observer for panels, group boxes, user controls and task panes. |
| [`ContainerControlStateEventProviderBase<TControl>`](ContainerControlStateEventProviderBase.cs) | class | Observes a container's children, follows additions and removals, and combines their notifications. |
| [`ControlStateEventProviderBase<TControl>`](ControlStateEventProviderBase.cs) | abstract class | Base observer for one borrowed control with typed register and deregister hooks and `StateChanged`. |
| [`ControlStateEventProviderManager`](ControlStateEventProviderManager.cs) | class | Resolves a fresh state observer for a control type from service registrations or built-in defaults. |
| [`CrudComboBoxControlStateEventProvider`](CrudComboBoxControlStateEventProvider.cs) | class | Observes the accepted entity of a `CrudComboBox`, even when two choices share a caption. |
| [`ExpandoControlStateEventProvider`](ExpandoControlStateEventProvider.cs) | class | Observes an ExplorerBar group's children and its collapsed or expanded state. |
| [`FinishedUpdateBehaviour`](FinishedUpdateBehaviour.cs) | enum | Flags for what an update scope does on completion: notify, force a UI-to-model copy, or copy the model to the UI. |
| [`FormCloseAction`](FormCloseAction.cs) | enum | Flags for what closing a `FormEx` does: close, hide, minimize or nothing. |
| [`IControlStateEventProvider`](IControlStateEventProvider.cs) | interface | Observer contract with `StateChanged`, the accepted control type, `SetControl` and `Clear`. |
| [`IControlStateEventProvider<TControl>`](IControlStateEventProvider.cs) | interface | Typed observer contract that exposes the observed control. |
| [`IUpdatable`](IUpdatable.cs) | interface | Disposable object with an `Updating` flag and internal begin and finish update members used by `UpdateScope`. |
| [`NoOpControlStateProvider`](NoOpControlStateProvider.cs) | class | Design-time observer that accepts a control and never raises `StateChanged`. |
| [`PadLockButtonControlStateEventProvider`](PadLockButtonControlStateEventProvider.cs) | class | Observes the lock state of a padlock button without treating clicks as edits. |
| [`SourceGridControlStateEventProvider`](SourceGridControlStateEventProvider.cs) | class | Observes cell value commits and selection changes of a SourceGrid `GridVirtual`. |
| [`SplitContainerControlStateEventProvider`](SplitContainerControlStateEventProvider.cs) | class | Combines state from both panels of a split container. |
| [`TabControlStateEventProvider`](TabControlStateEventProvider.cs) | class | Observes tab selection and the editable contents of each tab page. |
| [`ToolStripControlStateEventProvider`](ToolStripControlStateEventProvider.cs) | class | Observes retained tool strip and menu inputs and their dropdowns without treating commands as edits. |
| [`UpdateScope`](UpdateScope.cs) | struct | Scope that begins an update if none is active and finishes it with the requested behaviour on dispose. |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`UserControlEx`](Controls/UserControlEx.cs) | class | Model-aware user control with priming, model copy hooks, update scopes, owned disposables and `StateChanged`. |

### Forms

| Type | Kind | Purpose |
|---|---|---|
| [`FormEx`](Forms/FormEx.cs) | class | Model-aware form with priming, model copy hooks, update scopes, `StateChanged` and configurable close behaviour. |

`UserControlEx.StateEventProvider` and `FormEx.StateEventProvider` are public nested providers that relay a nested control's or form's `StateChanged` to its parent.
