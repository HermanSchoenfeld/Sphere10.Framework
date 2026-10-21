# Grids and screen hosts on WinForms and Blazor

Both adapters use the Framework's data-source and application-screen contracts.
Use this page to map behavior between platforms, then use the linked project
guides for concrete configuration and examples.

## CRUD grids

| Behavior | WinForms | Blazor |
|---|---|---|
| Typed source | `CrudGrid.SetDataSource<T>` borrows an `IDataSource<T>`. | `BlazorGrid<TItem>.DataSource` borrows the same contract; `IBlazorGridController<TItem>` owns query, selection and draft state. |
| Permissions | Intersects source capabilities with the caller's mask. | `AllowedCapabilities` intersects the source capabilities. |
| Paged | `CrudGridPageMode.Paged`, remembered manual page size. | `BlazorGridPageMode.Paged`, remembered `ManualPageSize`. |
| Auto Paged | Complete ordered rows that fit the native viewport; recalculated for font, DPI and viewport changes. | Complete ordered browser rows that fit the viewport; observes browser layout, zoom and viewport changes. |
| Single Paged | Page zero, `int.MaxValue` length, vertical scrolling. | Same query and presentation policy. |
| Nonpageable source | Complete result; paging controls hidden. | Same behavior; a UI setting cannot grant `CanPage`. |
| Natural widths | Measured once for a binding set; retained through paging, filter, sort and refresh. | Same policy, measured from the first nonempty rendered page. |
| Preferred stretching | `CrudGridColumn<T>.ExpandsToFit`. | `BlazorGridColumn<T>.ExpandsToFit`. |
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
| Preferences | `CrudGridSettings` through application `UserSettings`. | `BlazorGridSettings` in browser storage when `PersistSettings` and an explicit `SettingsKey` are supplied. |

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

A suppressed refresh retains the last committed rows while awaiting the source.
Selection made during that wait belongs to the user and must be reconciled with
the accepted result using stable entity identity. It must not be replaced with
a selection captured before the read. Source, query, binding or disposal changes
invalidate stale completions. Initial loading still has an indicator; inline
editing and mutations retain their own availability/validation rules.

`RefreshableSnapshotDataSource<T,TKey>` supports targeted replacement and removal
through `RefreshRangeAsync`; a null replacement means that the row disappeared.
`SnapshotDataSource<T>` alone deliberately does not supply entity refresh.
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

- [WinForms grid guide](../../../src/Sphere10.Framework.Windows.Forms/README.md#crudgrid)
  and [WinForms tester](../../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md).
- [Blazor grid guide](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#grid-configuration-and-behavior)
  and [Blazor tester](../../../utils/Sphere10.Framework.Utils.BlazorTester/README.md).
- [Native tests](../../../tests/Sphere10.Framework.Windows.Forms.Tests/README.md)
  cover source reads, input routing, selection and native sizing.
- [Blazor tests](../../../tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/README.md)
  cover controller/renderer contracts and opt-in real-browser layout, resize,
  persistence and interaction checks.

## Application screen hosts

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
Blazor preferences belong to the browser and origin under an explicit
application-qualified key; they must not use a process-wide server user-settings
object shared by different users. Browser startup restoration requires an
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
