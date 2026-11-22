# VisualGrid

The data-source-driven VisualGrid control with paging, search, sorting, entity editors, reference pickers, row details and per-user grid preferences.

`VisualGrid` displays an `IDataSource<TEntity>` through a list of `IVisualGridColumn` bindings, usually `VisualGridColumn<TEntity>` instances assigned to `GridBindings`. Await `SetDataSource` to bind it, then `RefreshGrid` to load its rows. The grid reads only the visible page, and `PageMode` chooses manual pages, pages fitted to the viewport, or one complete page. `Capabilities` decides which create, edit, delete, search and sort commands appear. Create and edit open an entity editor in a `VisualEntityEditorDialog`. The editor is the `VisualEntityEditor` property grid, or a custom `IVisualEntityEditor<TEntity>` control set with `SetEntityEditor`. Reference properties use the [reference binding and picker](../VisualGridDropDown/README.md) from inline cells and the property-grid editor. `VisualGridDropDown` hosts the same grid in a standalone selection control. A bound `IHierarchicalDataSource<TNode>` enables child expansion automatically. `CreateDetail` selects an owned panel or nested grid using the source-resolved `VisualGridDetailContext`; ordinary panels use `HasDetail` and `DetailIdentity`.

Use `VisualGrid.ConfigureDataSource` and `RefreshDataSourceAsync` to configure deferred binding and publish changes from a ranged source. `IsDataSourceBound` reports completed capability binding, independently of whether a page read has succeeded. Selection reconciliation, publication identity and invalidation belong to the grid; its data-source controller is internal. `VisualGridArrayController<T>` publishes a collected or projected array of rows through that same grid API and keeps the selection by a caller-defined identity. Its optional `viewSource` binds a hierarchy decorator over the array store while the controller continues publishing into that store.

The grid owns its source subscription and uses `Throttle.ExecuteAsync` to coalesce presentation on its dispatcher. Automatic reads, retries and publication defer while the view is inactive, retaining a pending update for reactivation. An explicitly requested refresh may still run while hidden, including when it joins an automatic refresh already in progress.

`Preferences` holds the working `VisualGridSettings` values for page size, sort and column layout. Persistence ownership is separate: `VisualGridConfigurationContextMenu` saves or restores those values from the header when the grid has a settings binding. A standalone grid may have applied preferences without owning saved settings. `Tools.WinForms.Grids` creates grids, array controllers and detail adapters on the calling UI thread. The grid derives from `UserControlEx` and reports edits through [state events](../StateEvent/README.md), and it draws with [SourceGrid](../SourceGrid/README.md).

Editor types supplied to `SetEntityEditor` and its `VisualGridDropDown`/`VisualGridDialog` forwarding APIs require a public parameterless constructor. The annotated type contract retains that constructor when trimming. Editors are still created lazily for each edit. A property-grid editor also needs the reflected properties of its entity model to be retained by the application; retaining the editor's constructor alone does not preserve an arbitrary model graph.

## Source organization

The grid, its row details and its implementation helpers share this flat folder.
The sibling [VisualGridDialog](../VisualGridDialog/README.md),
[VisualGridDropDown](../VisualGridDropDown/README.md) and
[VisualEntityEditor](../VisualEntityEditor/README.md) folders own their independent
components. They share the `Sphere10.Framework.Windows.Forms` namespace.
`VisualGrid` remains one type, organized by responsibility; each partial keeps
its related state and operations.

