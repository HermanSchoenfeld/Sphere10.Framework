# Grids

The controller, column model, settings and value conversions behind the `BlazorGrid<TItem>` CRUD grid.

`BlazorGrid<TItem>`, the component in [UI](../UI/README.md), creates and disposes one `BlazorGridController<TItem>` and binds it to an `IDataSource<TItem>`. The controller serializes source reads and writes, keeps the query, page, selection and buffered row edit, and saves an edit only on an explicit `SaveAsync`. It does not own or dispose the data source. Call it on the owning renderer context, and keep source and column instances stable because replacing them discards a pending draft. Pages are zero-based. `IDataSource<T>` has no cancellation API, so disposal only ignores the completion of a read that is already running.

`BlazorGridColumn<T>.For(item => item.Property)` builds a typed column for a direct public property, and `AutoGenerate` reflects readable public properties. Custom editors receive a `BlazorGridCellEditContext<T>` and publish replacement values through `ValueChanged`. `BlazorGridSettings` is the `SettingsObject` that remembers page mode, page size, page number, sort and explicit column widths, either for a hosted screen or for a standalone grid. `Tools.BlazorGrid` converts scalar values to and from HTML editor values with the invariant culture. The data-source contracts are described in the [Sphere10.Framework](../../Sphere10.Framework/README.md) library.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BlazorGridCellEditContext<T>`](BlazorGridCellEditContext.cs) | class | Item, column, buffered value, `ValueChanged` callback and new-item flag passed to a custom cell editor. |
| [`BlazorGridColumn<T>`](BlazorGridColumn.cs) | class | Typed column definition with header, property and sort names, getter and setter, templates, width, image presentation, format and preference key. |
| [`BlazorGridController<TItem>`](BlazorGridController.cs) | class | Default grid controller that serializes data-source I/O, coalesces refreshes and keeps edit values separate until an explicit save. |
| [`BlazorGridControllerBase<TItem>`](BlazorGridControllerBase.cs) | abstract class | Disposable base for grid controllers that declares the query, selection, refresh and edit members and the `Changed` event. |
| [`BlazorGridControllerDecorator<TItem>`](BlazorGridControllerDecorator.cs) | abstract class | Decorator base over an inner `IBlazorGridController<TItem>`. |
| [`BlazorGridControllerDecorator<TItem, TConcrete>`](BlazorGridControllerDecorator.cs) | abstract class | Forwards every controller member and event to an inner controller of type `TConcrete`. |
| [`BlazorGridHorizontalAlignment`](BlazorGridHorizontalAlignment.cs) | enum | `Left`, `Center` or `Right`, used for the grid caption. |
| [`BlazorGridPageMode`](BlazorGridPageMode.cs) | enum | `Paged` for manually sized pages, `Auto` for pages fitted to the viewport and `Single` for one complete result page. |
| [`BlazorGridSettings`](BlazorGridSettings.cs) | class | User-editable `SettingsObject` with page mode, page size, page number, sort column and direction, and explicit column widths. |
| [`IBlazorGridController<TItem>`](IBlazorGridController.cs) | interface | Contract for one grid's query, paging, selection, row refresh and buffered create, edit, save and delete state. |
| [`Tools.BlazorGrid`](GridTool.cs) | tools | Reports supported editor types, chooses HTML input types and formats and parses grid scalar values with the invariant culture. |
