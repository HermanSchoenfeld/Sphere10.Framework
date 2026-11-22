# VisualEntityEditor

Entity editing shared by [VisualGrid](../VisualGrid/README.md), modal entity
dialogs and [application settings](../Application/README.md). The family is
independent of the grid: a custom `IVisualEntityEditor<TEntity>` can be hosted by
the grid's edit dialog or supplied directly to the application settings host.

`VisualEntityEditor` implements the object editor with the existing `PropertyGridEx`.
It exposes hidden-property and filtering options, reference bindings, change
tracking, `UndoChanges` and `AcceptChanges`. Its internal type descriptor keeps
the edit session local and honors the model's existing converters and editors.
Related records use the [reference-picker family](../VisualGridDropDown/README.md).
A string property marked `[PathSelector(mode)]` gets the browse button of a
[`PathSelectorControl`](../PathSelector/README.md) through `PathSelectorEditor`,
and so does each entry of a marked string collection, whose own row keeps the
collection's editor. The editor is created directly rather than named by an
`[Editor]` attribute, so it needs no constructor preserved for trimming. A
read-only session offers no browse button.

`VisualEntityEditorDialog` hosts an editor and an `IDataSource<object>`, validates
create/update/delete operations and commits through that source. It accepts the
edit baseline only after persistence succeeds; cancellation undoes changes.
`VisualEntityEditorAdapter<TEntity>` supplies the existing typed-to-object bridge
when a caller hosts a typed editor directly. The source remains caller-owned;
the dialog owns its child editor control.

| Type | Purpose |
|---|---|
| [IVisualEntityEditor<TEntity>](IVisualEntityEditor.cs) | Entity assignment, change notification, undo/accept and native control contract. |
| [VisualEntityEditor](VisualEntityEditor.cs) | Property-grid implementation used by default and by application settings. |
| [VisualEntityEditorDialog](VisualEntityEditorDialog.cs) | Entity validation and save/delete/cancel workflow. |
| [VisualEntityEditorAdapterBase](VisualEntityEditorAdapterBase.cs) | Object editor bridge used by the hosting grid. |
| [VisualEntityEditorAdapter<TEntity>](VisualEntityEditorAdapter.cs) | Adapts a typed editor to the object editor contract. |
| [VisualEntityPropertyChangedEventArgs](VisualEntityPropertyChangedEventArgs.cs) | Entity, property name, old value and new value for a completed edit. |
| [VisualEntityTypeDescriptor](VisualEntityTypeDescriptor.cs) | Internal descriptor and rollback state for one editing session. |

The editor and dialog each keep their native layout in a matching `.Designer.cs`
partial and their resources in a matching `.resx`. Editor types passed to
`VisualGrid.SetEntityEditor` require a public parameterless constructor. Trimmed
applications must also retain reflected model properties; preserving the editor
constructor alone does not retain an arbitrary model graph.
