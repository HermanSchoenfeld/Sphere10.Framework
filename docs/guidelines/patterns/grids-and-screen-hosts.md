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
| Explicit resizing | Resized columns keep their chosen widths instead of stretching. | Same; other stretch columns continue sharing spare width. |
| Sorting | Ascending, Descending, None; retains the current page and widths. | Same three-state cycle and source query. |
| Header sizing | Reserves the native sort-indicator extent before sorting. | Reserves a sort-indicator slot before sorting. |
| Title | `GridTitle`, `GridTitleAlignment` (`ContentAlignment`). | `Caption`, `CaptionAlignment` (`BlazorGridHorizontalAlignment`); Left is the default. |
| Images | `CrudCellDisplayType.Image`; borrowed `Image`. | Column `ImageSource`/`ImageAlternativeText`; browser image URL. |
| Image layout | `ImageAlignment` and optional `ImageResizeMethod`. | Same `ResizeAlignment` and nullable `ResizeMethod` enums, applied through CSS fit/position. |
| Loading | First binding and ordinary reloads use the full-grid loading surface. | First binding and ordinary reloads use the full-viewport loading surface. |
| Live refresh | `SuppressLoadingIndicator`, keeping the committed page usable during collection. | Same option, or `RefreshAsync(showLoadingIndicator: false)` for one read. |
| Targeted refresh | `RefreshEntityAsync`, `RefreshSelectedEntityAsync`. | Same methods plus typed `RefreshRowsAsync`; `RemoveRows` applies caller-known removals. |
| Filtered export input | `ReadFilteredEntitiesAsync` returns a typed `Array`. | `ReadFilteredEntitiesAsync` returns `TItem[]`. Both read all matches across pages in current sort order. |
| Reference selection | `CrudComboBox`, shared grid configuration and reference bindings. | `BlazorGridReferencePicker`, typed reference bindings/editor templates. |
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

### General native row-detail panels and recursive tables

WinForms `VisualGrid.RowDetails` opts into an owned control **below** an entity
row. A detail may be an ordinary designer-authored `UserControl` with images,
labels and editors, or a nested `VisualGrid`. `VisualGridControlDetail` adapts
the former; `VisualGridArrayDetail<T>` publishes prepared rows directly to a
passive source and uses the ordinary `VisualGridDataSourceController<T>` query
path for the latter. A screen must not access
SourceGrid directly or construct another polling service just to show children.

`Identity`, `QueryIdentity`, `HasDetails` and `PrepareAsync` read detached model
values on a worker. `Create` and detail publication run on the owning UI
dispatcher. The factory returns a fresh unparented control whose ownership it
transfers to the detail; a disposable prepared model is likewise owned and
released when replaced, rejected, collapsed or evicted. Do not return controls
from worker preparation. Keep source collection and model sorting out of the
native factory. `QueryIdentity` identifies relation/filter parameters rather
than a constantly changing counter timestamp, so a slow detail read is not
starved by repeated live samples.

The same descriptor can be assigned recursively. Each root shares a bounded
expanded-control budget and a bounded value-only preference/selection cache.
Ancestor identity checks stop cycles; `MaximumDepth` stops deeper expansion.
Collapsed or paged/filtered-away children cancel work and dispose native
descendants. Await `DetailsDisposalCompletion` during the owning screen's
finalization when pending preparation must drain. Surviving keys keep their
native controls, expansion, selection and widths during refresh and reorder;
the selected descendant is exposed by the parent's `SelectedEntity` and normal
selection events. Use verified process identity or another stable schema key,
never a physical row number or a recycled PID alone.

For example, prepare a typed child array from an already published model and
create the control only in the UI factory:

```csharp
VisualGridRowDetails details = null;
details = new VisualGridRowDetails {
	Identity = row => ((Node)row).Key,
	HasDetails = row => ((Node)row).Children.Length > 0,
	PrepareAsync = (row, token) => Task.FromResult<object>(((Node)row).Children),
	Create = (_, _) => {
		var child = new VisualGrid {
			GridBindings = columns, RowDetails = details,
			HeaderVisible = false, ColumnHeadersVisible = false, FooterVisible = false,
			PageMode = VisualGridPageMode.Single,
			PrepareReadOnlyCellsInBackground = true, SuppressLoadingIndicator = true
		};
		return new VisualGridArrayDetail<Node>(child, row => row.Key);
	},
	Height = 240, FitHeightToChildRows = true, MaximumDepth = 8,
	MaximumExpandedDetails = 16, MaximumRememberedDetails = 64
};
grid.RowDetails = details;
```

