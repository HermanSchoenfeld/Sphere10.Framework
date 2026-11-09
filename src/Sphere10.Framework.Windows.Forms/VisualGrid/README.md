# VisualGrid

The data-source-driven CRUD grid control with paging, search, sorting, entity editors, reference pickers, row details and per-user grid preferences.

`VisualGrid` displays an `IDataSource<TEntity>` through a list of `IVisualGridColumn` bindings, usually `VisualGridColumn<TEntity>` instances assigned to `GridBindings`. Call `SetDataSource` to bind it. The grid reads only the visible page, and `PageMode` chooses manual pages, pages fitted to the viewport, or one complete page. `Capabilities` decides which create, edit, delete, search and sort commands appear. Create and edit open an entity editor in a `CrudEntityEditorDialog`. The editor is the `DefaultCrudEntityEditor` property grid, or a custom `ICrudEntityEditor<TEntity>` control set with `SetEntityEditor`. Reference properties use a `CrudReferenceBinding`, which shows a searchable `CrudReferencePicker` from a column, the property grid editor (`CrudReferenceEditor`) or a `CrudComboBox`. `RowDetails` adds owned panels or nested grids below rows. Their values are prepared on a worker by `PrepareAsync` and published on the UI thread.

Two controllers bind a grid without writing a data source by hand. `VisualGridDataSourceController<T>` binds a ranged source and refreshes it after source changes. `VisualGridArrayController<T>` publishes a collected or projected array of rows and keeps the selection by a caller-defined identity. `DataSourceViewSubscription<T>` coalesces a source's change notifications into refreshes on the owner's dispatcher. `Preferences` holds a `VisualGridSettings` object with page size, sort and column layout, and `VisualGridConfigurationContextMenu` saves or restores it from the header. `Tools.Windows.Forms.Grids` (`WindowsFormsGridUtil`) creates these grids, controllers and detail adapters on the calling UI thread. The grid derives from `UserControlEx` and reports edits through [state events](../StateEvent/README.md), and it draws with [SourceGrid](../SourceGrid/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CrudCellDisplayType`](CrudCellDisplayType.cs) | enum | How a column cell is shown and edited: text, boolean, currency, numeric, date and time kinds, drop-down, button, edit or delete command, empty or image. |
| [`CrudEntityEditorAdapter`](CrudEntityEditorAdapter.cs) | abstract class | Untyped `ICrudEntityEditor<object>` view of a typed entity editor, used by the editor dialog. |
| [`CrudEntityEditorAdapter<TEntity>`](CrudEntityEditorAdapter.cs) | class | Adapts an `ICrudEntityEditor<TEntity>` to the untyped editor contract. |
| [`CrudEntityPropertyChangedEventArgs`](CrudEntityPropertyChangedEventArgs.cs) | class | Event data for an edited entity property with the entity, property name and old and new values. |
| [`CrudReferenceBinding`](CrudReferenceBinding.cs) | abstract class | Describes the records offered when editing an entity reference, with columns, null support and drop-down size. |
| [`CrudReferenceBinding<TEntity>`](CrudReferenceBinding.cs) | class | Reference binding over an `IDataSource<TEntity>` with an optional display text function. |
| [`CrudReferenceEditor`](CrudReferenceEditor.cs) | class | `UITypeEditor` that shows the reference picker drop-down in a `PropertyGrid` and in inline grid editors. |
| [`ICrudEntityEditor<TEntity>`](ICrudEntityEditor.cs) | interface | Entity editor control contract with entity assignment, change tracking, undo, accept and property change events. |
| [`IVisualGrid`](IVisualGrid.cs) | interface | Non-generic grid contract for title, capabilities, column bindings, data source assignment and entity refresh. |
| [`IVisualGridColumn`](IVisualGridColumn.cs) | interface | Column binding contract for value access, display type, editing, drop-down items, buttons, sorting and image layout. |
| [`VisualGridColumn<TEntity>`](VisualGridColumn.cs) | class | Configurable typed column binding with getters, setters, display type, sort name, references and button callbacks. |
| [`VisualGridColumnsEditor`](VisualGridColumnsEditor.cs) | class | Collection editor for a screen's fixed columns that edits order, width and stretch without adding or removing columns. |
| [`VisualGridDefaultsRestorePolicy`](VisualGridDefaultsRestorePolicy.cs) | enum | Whether restoring defaults from the header menu only restores the view or also saves the configuration. |
| [`VisualGridPageMode`](VisualGridPageMode.cs) | enum | Manual pages, viewport-fitted pages or one complete result page. |
| [`VisualGridSettings`](VisualGridSettings.cs) | class | Per-user settings object with page mode, page size, page number, sort and column preferences for one grid. |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`CrudComboBox`](Controls/CrudComboBox.cs) | class | Combo box whose drop-down is a `VisualGrid` over a data source, exposing the chosen `SelectedEntity`. |
| [`CrudReferencePicker`](Controls/CrudReferencePicker.cs) | class | Searchable, paged picker of existing records that raises accepted and cancelled events without editing the record. |
| [`DefaultCrudEntityEditor`](Controls/DefaultCrudEntityEditor.cs) | class | Property-grid entity editor with reference bindings, hidden properties, filtering and change tracking. |
| [`VisualGrid`](Controls/VisualGrid.cs) | class | Paged, searchable CRUD grid over an `IDataSource<TEntity>` with editors, references, row details and preferences. |
| [`VisualGridConfigurationContextMenu`](Controls/VisualGridConfigurationContextMenu.cs) | class | Header context menu that saves the grid configuration or restores its defaults. |

### Forms

| Type | Kind | Purpose |
|---|---|---|
| [`CrudDialog`](Forms/CrudDialog.cs) | class | Modal dialog that hosts a `VisualGrid` for a data source, shown with the static `ShowAsync` overloads. |
| [`CrudEntityEditorDialog`](Forms/CrudEntityEditorDialog.cs) | class | Modal dialog that hosts an entity editor and validates, saves, deletes or cancels the edit. |

### RowDetails

| Type | Kind | Purpose |
|---|---|---|