| File | Responsibility |
|---|---|
| [VisualGrid.cs](VisualGrid.cs) | Construction and shared interaction state. |
| [VisualGrid.Binding.cs](VisualGrid.Binding.cs) | Source assignment, refresh, queries and accepted page publication. |
| [VisualGrid.Cells.cs](VisualGrid.Cells.cs) | Column bindings, cell construction, prepared values and cell views. |
| [VisualGrid.Editing.cs](VisualGrid.Editing.cs) | Entity editors, CRUD commands and post-edit updates. |
| [VisualGrid.Interaction.cs](VisualGrid.Interaction.cs) | Selection, mouse and keyboard input, and row context menus. |
| [VisualGrid.Layout.cs](VisualGrid.Layout.cs) | Header/footer layout, loading surfaces, column widths and row geometry. |
| [VisualGrid.Paging.cs](VisualGrid.Paging.cs) | Search, sorting, page navigation and automatic page capacity. |
| [VisualGrid.RowDetails.cs](VisualGrid.RowDetails.cs) | Detail configuration, layout, selection and disposal tracking. |
| [VisualGrid.Notifications.cs](VisualGrid.Notifications.cs) | Source change presentation and edit deferral. |
| [VisualGrid.Preferences.cs](VisualGrid.Preferences.cs) | Saved preferences and shared column layouts. |
| [VisualGrid.Designer.cs](VisualGrid.Designer.cs) | Designer-authored native controls and disposal. |

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`VisualGridCellDisplayType`](VisualGridCellDisplayType.cs) | enum | How a column cell is shown and edited: text, boolean, currency, numeric, date and time kinds, drop-down, button, edit or delete command, empty or image. |
| [`IVisualGrid`](IVisualGrid.cs) | interface | Non-generic grid contract for title, capabilities, column bindings, data source assignment and entity refresh. |
| [`IVisualGridArrayController`](IVisualGridArrayController.cs) | interface | Non-generic array controller contract with row count, refresh, default sort and row capture. |
| [`IVisualGridColumn`](IVisualGridColumn.cs) | interface | Column binding contract for value access, display type, editing, drop-down items, buttons, sorting and image layout. |
| [`VisualGridArrayController<T>`](VisualGridArrayController.cs) | class | Publishes collected or projected typed rows to a grid through an array store and keeps selection by identity. |
| [`VisualGridColumn<TEntity>`](VisualGridColumn.cs) | class | Configurable typed column binding with getters, setters, display type, sort name, references and button callbacks. |
| [`VisualGridColumnsEditor`](VisualGridColumnsEditor.cs) | internal class | Collection editor for a screen's fixed columns that edits order, width and stretch without adding or removing columns. |
| [`VisualGridDataSourceController`](VisualGridDataSourceController.cs) | internal class | Grid-owned binding implementation; consumers configure and refresh the `VisualGrid` directly. |
| [`VisualGridDefaultsRestorePolicy`](VisualGridDefaultsRestorePolicy.cs) | enum | Whether restoring defaults from the header menu only restores the view or also saves the configuration. |
| [`VisualGridPageMode`](VisualGridPageMode.cs) | enum | Manual pages, viewport-fitted pages or one complete result page. |
| [`VisualGridRowStyle`](VisualGridRowStyle.cs) | record | Foreground and background colours for one row, where `Color.Empty` keeps the column colour. |
| [`VisualGridSettings`](VisualGridSettings.cs) | class | Per-user settings object with page mode, page size, page number, sort and column preferences for one grid. |
| [`Tools.WinForms.Grids`](WindowsFormsGridUtil.cs) | static class | Factory for grids, array controllers and row-detail adapters, exposed as `Tools.WinForms.Grids`. |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`VisualGrid`](VisualGrid.cs) | class | Paged, searchable VisualGrid over an `IDataSource<TEntity>` with editors, references, row details and preferences. |
| [`VisualGridConfigurationContextMenu`](VisualGridConfigurationContextMenu.cs) | class | Header context menu that saves the grid configuration or restores its defaults. |

### Detail controls

| Type | Kind | Purpose |
|---|---|---|
| [`VisualGridDataSourceDetail`](VisualGridDataSourceDetail.cs) | class | Hosts a child grid over the hierarchy-supplied source. |
| [`VisualGridControlDetail`](VisualGridControlDetail.cs) | class | Row detail that hosts an arbitrary control and disposes it and its supplied resources. |
| [`VisualGridDetailBase`](VisualGridDetailBase.cs) | abstract class | Base for one owned detail surface that applies prepared values on the UI thread and keeps child selection. |
| [`VisualGridDetailContext`](VisualGridDetailContext.cs) | class | Resolved node, payload and owned child-view context passed to the presentation factory. |

## Hierarchy and custom presentation

Implement relationships in [hierarchical data sources](../../Sphere10.Framework/DataSource/README.md),
then bind the source normally. Configure `CreateDetail` only to choose or configure
the UI; without it, the grid creates a nested grid. `DetailsEnabled` can suppress
expansion for a flat presentation. The root source remains borrowed, while the
grid releases returned child views and owned controls.

An expanded child table takes the height of all its rows and pushes the
following rows down, so only the outermost grid scrolls vertically.
`FitDetailHeightToRows` (on by default) selects this; `DetailHeight` sizes
custom panels, the loading surface and an Auto Paged child's page. The detail
host spans row-high physical rows ([VisualGridDetailLayout](VisualGridDetailLayout.cs)),
because SourceGrid scrolls by whole rows.

Expanded details never change an Auto page. Its size comes from the ordinary
rows and the viewport they have with every detail closed
(`CustomScrollControl.UnscrolledDisplayHeight`), so opening or closing a detail
does not move rows to another page; the grid scrolls to the rows a detail pushes
down instead. With every detail closed the page fits without a vertical
scrollbar. An Auto child likewise pages within `DetailHeight` regardless of its
own expanded details. See the
[shared platform guide](../../../docs/guidelines/patterns/grids-and-screen-hosts.md#hierarchical-sources-and-detail-controls)
for complete examples, migration from `RowDetails`, lazy graphs and the Blazor equivalent.
