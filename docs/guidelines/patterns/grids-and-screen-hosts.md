# Grids and screen hosts on WinForms and Blazor

Both adapters use the Framework's data-source and application-screen contracts.
Use this page to map behavior between platforms, then use the linked project
guides for concrete configuration and examples.

## CRUD grids

| Behavior | WinForms | Blazor |
|---|---|---|
| Typed source | `VisualGrid.SetDataSource<T>` borrows an `IDataSource<T>`. | `BlazorGrid<TItem>.DataSource` borrows the same contract; `IBlazorGridController<TItem>` owns query, selection and draft state. |
| Permissions | Intersects source capabilities with the caller's mask. | `AllowedCapabilities` intersects the source capabilities. |
| Paged | `VisualGridPageMode.Paged`, remembered manual page size. | `BlazorGridPageMode.Paged`, remembered `ManualPageSize`. |
| Auto Paged | Complete ordered rows that fit the native viewport; recalculated for font, DPI and viewport changes. | Complete ordered browser rows that fit the viewport; observes browser layout, zoom and viewport changes. |
| Single Paged | Page zero, `int.MaxValue` length, vertical scrolling. | Same query and presentation policy. |
| Nonpageable source | Complete result; paging controls hidden. | Same behavior; a UI setting cannot grant `CanPage`. |
| Natural widths | Measured once for a binding set; retained through paging, filter, sort and refresh. | Same policy, measured from the first nonempty rendered page. |
| Preferred stretching | `VisualGridColumn<T>.ExpandsToFit`. | `BlazorGridColumn<T>.ExpandsToFit`. |
| Hidden columns | `VisualGridColumn<T>.Hidden` omits the native column; the `Hidden` column preference overrides it. `VisibleGridBindings` lists the displayed columns. | `BlazorGridColumn<T>.Hidden` omits the rendered column; the same preference overrides it. |
| Explicit resizing | Resized columns keep their chosen widths instead of stretching. | Same; other stretch columns continue sharing spare width. |
| Sorting | Ascending, Descending, None; retains the current page and widths. | Same three-state cycle and source query. |
| Header sizing | Reserves the native sort-indicator extent before sorting. | Reserves a sort-indicator slot before sorting. |
| Title | `GridTitle`, `GridTitleAlignment` (`ContentAlignment`). | `Caption`, `CaptionAlignment` (`BlazorGridHorizontalAlignment`); Left is the default, and an empty caption renders none. |
| Header and footer rows | Independent `HeaderVisible`, `ColumnHeadersVisible` and `FooterVisible` flags. | The toolbar row is omitted when empty; `ReloadPlacement` moves Reload to a header icon and `Footer` (`Always`, `Paged`, `Hidden`) controls the footer. |
| Row commands and density | Borrowed toolbar commands through `SetRowCommands`. | `ActionStyle` icons in an untitled narrow column, built-in `RowCommands` (Open, Edit, Delete) and `Density` per grid or per application. |
| Selection binding and opening | `SelectedEntity` and its events. | Two-way `SelectedItem` with `SelectedItemChanged`; `ItemOpened` for double-click and Enter. |
| Images | `VisualGridCellDisplayType.Image`; borrowed `Image`. | Column `ImageSource`/`ImageAlternativeText`; browser image URL. |
| Image layout | `ImageAlignment` and optional `ImageResizeMethod`. | Same `ResizeAlignment` and nullable `ResizeMethod` enums, applied through CSS fit/position. |
| Loading | First binding and ordinary reloads use the full-grid loading surface. | First binding and ordinary reloads use the full-viewport loading surface. |
| Live refresh | `SuppressLoadingIndicator`, keeping the committed page usable during collection. | Same option, or `RefreshAsync(showLoadingIndicator: false)` for one read. |
| Targeted refresh | `RefreshEntityAsync`, `RefreshSelectedEntityAsync`. | Same methods plus typed `RefreshRowsAsync`; `RemoveRows` applies caller-known removals. |
| Filtered export input | `ReadFilteredEntitiesAsync` returns a typed `Array`. | `ReadFilteredEntitiesAsync` returns `TItem[]`. Both read all matches across pages in current sort order. |
| Reference selection | `VisualGridDropDown`, shared grid configuration and reference bindings. | `BlazorGridReferencePicker`, typed reference bindings/editor templates. |
| Preferences | Accepted `VisualGridSettings` in the owning screen object; runtime operations stay transient until explicit configuration Save. | `BlazorGridSettings` in the owning screen object, or standalone in browser storage with an explicit `SettingsKey`. |
| Persistent manual widths | Stable column `PreferenceKey`, logical 96-DPI width, explicitly saved after a deliberate drag. | Stable column `PreferenceKey`, CSS-pixel width, applied before first markup/sizing. |

