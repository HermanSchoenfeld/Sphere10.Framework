# VisualGridDropDown

This family presents [VisualGrid](../VisualGrid/README.md) in dropdown hosts.
`VisualGridDropDown` is a standalone `CustomComboBox` with a grid as its popup.
Configure it with `SetParametersAsync`, read `SelectedEntity`, and observe
`EntitySelectionChanged`. Its capabilities permit the same configured entity
operations as the grid; `AutoHideOnSelect` controls closing after a user choice.

`VisualGridReferenceBinding<TEntity>` describes eligible existing records,
columns, display text, null support and popup size. Assign it to a column's
`ReferenceBinding` or an entity editor's `ReferenceBindings`. The shared
`VisualGridReferenceEditor` opens `VisualGridReferencePicker` through the native
`IWindowsFormsEditorService` for both property-grid and inline cell editing.
The picker exposes read-only lookup operations, Clear when null is allowed, and
Cancel. An accepted choice preserves the entity reference; dismissal without
acceptance retains the original value. Source-driven selection reconciliation
does not accept a choice or close either host.

The combo control and the reference picker reuse the grid's query and selection
implementation. Their native hosts own different lifetimes: the combo retains
its popup control, while the property editor creates and disposes a picker for
each edit. Their sources are borrowed.

| Type | Purpose |
|---|---|
| [VisualGridDropDown](VisualGridDropDown.cs) | Standalone grid-backed dropdown with selected-entity display. |
| [VisualGridReferencePicker](VisualGridReferencePicker.cs) | Popup content for choosing, clearing or cancelling an existing reference. |
| [VisualGridReferenceBindingBase](VisualGridReferenceBindingBase.cs) | Shared reference configuration and read-only grid policy. |
| [VisualGridReferenceBinding<TEntity>](VisualGridReferenceBinding.cs) | Typed eligible source and display-text function. |
| [VisualGridReferenceEditor](VisualGridReferenceEditor.cs) | Internal native property-editor integration. |
| [VisualGridReferenceConverter](VisualGridReferenceConverter.cs) | Internal bound-reference display conversion. |

`MaximumDropDownSize` defines the configured content size in device pixels;
native hosts cap it to the available screen. `ExpandsToFit` selects which columns
share spare width. See the [WinForms reference-column example](../README.md#references-from-a-data-source)
for column and property-path configuration. The
[SourceGrid editor](../SourceGrid/Cells/Editors/VisualGridReference.cs) and
[state-event provider](../StateEvent/VisualGridDropDownControlStateEventProvider.cs)
stay with their existing integration owners.
