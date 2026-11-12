# VisualGrid

The data-source-driven CRUD grid control with paging, search, sorting, entity editors, reference pickers, row details and per-user grid preferences.

`VisualGrid` displays an `IDataSource<TEntity>` through a list of `IVisualGridColumn` bindings, usually `VisualGridColumn<TEntity>` instances assigned to `GridBindings`. Call `SetDataSource` to bind it. The grid reads only the visible page, and `PageMode` chooses manual pages, pages fitted to the viewport, or one complete page. `Capabilities` decides which create, edit, delete, search and sort commands appear. Create and edit open an entity editor in a `CrudEntityEditorDialog`. The editor is the `DefaultCrudEntityEditor` property grid, or a custom `ICrudEntityEditor<TEntity>` control set with `SetEntityEditor`. Reference properties use a `CrudReferenceBinding`, which shows a searchable `CrudReferencePicker` from a column, the property grid editor (`CrudReferenceEditor`) or a `CrudComboBox`. `RowDetails` adds owned panels or nested grids below rows. Their values are prepared on a worker by `PrepareAsync` and published on the UI thread.

Two controllers bind a grid without writing a data source by hand. `VisualGridDataSourceController<T>` binds a ranged source and refreshes it after source changes. `VisualGridArrayController<T>` publishes a collected or projected array of rows and keeps the selection by a caller-defined identity. `DataSourceViewSubscription<T>` coalesces a source's change notifications into refreshes on the owner's dispatcher. `Preferences` holds a `VisualGridSettings` object with page size, sort and column layout, and `VisualGridConfigurationContextMenu` saves or restores it from the header. `Tools.WinForms.Grids` creates these grids, controllers and detail adapters on the calling UI thread. The grid derives from `UserControlEx` and reports edits through [state events](../StateEvent/README.md), and it draws with [SourceGrid](../SourceGrid/README.md).

Editor types supplied to `SetEntityEditor` and its `CrudComboBox`/`CrudDialog` forwarding APIs require a public parameterless constructor. The annotated type contract retains that constructor when trimming. Editors are still created lazily for each edit. A property-grid editor also needs the reflected properties of its entity model to be retained by the application; retaining the editor's constructor alone does not preserve an arbitrary model graph.

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
| [`DataSourceViewSubscription<T>`](DataSourceViewSubscription.cs) | class | Disposable subscription that coalesces a borrowed source's notifications into refreshes on a control's dispatcher. |
| [`ICrudEntityEditor<TEntity>`](ICrudEntityEditor.cs) | interface | Entity editor control contract with entity assignment, change tracking, undo, accept and property change events. |
| [`IVisualGrid`](IVisualGrid.cs) | interface | Non-generic grid contract for title, capabilities, column bindings, data source assignment and entity refresh. |
| [`IVisualGridArrayController`](IVisualGridArrayController.cs) | interface | Non-generic array controller contract with row count, refresh, default sort and row capture. |
| [`IVisualGridColumn`](IVisualGridColumn.cs) | interface | Column binding contract for value access, display type, editing, drop-down items, buttons, sorting and image layout. |
| [`VisualGridArrayController<T>`](VisualGridArrayController.cs) | class | Publishes collected or projected typed rows to a grid through an array store and keeps selection by identity. |
| [`VisualGridColumn<TEntity>`](VisualGridColumn.cs) | class | Configurable typed column binding with getters, setters, display type, sort name, references and button callbacks. |
| [`VisualGridColumnsEditor`](VisualGridColumnsEditor.cs) | class | Collection editor for a screen's fixed columns that edits order, width and stretch without adding or removing columns. |
| [`VisualGridDataSourceController<T>`](VisualGridDataSourceController.cs) | class | Binds a ranged `IDataSource<T>` to a grid without loading the whole dataset and refreshes it after changes. |
| [`VisualGridDefaultsRestorePolicy`](VisualGridDefaultsRestorePolicy.cs) | enum | Whether restoring defaults from the header menu only restores the view or also saves the configuration. |
| [`VisualGridPageMode`](VisualGridPageMode.cs) | enum | Manual pages, viewport-fitted pages or one complete result page. |
| [`VisualGridRowStyle`](VisualGridRowStyle.cs) | record | Foreground and background colours for one row, where `Color.Empty` keeps the column colour. |
| [`VisualGridSettings`](VisualGridSettings.cs) | class | Per-user settings object with page mode, page size, page number, sort and column preferences for one grid. |
| [`Tools.WinForms.Grids`](WindowsFormsGridUtil.cs) | static class | Factory for grids, data source and array controllers, view subscriptions and row-detail adapters, exposed as `Tools.WinForms.Grids`. |

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
| [`VisualGridArrayDetail<T>`](RowDetails/VisualGridArrayDetail.cs) | class | Row detail that shows prepared child rows in a nested grid through an array store. |
| [`VisualGridControlDetail`](RowDetails/VisualGridControlDetail.cs) | class | Row detail that hosts an arbitrary control and disposes it and its supplied resources. |
| [`VisualGridDetail`](RowDetails/VisualGridDetail.cs) | abstract class | Base for one owned detail surface that applies prepared values on the UI thread and keeps child selection. |
| [`VisualGridRowDetails`](RowDetails/VisualGridRowDetails.cs) | class | Opt-in row detail configuration with identity, worker preparation, UI factory, height, column sharing and depth limit. |
