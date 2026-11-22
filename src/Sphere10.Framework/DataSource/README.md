# Data sources

The `IDataSource<TItem>` CRUD, paging and change-notification contract, its synchronous and asynchronous base classes, list, array and graph storage, and the hierarchy, refreshable and polling decorators.

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

`IHierarchicalDataSource<TNode, TEntity>` adds child views and typed payloads while retaining `TNode` for rows, selection and CRUD. `HierarchicalDataSourceBase` adds this capability over a borrowed source. The graph helpers capture connections at construction and serve read-only sibling pages. Supply stable keys that identify the nodes independently of their captions or object instances.

```csharp
var root = new GraphNode<string>("root");
var child = new GraphNode<string>("child");
root.Connections = new[] { child };
var tree = new ActionGraphDataSource<string>(new[] { root }, node => node.Entity);
var children = await tree.GetChildrenAsync(root);
var childPage = await children.ReadRangeAsync(null, 50, 0, null, SortDirection.None);
```

`KeyedHierarchyDataSource<T>` shows an existing flat source as a tree by each row's key and parent key. A row whose parent key is null or names no row is a root, and a loop of parents is broken at its first row. A tree read pages the roots only, and a search keeps each matching row together with the rows above it. `Presentation` chooses how reads serve the rows. `HierarchyPresentation.Tree`, the default, pages the roots with child views to drill into. `Outline` pages every shown row in tree order, and the source's `IHierarchyOutline` gives each row's depth and keeps its expansion by node key. Every row starts expanded, `ExpandAll` and `CollapseAll` reset all of them, and a search shows its matches expanded. A grid can then show a tree of any size in one table. `Flat` reads the source's rows as they are, with no children. `GetDepth` and `GetSubtree` answer from the last tree or outline read, whether or not an outline shows the rows. The optional `createParent` factory makes a row for a parent key that names no row, such as a folder for the tasks in it or a device for its sensors. The made row's key must equal the parent key it was made for, its own parent key is followed in turn, and `IsCreatedParent` tells it apart from a source row. Made rows appear only in the tree. Each tree read captures the result once, so child-query keys change from one read to the next.

```csharp
// Tasks name their folder path; the folders are not rows, so each is made from its path.
static string Folder(string path) => path.LastIndexOf('\\') is var slash and > 0 ? path[..slash] : null;
var tasks = new KeyedHierarchyDataSource<TaskRow>(new ArrayDataSource<TaskRow>(LoadTasks()),
	row => row.Path, row => Folder(row.Path), path => new TaskRow((string)path));
var roots = await tasks.ReadRangeAsync(null, 50, 0, "Path", SortDirection.Ascending);
var firstFolder = await tasks.GetChildrenAsync(roots.Items.First());
tasks.Presentation = HierarchyPresentation.Outline; // every folder and task in one list, indented by GetDepth
tasks.CollapseAll(); // only the top-level folders from the next read
tasks.Presentation = HierarchyPresentation.Flat; // the same rows as a flat list, without the made folders
```