Give child column schemas stable `PreferenceKey` values to retain widths. For
arbitrary panels, return `VisualGridControlDetail` with the designer-authored
control and a cheap UI publication callback instead. `VisualGridArrayDetail`
also accepts an optional `beforePublish` callback for cheap UI configuration
over the prepared array; it must not perform collection or model sorting.
If preparation returns a DTO containing rows and capture metadata, use its
optional `readPreparedRows` adapter. That adapter runs on the UI dispatcher and
returns the typed array; it can also publish cheap metadata such as a partial
capture warning. Keep that metadata with the prepared result rather than
looking it up in an independently evictable cache during publication.

For a hierarchy whose child rows have the same schema as their parent, set
`ShareParentColumns = true` on the descriptor and omit a separate child column
configuration. The adapter assigns the child's `ColumnLayoutSource` to its
parent. Definitions, displayed order, visibility and resolved widths follow
that source, including manual widths and stretch resizing. Child query,
sorting, paging and selection remain independently controlled by the caller.
Column preferences belong to the parent; do not create another child settings
section for the same layout. Source subscriptions detach when either control
is disposed, and source cycles are rejected.

Shared recursive tables start at the same native column origin as the parent.
Their hierarchy gutter and indentation are drawn inside a column, so values
stay aligned with the parent headers even when a user fixes the first column's
width. Set `IndentColumnKey` to that column's stable `PreferenceKey` when a
narrow leading icon column should remain untouched. If the selected column is
hidden, the gutter uses the first displayed column. Ordinary image/editor
panels retain their separate indented host. Other ordinary controls can opt
into the same column mechanism directly with `ColumnLayoutSource`.

Three flags are independent: `HeaderVisible` controls title/search/CRUD chrome,
`ColumnHeadersVisible` controls the native column-heading row, and
`FooterVisible` controls totals and page controls. A compact child table can
hide all three. Explicitly use `VisualGridPageMode.Single` for headerless,
footerless children so every child remains reachable by vertical scrolling;
hiding a footer does not implicitly change source paging. The fixed detail
height scales with DPI and the child owns its scrolling. `FitHeightToChildRows`
optionally shrinks table details to already measured committed rows, up to that
height cap. Descendant expansion/collapse propagates native geometry without
another source query; arbitrary panels retain their explicit height. Parent Auto Paged
estimates count entity rows, excluding detail height, so expanding a row cannot
immediately page that same entity away.

`SetRowCommands` mirrors borrowed toolbar commands into the row context menu,
including enabled/checked state and nested commands. Native hit testing and
keyboard menus use the entity-to-physical-row map, so detail spans never become
fake records. Applications should use this public method rather than infer a
native row from its index in `VisibleEntities`.

The WinForms Tester **Visual Grid > Expandable Panels And Hierarchy** tab shows
recursive tables, an image panel, a normal TextBox editor, independent chrome
flags and delayed preparation. See [SourceGrid composition](../../usage/sourcegrid.md)
for the native span/link mechanism and its limits. These HWND-specific mechanics
are not a Blazor API.

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
| Screen changes | `Title`, screen lifecycle events and merged original menu/toolbar instances. | Override `Title`, expose screen commands, call `NotifyScreenChanged` for updated chrome/dirty state. |
| Runtime block changes | Register/unregister with guards and permanent-screen initialization. | Same logical transition policy without constructing a second block owner. |
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
permission enforcement remains with the application/service.

See the [WinForms host guide](../../../src/Sphere10.Framework.Windows.Forms/README.md#multiple-screens-and-detachable-tabs),
[Blazor host guide](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#default-permanent-and-empty-screens)
and [application ownership](application-ownership.md) for registration, lifecycle,
startup selection and platform-specific examples.
