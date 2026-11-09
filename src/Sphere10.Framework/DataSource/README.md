# Data sources

The `IDataSource<TItem>` CRUD, paging and change-notification contract, its synchronous and asynchronous base classes, list and array storage, and the refreshable and polling decorators.

An `IDataSource<TItem>` serves pages of items through `ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection)`, accepts item and batch create, refresh, update, delete and validate calls, reports its `DataSourceCapabilities` and raises `Changed` after committed mutations. Every member has a synchronous and an asynchronous form. An implementation derives from the base that matches its backend. `SyncDataSourceBase<TItem>` runs the asynchronous members on a worker, and `AsyncDataSourceBase<TItem>` blocks its synchronous members on the asynchronous ones, so those synchronous calls must not run on a UI thread. The `Item` and `Batch` variants of each base supply one operation shape from the other.

Storage comes from `ListDataSource<TItem>`, `AsyncListDataSource<TItem>` and `ArrayDataSource<TItem>`. The list sources implement `ILoadableDataSource<TItem>` (see [Loadable](../Loadable/README.md)) and fetch on first read when constructed without rows. `RefreshableDataSourceBase<TItem, TConcrete>` decorates a source and refreshes it by building a fresh source, loading it, swapping it in and disposing the old one, while the current data stays readable. `PollingDataSource<TItem, TConcrete>` adds a repeating [Scheduler](../Scheduler/README.md) job, which runs only while both `Enabled` and `AutoRefresh` are true. `StopAsync()` ends polling permanently and disposes the decorated source. `DataSourceLifetime` drains registered `IRefreshingDataSource` instances before an application finalizes their dependencies. The `Action*` types compose a source from callbacks without a subclass. Filter and sort trees for in-memory queries are in [Filter](../Filter/README.md).

```csharp
var orders = new ActionRefreshableDataSource<Order>(() => new ListDataSource<Order>(LoadOrders()));
await using var polling = new PollingDataSource<Order>(orders) {
	RefreshInterval = TimeSpan.FromSeconds(30),
	AutoRefresh = true,
	Enabled = true,
};
var page = await polling.ReadRangeAsync(null, 50, 0, "Date", SortDirection.Descending);
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AsyncBatchDataSourceBase<TItem>`](AsyncBatchDataSourceBase.cs) | abstract class | Asynchronous base whose item operations delegate to batch operations. |
| [`AsyncItemDataSourceBase<TItem>`](AsyncItemDataSourceBase.cs) | abstract class | Asynchronous base whose batch operations loop over item operations. |
| [`CrudAction`](CrudAction.cs) | enum | The operation being validated or recorded: `Create`, `Read`, `Update` or `Delete`. |
| [`CrudActionItem<TItem>`](CrudActionItem.cs) | class | Pairs an item with the `CrudAction` applied to it. |
| [`DataSourceBase<TItem>`](DataSourceBase.cs) | abstract class | Root base that declares every item, batch, synchronous and asynchronous member and raises `Changed`. |
| [`DataSourceCapabilities`](DataSourceCapabilities.cs) | enum | Flags for create, read, update, delete, search, sort, page and notify support, with `Default` and `ReadOnly` sets. |
| [`DataSourceDecorator<TItem>`](DataSourceDecorator.cs) | abstract class | Interface-typed decorator that forwards every member to an inner `IDataSource<TItem>`. |
| [`DataSourceDecorator<TItem, TConcrete>`](DataSourceDecorator.cs) | abstract class | Decorator that forwards every member and the `Changed` event to an inner source of type `TConcrete`. |
| [`DataSourceItems<TItem>`](DataSourceItems.cs) | struct | One page of results with its page number and the total item count. |
| [`DataSourceMutatedItems<TItem>`](DataSourceMutatedItems.cs) | class | Batch of changed items with the current page and total item count. |
| [`IDataSource<TItem>`](IDataSource.cs) | interface | CRUD, paged read, validation, count, capability and change contract in synchronous and asynchronous forms. |
| [`ListDataSource<TItem>`](ListDataSource.cs) | class | List storage fetched synchronously, with search, sort, paging and change notification. |
| [`ProjectedDataSource<TFrom, TTo>`](ProjectedDataSource.cs) | class | Presents a data source of one item type as another through a projection and, for writes, its inverse. |
| [`SyncBatchDataSourceBase<TItem>`](SyncBatchDataSourceBase.cs) | abstract class | Synchronous base whose item operations delegate to batch operations. |
| [`SyncItemDataSourceBase<TItem>`](SyncItemDataSourceBase.cs) | abstract class | Synchronous base whose batch operations loop over item operations. |
