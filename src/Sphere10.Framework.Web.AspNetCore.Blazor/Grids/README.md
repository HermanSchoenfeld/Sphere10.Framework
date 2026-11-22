# Grids

The controller, column model, settings and value conversions behind `BlazorGrid<TItem>`.

`BlazorGrid<TItem>`, the component in [UI](../UI/README.md), creates and disposes one `BlazorGridController<TItem>` and binds it to an `IDataSource<TItem>`. The controller serializes source reads and writes, keeps the query, page, selection and buffered row edit, and saves an edit only on an explicit `SaveAsync`. It does not own or dispose the data source. Call it on the owning renderer context, and keep source and column instances stable because replacing them discards a pending draft. Pages are zero-based. `IDataSource<T>` has no cancellation API. Synchronous controller disposal invalidates late results; asynchronous disposal additionally drains admitted reads and writes before returning. Neither disposes the borrowed source.

`BlazorGridColumn<T>.For(item => item.Property)` builds a typed column for a direct public property, and `AutoGenerate` reflects readable public properties. Custom editors receive a `BlazorGridCellEditContext<T>` and publish replacement values through `ValueChanged`. `BlazorGridSettings` is the `SettingsObject` that remembers page mode, page size, page number, sort and explicit column widths, either for a hosted screen or for a standalone grid. `Tools.BlazorGrid` converts scalar values to and from HTML editor values with the invariant culture. The data-source contracts are described in the [Sphere10.Framework](../../Sphere10.Framework/README.md) library.

## Hierarchical details

Hierarchical sources implement `IHierarchicalDataSource<TNode>` in core. The component automatically offers expansion, reads the payload through synchronous `GetEntity` and awaits the child view through `GetChildrenAsync`. `BlazorGridDetailContext<TNode>` supplies those values to `DetailTemplate`. Without a template, the component renders a nested grid. Nodes remain the row, selection and CRUD type. Each nested controller has its own query and selection. Selecting a child does not change the parent's selected item or CRUD target.

The component owns child views returned during expansion and releases them when details collapse, leave the page, become invalid or are disposed. The bound root remains borrowed. Replacing the bound root or changing search/sort clears details. A changed `GetChildQueryKey` reloads stale child views while keeping the row expanded. Compatible publications let an admitted load finish, then refresh the payload and children from the newest accepted node. This also applies when the source reuses node objects. Nested grids retain their controls, query, selection and compatible expansions when their child view changes. Retained rows use their accepted source until replacement succeeds. CRUD waits for the replacement page. A nested grid with a row draft keeps its view and draft until the edit ends, then adopts the replacement. Child acquisition accepts a cancellation token even though ordinary `IDataSource.ReadRangeAsync` does not. Cycles are blocked on the current ancestry path, so shared nodes remain usable on separate branches. See the [hierarchy example and limits](../README.md#hierarchical-sources-and-custom-row-details).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BlazorGridActionStyle`](BlazorGridActionStyle.cs) | enum | `Text` for a 120-pixel "Actions" column of text buttons or `Icons` for small icon buttons with tooltips in a narrow untitled column. |
| [`BlazorGridCellEditContext<T>`](BlazorGridCellEditContext.cs) | class | Item, column, buffered value, `ValueChanged` callback and new-item flag passed to a custom cell editor. |
| [`BlazorGridColumn<T>`](BlazorGridColumn.cs) | class | Typed column definition with header, property and sort names, getter and setter, templates, width, image presentation, format and preference key. |
| [`BlazorGridController<TItem>`](BlazorGridController.cs) | class | Default grid controller that serializes data-source I/O, coalesces refreshes and keeps edit values separate until an explicit save. |
| [`BlazorGridControllerBase<TItem>`](BlazorGridControllerBase.cs) | abstract class | Disposable base for grid controllers that declares the query, selection, refresh and edit members and the `Changed` event. |
| [`BlazorGridDensity`](BlazorGridDensity.cs) | enum | `Normal` or `Compact` row height and paddings; `ApplicationOptions.GridDensity` is the application default. |
| [`BlazorGridDetailContext<TNode>`](BlazorGridDetailContext.cs) | class | Node, resolved entity and borrowed child view supplied to a row-detail template. |
| [`BlazorGridFooterMode`](BlazorGridFooterMode.cs) | enum | `Always`, `Paged` (only while the results span more than one page) or `Hidden` for the footer with the item count and pager. |
| [`BlazorGridHorizontalAlignment`](BlazorGridHorizontalAlignment.cs) | enum | `Left`, `Center` or `Right`, used for the grid caption. |
| [`BlazorGridPageMode`](BlazorGridPageMode.cs) | enum | `Paged` for manually sized pages, `Auto` for pages fitted to the viewport and `Single` for one complete result page. |
| [`BlazorGridReloadPlacement`](BlazorGridReloadPlacement.cs) | enum | `Toolbar` for the Reload button, `Header` for a reload icon in the column header row or `Hidden`. |
| [`BlazorGridRowClick`](BlazorGridRowClick.cs) | enum | What a single row click does: `Select` (default) or `Open`, which with `ItemOpened` selects and opens the row unless the click lands on a control inside it. |
| [`BlazorGridRowCommands`](BlazorGridRowCommands.cs) | enum | Flags selecting the built-in `Open`, `Edit` and `Delete` row commands. |
| [`BlazorGridSettings`](BlazorGridSettings.cs) | class | User-editable `SettingsObject` with page mode, page size, page number, sort column and direction, and explicit column widths. |
| [`IBlazorGridController<TItem>`](IBlazorGridController.cs) | interface | Contract for one grid's query, paging, selection, row refresh and buffered create, edit, save and delete state. |
| [`Tools.BlazorGrid`](GridTool.cs) | tools | Reports supported editor types, chooses HTML input types and formats and parses grid scalar values with the invariant culture. |