For screen-owned preferences, declare `[Settings(typeof(...))]` on the screen
with its nested `SettingsObject` schema, declare named grid roles on that root
and bind non-null `Preferences`. Native grids use that owner through their
capture/apply path and have no local settings editor or independent persistence.
A bound browser grid suppresses its standalone writes. Complete default column
schemas serve unopened Options pages; older saved overrides reconcile by key,
and screens without settings are omitted. The native screen's `Columns`
collection order controls its displayed column order while preserving overrides
by stable key. The native footer retains its page-mode selector, manual size and
page navigation as transient grid operations. An optional designer-owned
`VisualGridConfigurationContextMenu` on `HeaderContextMenuStrip` saves only the
clicked grid into the actual screen root or temporarily restores that grid's
screen-declared defaults. Screen close/finalization does not capture runtime
grid choices. Screen Options uses explicit Apply/OK for its edits.
Import legacy standalone records only while the typed screen record is absent;
Reset of an existing typed record must not reimport them after restart. See
[Screen Options](../../usage/screen-options.md) for startup, detached editing,
ordered accepted persistence and custom native/web editors.

Paging belongs at the source's query boundary. Filter and sort the complete
logical result before applying the requested page, and report its matching
total. A native inventory may first be collected as a complete snapshot; an
`IDataSource<T>` over that published snapshot can still provide genuine range
queries. This does not mean that the native operating-system API itself pages.
See [data sources](data-sources.md) for the paging/capability contract.

Auto measurements do not overwrite the remembered manual size. Switching modes
starts at page zero; Single Paged hides the navigator and manual size controls.
Sources must honor `SortDirection.None` as natural order, including when they
have a configured initial sort.

### Refresh and ownership

Source collection, expensive model evaluation and formatting must finish before
publishing new rows. An `async` signature alone does not move a synchronous
prefix off the UI thread. Use the source's native asynchronous operation or its
existing synchronous-source worker adapter. WinForms additionally offers
`PrepareReadOnlyCellsInBackground` for worker-safe read-only Text/Image bindings;
Blazor has no native cell graph to prepare and uses its renderer for DOM updates.

WinForms `RowStyleProvider` supplies optional `VisualGridRowStyle` foreground and
background colors without modifying shared SourceGrid views. `Color.Empty`
inherits the column's ordinary view. In worker-prepared mode the provider runs
once per row on the preparation worker: capture immutable rules beforehand and
never access controls or call native APIs from it. Ordinary cell binding runs the
provider on the owning UI thread, so its work must remain cheap. Changing or
clearing the provider invalidates stale prepared results and preserves selection,
image geometry and already allocated prepared cells. The WinForms Tester's Live
Snapshots tab demonstrates **Color High Load Rows** together with prepared cells
and suppressed loading feedback.

A suppressed refresh retains the last committed rows while awaiting the source.
Selection made during that wait belongs to the user and must be reconciled with
the accepted result using stable entity identity. It must not be replaced with
a selection captured before the read. Source, query, binding or disposal changes
invalidate stale completions. Initial loading still has an indicator; inline
editing and mutations retain their own availability/validation rules.

