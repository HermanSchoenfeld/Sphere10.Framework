# VisualGridDialog

[VisualGridDialog](VisualGridDialog.cs) presents a borrowed `IDataSource<TEntity>`
in a modal [VisualGrid](../VisualGrid/README.md). Configure columns and allowed
operations through `ShowAsync`, or call `SetParametersAsync` on a form you own.
The grid supplies searching, paging, selection and entity editing; the dialog
does not keep a second collection or binding pipeline.

The static `ShowAsync` overloads dispose the form after it closes. They use
[VisualEntityEditor](../VisualEntityEditor/README.md) by default and accept a
custom editor type with a public parameterless constructor. Configuration made
before handle creation is applied on load, then the grid is refreshed. The data
source remains caller-owned.

| File | Responsibility |
|---|---|
| [VisualGridDialog.cs](VisualGridDialog.cs) | Configuration, delayed initialization and modal lifetime. |
| [VisualGridDialog.Designer.cs](VisualGridDialog.Designer.cs) | Native form layout and child-control disposal. |
| [VisualGridDialog.resx](VisualGridDialog.resx) | Designer resources. |

Use [VisualGridDropDown](../VisualGridDropDown/README.md) for selection in a
dropdown, or [VisualEntityEditorDialog](../VisualEntityEditor/VisualEntityEditorDialog.cs)
to edit a single entity rather than display a table.