Each child view belongs to its caller and must be released when it implements `IAsyncDisposable` or `IDisposable`. The graph views above hold managed graph data and require no disposal. `ActionFutureGraphDataSource<TEntity>` accepts `GraphNode<IFuture<TEntity>>` and resolves only the returned page's payloads for natural reads. Search or sort can require other siblings' payloads. See the [hierarchy guide](../../../docs/guidelines/patterns/data-sources.md#hierarchical-data-sources) for query identity, lazy payloads and composition, and the [VisualGrid](../../Sphere10.Framework.Windows.Forms/VisualGrid/README.md) and [Blazor grids](../../Sphere10.Framework.Web.AspNetCore.Blazor/Grids/README.md) documentation for presentation callbacks.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ActionAsyncListDataSource<TItem>`](ActionAsyncListDataSource.cs) | class | `AsyncListDataSource<TItem>` whose list a supplied asynchronous callback fetches on load. |
| [`ActionFutureGraphDataSource<TEntity>`](ActionFutureGraphDataSource.cs) | class | Read-only graph source with lazy `IFuture` payloads and an explicit stable node-key selector. |
| [`ActionGraphDataSource<TEntity>`](ActionGraphDataSource.cs) | class | Read-only graph source with eager payloads and an explicit stable node-key selector. |
| [`ActionListDataSource<TItem>`](ActionListDataSource.cs) | class | `ListDataSource<TItem>` whose list a supplied synchronous callback fetches on load. |
| [`ActionRefreshableDataSource<TItem>`](ActionRefreshableDataSource.cs) | class | Interface-typed form of `ActionRefreshableDataSource<TItem, TConcrete>` over `IDataSource<TItem>`. |
| [`ActionRefreshableDataSource<TItem, TConcrete>`](ActionRefreshableDataSource.cs) | class | Refreshable data source whose fresh sources a supplied factory creates. |
| [`ArrayDataSource<TItem>`](ArrayDataSource.cs) | class | Read-only source over an array that its owner replaces wholesale with `ReplaceData`, searching, sorting and paging that array. |
| [`AsyncBatchDataSourceBase<TItem>`](AsyncBatchDataSourceBase.cs) | abstract class | Asynchronous base whose item operations delegate to batch operations. |
| [`AsyncDataSourceBase<TItem>`](AsyncDataSourceBase.cs) | abstract class | Base for asynchronous backends, whose synchronous members wait on the asynchronous ones. |
| [`AsyncItemDataSourceBase<TItem>`](AsyncItemDataSourceBase.cs) | abstract class | Asynchronous base whose batch operations loop over item operations. |
| [`AsyncListDataSource<TItem>`](AsyncListDataSource.cs) | class | List storage fetched asynchronously, with search, sort, paging and change notification. |
| [`CrudAction`](CrudAction.cs) | enum | The operation being validated or recorded: `Create`, `Read`, `Update` or `Delete`. |
| [`CrudActionItem<TItem>`](CrudActionItem.cs) | class | Pairs an item with the `CrudAction` applied to it. |
| [`DataSourceBase<TItem>`](DataSourceBase.cs) | abstract class | Root base that declares every item, batch, synchronous and asynchronous member and raises `Changed`. |
| [`DataSourceCapabilities`](DataSourceCapabilities.cs) | enum | Flags for create, read, update, delete, search, sort, page and notify support, with `Default` and `ReadOnly` sets. |
| [`DataSourceDecorator<TItem>`](DataSourceDecorator.cs) | abstract class | Interface-typed decorator that forwards every member to an inner `IDataSource<TItem>`. |
| [`DataSourceDecorator<TItem, TConcrete>`](DataSourceDecorator.cs) | abstract class | Decorator that forwards every member and the `Changed` event to an inner source of type `TConcrete`. |
| [`DataSourceFormatting`](DataSourceFormatting.cs) | static class | Formats a property value as the text that searching and sorting use. |
| [`DataSourceItems<TItem>`](DataSourceItems.cs) | struct | One page of results with its page number and the total item count. |
| [`DataSourceLifetime`](DataSourceLifetime.cs) | class | Drains owned data sources before their application dependencies are finalized. |
| [`DataSourceMutatedItems<TItem>`](DataSourceMutatedItems.cs) | class | Batch of changed items with the current page and total item count. |
| [`GraphDataSourceBase<TNode, TEntity>`](GraphDataSourceBase.cs) | abstract class | Captures graph connections and serves read-only sibling pages with typed payload resolution. |
| [`HierarchicalDataSourceAdapter<TNode>`](HierarchicalDataSourceAdapter.cs) | class | Boxes a hierarchical source's nodes without losing child queries or typed source metadata. |
| [`HierarchicalDataSourceBase<TNode, TEntity>`](HierarchicalDataSourceBase.cs) | abstract class | Adds source-owned relationships, keys and payload resolution over a borrowed data source. |
| [`HierarchyPresentation`](HierarchyPresentation.cs) | enum | How a source that arranges its rows as a tree serves them: `Tree`, `Outline` or `Flat`. |
| [`IDataSource<TItem>`](IDataSource.cs) | interface | CRUD, paged read, validation, count, capability and change contract in synchronous and asynchronous forms. |
| [`IHierarchicalDataSource<TNode>`](IHierarchicalDataSource.cs) | interface | Sibling-page source with node keys, child-query keys, payload resolution and child views. |
| [`IHierarchicalDataSource<TNode, TEntity>`](IHierarchicalDataSource.cs) | interface | Hierarchical source with typed payloads and an object bridge for presentation controls. |
| [`IHierarchyOutline`](IHierarchyOutline.cs) | interface | A source's outline: each row's depth, whether it has rows beneath it and whether they are shown, with `ExpandAll` and `CollapseAll`. |
| [`ILoadableDataSource<TItem>`](ILoadableDataSource.cs) | interface | A data source which fetches its data on demand and loads before its first read. |
| [`IRefreshableDataSource<TItem>`](IRefreshableDataSource.cs) | interface | A data source whose complete data can be replaced by a fresh acquisition while the current data stays readable. |
| [`IRefreshingDataSource`](IRefreshingDataSource.cs) | interface | Automatic refresh cadence and lifecycle of a data source, independent of its paging and CRUD contract. |
| [`KeyedHierarchyDataSource<T>`](KeyedHierarchyDataSource.cs) | class | Presents a flat source as a tree, an outline or a flat list by each row's key and parent key, with optional made parents. |
| [`ListDataSource<TItem>`](ListDataSource.cs) | class | List storage fetched synchronously, with search, sort, paging and change notification. |
| [`PollingDataSource<TItem>`](PollingDataSource.cs) | class | Interface-typed form of `PollingDataSource<TItem, TConcrete>` over `IRefreshableDataSource<TItem>`. |
| [`PollingDataSource<TItem, TConcrete>`](PollingDataSource.cs) | class | Decorates a refreshable data source with scheduled automatic refresh and owns its lifecycle. |
| [`ProjectedDataSource<TFrom, TTo>`](ProjectedDataSource.cs) | class | Presents a data source of one item type as another through a projection and, for writes, its inverse. |
| [`RefreshableDataSourceBase<TItem, TConcrete>`](RefreshableDataSourceBase.cs) | abstract class | Refreshes by creating and loading a fresh source, swapping it in and disposing the old one. |
| [`SyncBatchDataSourceBase<TItem>`](SyncBatchDataSourceBase.cs) | abstract class | Synchronous base whose item operations delegate to batch operations. |
| [`SyncDataSourceBase<TItem>`](SyncDataSourceBase.cs) | abstract class | Base for synchronous backends, whose asynchronous members run the synchronous ones with `Task.Run`. |
| [`SyncItemDataSourceBase<TItem>`](SyncItemDataSourceBase.cs) | abstract class | Synchronous base whose batch operations loop over item operations. |
| [`Tools.DataSources`](DataSourceTool.cs) | tools | One-shot reads, hierarchy-preserving object boxing and shared node or payload search, sort and paging. |