`ListDataSource<T>` and `AsyncListDataSource<T>` leaves
support targeted replacement and removal through `RefreshRangeAsync` and the
`RefreshMethod` hook; a null replacement means that the row disappeared.
`ArrayDataSource<T>` alone deliberately does not supply entity refresh.
Neither grid owns the source, cached source images or an application's polling
service. CSV/JSON formatting, downloads and snapshot capture policy belong to
the caller; filtered reads do not trigger a separate inventory collector.

Native pixel sizing, editor HWNDs, SourceGrid redraw/layout scopes and Windows
image decoding remain WinForms concerns. Browser observers, CSS sizing,
accessible markup, focus and local storage remain Blazor concerns. The pure
`ResizeAlignment` and `ResizeMethod` enums live in Core in the
`Sphere10.Framework` namespace; Drawing forwards their former assembly identity.
Actual bitmap resizing continues to use Drawing's existing API.

### Examples and verification

- [WinForms grid guide](../../../src/Sphere10.Framework.Windows.Forms/README.md#visualgrid)
  and [WinForms tester](../../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md).
- [Blazor grid guide](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#grid-configuration-and-behavior)
  and [Blazor tester](../../../utils/Sphere10.Framework.Utils.BlazorTester/README.md).
- [Native tests](../../../tests/Sphere10.Framework.Windows.Forms.Tests/README.md)
  cover source reads, input routing, selection and native sizing.
- [Blazor tests](../../../tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/README.md)
  cover controller/renderer contracts and opt-in real-browser layout, resize,
  persistence and interaction checks.

### Hierarchical sources and detail controls

Both grids detect `IHierarchicalDataSource<TNode>` on the bound source. Nodes
remain the rows used for columns, selection and CRUD; the optional `TEntity`
parameter exposes a separately resolved payload. The source supplies stable node
keys, child-query keys, availability, entity resolution and child sources. The
grid owns expansion and calls the client's presentation callback after resolving
the entity and child source. A missing callback uses an ordinary nested grid.

| Behavior | WinForms | Blazor |
|---|---|---|
| Client presentation | `CreateDetail(VisualGridDetailContext)` returns a fresh owned detail control. | `DetailTemplate` renders `BlazorGridDetailContext<TNode>`. |
| Resolved context | `Node`, `Entity`, `ChildDataSource` | Same values with a typed node and child source. |
| Enable or suppress details | `DetailsEnabled` | `DetailsEnabled` |
| Ordinary custom panels | `HasDetail`, `DetailIdentity` and `CreateDetail` | `HasDetail`, `DetailIdentity` and `DetailTemplate` |
| Expansion API | `SetRowExpandedAsync`, `IsRowExpanded` | `ExpandDetailAsync`, `CollapseDetailAsync`, `IsDetailExpanded` |
| Bounds | `DetailHeight`, `MaximumDetailDepth`, `MaximumExpandedDetails`, `MaximumRememberedDetails` | `DetailHeight`, `MaximumDetailDepth`, `MaximumExpandedDetails` |
| Selection | Selected descendants bubble through the native parent's `SelectedEntity` and events. | Each nested grid retains its own selection and CRUD target. Configure child selection callbacks in a custom template. |

`RowDetails`, `VisualGridRowDetails` and the prepared-array detail adapter have
been replaced. Move their `Identity`, `QueryIdentity`, `HasDetails` and
`PrepareAsync` relationship logic into a hierarchy source. Move `Create` to the
grid's presentation callback. A factory chooses the control and its columns; it
does not fetch children, sort model data or create another acquisition poller.
For ordinary image or editor panels there is no child query: the callback receives
the row as its entity and a null child source. On a hierarchical source only a
custom detail on a leaf has a null child source. A row that was admitted through
its children and has since lost them receives the source's empty child view.

A graph-backed source can use eager entities or `GraphNode<IFuture<TEntity>>`.
The graph source captures topology at construction, while future payloads resolve
during reads. Use cached or explicitly supplied futures for UI value getters;
`IFuture.Value` itself is synchronous and an always-loading future can still do
work on every access. Truly asynchronous or paged child membership belongs in a
custom hierarchy source. See the [data-source contract](data-sources.md) for
query and lifetime rules and the graph helper variants.

To show an existing flat inventory as a drill-down, wrap it in
[`KeyedHierarchyDataSource<T>`](../../../src/Sphere10.Framework/DataSource/KeyedHierarchyDataSource.cs)
with each row's key and parent key, and give the screen a switch bound to its
`Flatten` property. While `Flatten` is set the source reads the rows as a flat
list, so turn the grid's `DetailsEnabled` off with it. Its optional `createParent`
factory makes rows for parents that are not in the inventory, such as a folder
for scheduled tasks or a device for sensors.

For example, this native grid discovers the hierarchy from its source:

```csharp
var parent = new GraphNode<string>("Parent") {
	Connections = new[] { new GraphNode<string>("Child") }
};
var source = new ActionGraphDataSource<string>(new[] { parent }, node => node.Entity);
grid.GridBindings = new IVisualGridColumn[] {
	new VisualGridColumn<GraphNode<string>> {
		ColumnName = "Name", PreferenceKey = "Name", DataType = typeof(string),
		PropertyValue = node => node.Entity, ExpandsToFit = true
	}
};
grid.DetailHeight = 240;
grid.FitDetailHeightToRows = true;
grid.ShareDetailColumns = true;
await grid.SetDataSource(source);
await grid.RefreshGrid();
```

To customize the child control, set the callback before binding:

```csharp
grid.CreateDetail = context => {
	var child = new VisualGrid {
		GridBindings = grid.GridBindings,
		CreateDetail = grid.CreateDetail,
		ShareDetailColumns = grid.ShareDetailColumns,
		FitDetailHeightToRows = grid.FitDetailHeightToRows,
		DetailHeight = grid.DetailHeight,
		HeaderVisible = false, ColumnHeadersVisible = false, FooterVisible = false,
		PageMode = VisualGridPageMode.Single
	};
	return new VisualGridDataSourceDetail(child);
};
```

`VisualGridDataSourceDetail` binds the supplied child source through the grid's
ordinary query pipeline. Its optional `beforePublish` callback configures cheap
UI state from `VisualGridDetailContext`, including capture provenance retained by
the child source. It does not convert worker-prepared arrays into a second store.
`VisualGridControlDetail` hosts an arbitrary designer-authored control instead.
Native factories and publication run on the UI thread. Preparation resolves
the payload synchronously through `GetEntity` on a worker and awaits
`GetChildrenAsync` for the child query. Identity and availability callbacks must
also be safe on that worker.

The bound root source is borrowed. Each returned child view is owned by the
expansion and released on collapse, eviction, replacement or grid disposal,
including a late completion that ignored cancellation. A view is released only
after the nested grid's page read and child queries against it have drained. An
update that fails or is cancelled leaves the detail on the view it applied last
and releases only the rejected one. Child views borrow their
underlying acquisition owner. Stable node keys retain expansion through refresh;
a changed child-query key invalidates a pending result. Keys must be non-null
and unique within a sibling page. Shared nodes may appear on separate ancestry
paths; cycles are stopped on the current path. Repeated edges within one sibling
page need distinct occurrence nodes and keys. A globally de-duplicated graph
traversal is not the visual expansion algorithm.

The native root shares a bounded expansion budget and value-only remembered
state. Await `DetailsDisposalCompletion` when the owner must join pending native
cleanup. Surviving controls retain handles and layout during accepted refreshes.
`ShareDetailColumns` follows the parent's `ColumnLayoutSource`, including resolved
widths and horizontal scrolling. `DetailIndentColumnKey` chooses the gutter's
stable column key.

`HeaderVisible`, `ColumnHeadersVisible` and `FooterVisible` are independent native
flags. Headerless, footerless child tables should explicitly use
`VisualGridPageMode.Single` so every child remains reachable. `DetailHeight` is
in 96-DPI logical pixels on WinForms and CSS pixels on Blazor. Native
`FitDetailHeightToRows`, on by default, sizes an expanded child table to all of
its rows: the detail pushes the following rows down, expanding or collapsing a
nested row resizes every ancestor detail, and only the outermost grid scrolls
vertically. The mouse wheel over a fitted child scrolls that outer grid. A tall
detail spans row-high physical rows, so the outer grid scrolls through it one
ordinary row at a time. `DetailHeight` then sizes custom panels, the loading
surface, and the page that an Auto Paged child fits; a fitted child stops
growing at 32,000 device pixels, the safe Win32 window height, and scrolls its
own rows beyond that. Set `FitDetailHeightToRows = false` for a fixed-height
child table with its own vertical scrollbar. Expansion rows do not increase the
source's item count or cause Auto Paged to page away the parent. Both platforms
measure page capacity from source rows only.

A detail grid inherits `PrepareReadOnlyCellsInBackground` from its parent when
it displays only the parent's own column bindings and no other row style
provider, because those delegates are already worker-safe.

A Blazor nested grid keeps its row draft when its parent publishes. It adopts the
replacement child view once the edit is saved or cancelled, and the view under
edit stays alive until then. After a publication the parent presents its accepted
rows first; expanded details then reload together rather than one after another.

`SetRowCommands` gives native children the existing borrowed toolbar commands.
Applications never maintain SourceGrid's physical-row mapping themselves. See
[SourceGrid composition](../../usage/sourcegrid.md) for the native host mechanism.
The WinForms Tester **Visual Grid > Expandable Panels And Hierarchy** tab and
Blazor Tester's Grid demo show source-owned hierarchies and ordinary custom
panels through their platform callbacks.

## Application screen hosts

Use the [application and screen lifetime practice](application-ownership.md#application-and-screen-lifetimes)
when a screen owns refresh work or observes a shared service. Lifecycle overrides
own the work; guards only decide whether a transition is allowed.

The shared `IApplicationScreenHost` and single/multi-screen contracts own logical
selection and transitions. Adapter objects preserve the identity of the running
application, canonical blocks, menu definitions and exact screen instances.
Screens are registered types, not arbitrary types loaded from saved text.

| Behavior | WinForms | Blazor |
|---|---|---|
| Composition | Application/block/plugin builders, screen-owned menus and toolbars. | Equivalent builders and scoped application/block/session composition. |
| Screen modes | SingleView by default; MultiView shows tabs. | MultiView by default; SingleView retains its intended hidden singleton behavior. |
| Single instance | Reuses one type per host, including a detached instance. | Reuses one component type per host. |
| Multiple instances | Distinct native screen instances. | Distinct sessions/components, optionally keyed by caller-supplied instance identity. |
| Permanent singleton | Created at initialization; cannot close/undock; retained across mode changes. | Created at initialization; cannot close; retained across mode changes. |
| Empty workspace | A tabless `ScreenKind.Empty` fallback; cannot also be permanent. | Same role and lifetime policy. |
| Selection | Exact instances, active chrome follows selected docked screen. | Exact session IDs; active chrome follows the selected session. |
| Tabs | Selection, close, middle-click, reorder and context commands. | Same applicable interactions, including keyboard selection/reorder and context commands. |
| Long titles | DPI-scaled width cap, ellipsis and full tooltip. | CSS width cap, ellipsis and full tooltip. |
| Batch closure | Close, Close All, Close All Others; preflight all guards. | Equivalent context/host commands; preflight all guards. |
| Screen changes | `Title`, screen lifecycle events and merged original menu/toolbar instances. | Override `Title` and expose screen commands; each render publishes a changed title, dirty state or command state, and `NotifyScreenChanged` covers changes made without a render. |
| File menu | The main form's designer File menu, extended by screen strips merged by text. | The shell's standard File menu (`Application.FileMenuId`), shown first once any layer contributes to it and extended by menus with that ID. |
| Runtime block changes | Register/unregister with guards and permanent-screen initialization. | Same logical transition policy without constructing a second block owner. |
| Block availability | `AvailableWhen` is decided once per application with its provider, and registration skips a block that declined. | `AvailableWhen` is decided once per circuit from its scoped services, and a block that declined has no copy, route or screens there. |
| User startup choice | Make Startup Screen through app `UserSettings`. | Make Startup Screen through an explicit browser/application preference key. |
| Startup fallback | Registered default order; permanent screens still created. | Registered default order; permanent screens still created; explicit bookmark wins over saved preference. |
| Exit checks | Includes hidden retained and detached screens. | Includes all retained sessions; browser navigation lock also uses dirty state. |
| Ownership | Host owns native screens/forms; borrowed application references stay borrowed. | Renderer creates/disposes components; host owns logical sessions, never constructs/disposes components itself. |

### Transitions, startup and close guards

Use the host's awaited transition methods for navigation, screen mode changes
and batch closure. A guard veto leaves the existing selection and batch intact.
Permanent lifetime prevents ordinary close but does not veto shutdown or guarded
administrative block removal. Hidden retained singletons also participate in
unsaved-state/exit checks. `Screens` includes retained hidden sessions;
`OpenScreens` supplies the normal tab membership in presentation order.

WinForms screens use `OnHide(ref bool cancelHide)`, `ScreenHidden`,
`ScreenDisplayedFirstTime`, `ScreenDisplayed` and `ScreenDestroyed`. Blazor screens
use `CanDeactivateAsync`, `OnActivatedAsync`, `OnDeactivatedAsync`,
`HasUnsavedChanges`, `NotifyScreenChanged` and `DisposeAsyncCore`. Blazor lifecycle
callbacks must not await a nested transition on their own serialized host.
An activation error after commit can leave the selected session active; handle
the reported error rather than assuming that every failure implies rollback.

User startup preferences identify a registered definition independently of its
caption, running instance, assembly version or transient process/window handle.
Unavailable or invalid preferences fall back without rewriting the saved choice.
Screens that require transient arguments must reject startup restoration. This
is one preferred startup screen, not an automatic replay of every previously
open tab or its unsaved state.

WinForms startup settings belong to the application user's settings provider.
Blazor preferences use the same `UserSettings` facade with its circuit-bound
browser provider and an application-qualified key. Do not install one browser
user's provider as the process default. Browser startup restoration requires an
interactive render; a prerender cannot access browser storage.

### Platform presentation

WinForms supports native undock/re-dock, detached windows with original screen
menus/toolbars, compact window chrome, drag docking and monitor/DPI geometry.
Those operations are not implemented by the Blazor screen host: a browser tab
or pop-up has a different document/circuit and cannot take ownership of an
existing renderer component. Shared `ShowScreenUndockedAsync` remains unsupported
there. Blazor instead supports route/bookmark navigation and retained components
inside its current shell. Native WinForms HWND, caption and repaint mechanics
are not copied into browser code.

Both hosts offer their own navigation layout and command availability. WinForms
uses its collapsible/sizable ExplorerBar pane; Blazor uses responsive shell
sidebar/drawer controls. Command visibility is presentation, not authorization;
permission enforcement remains with the application/service. Block availability
is presentation too. It removes a block's navigation for one host and leaves
every permission check with the services behind its screens.

See the [WinForms host guide](../../../src/Sphere10.Framework.Windows.Forms/README.md#multiple-screens-and-detachable-tabs),
[Blazor host guide](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#default-permanent-and-empty-screens)
and [application ownership](application-ownership.md) for registration, lifecycle,
startup selection and platform-specific examples.
