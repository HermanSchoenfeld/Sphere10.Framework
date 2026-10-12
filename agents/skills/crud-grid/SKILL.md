---
name: crud-grid
description: Configure or extend WinForms CrudGrid, CRUD entity editors, and CRUD reference dropdowns. Use for data-bound CRUD screens, column editing, paging, selection, and popup layout in Sphere10 applications.
---

# CRUD Grid

Use `CrudGrid` with `IDataSource<TEntity>` in a `WinFormsApplicationScreen`. Follow [winforms-ui](../winforms-ui/SKILL.md) for screen registration and [data-source](../data-source/SKILL.md) when implementing the source. Inspect [CrudTestScreen](../../../utils/Sphere10.Framework.Utils.WinFormsTester/Screens/CrudTestScreen.cs) for a working example; its **CRUD Grid** entry opens a new screen instance each time.

## Bind the source and columns

Assign `GridBindings` using `CrudGridColumn<TEntity>`, then await `SetDataSource(Source, AllowedCapabilities)` and `RefreshGrid()` on the UI thread. `SetDataSource` intersects the supplied mask with the source's advertised capabilities; assigning `Capabilities` later directly replaces that mask. Preserve this intersection when changing allowed operations at runtime. Capability flags configure UI behavior; the data source/service must enforce application permissions and validation too.

Choose the existing sync/async data-source base rather than implementing both hierarchies independently. Reads return the requested page, its actual zero-based `Page`, and the filtered `TotalCount`. Implement application searching/sorting in the source: the base `ListDataSource` read only pages its stored list. `VisibleEntities` contains the current page, not all selectable records.

Column configuration has separate responsibilities:

| Member | Purpose |
|---|---|
| `ColumnName` | Visible header text. |
| `DataType`, `DisplayType` | Actual value type and cell/editor presentation. |
| `PropertyValue`, `SetPropertyValue` | Read/write the value; provide a setter for editable columns. |
| `SortName` | Source-specific sort key, which can describe a computed display column. |
| `PropertyName` | Entity property name/path used to connect a reference binding to the default editor. |
| `CanEditCell`, `PropertyHasValue` | Per-column edit permission and availability for each row. |
| `ExpandsToFit` | Explicitly share spare viewport width with other configured stretch columns. |

For an editable projection such as `Address.Street`, expose a string value and update that owned object in the setter, creating it when null if the model allows. Do not set `PropertyHasValue` to false for a null object that the user should be able to create by editing; unavailable cells cannot edit.

`AllowCellEditing`, `CanUpdate`, `CanEditCell`, and the row's value availability must all permit inline editing. Use `SetEntityEditor<TEntity>(typeof(Editor))` for a custom `ICrudEntityEditor<TEntity>`; the default is the property-grid editor. Create/delete controls follow their capability flags, and delete remains hidden without a selected visible row.

## Selection and paging

With `LeftClickToDeselect = true`, successive single clicks select/deselect a row. Double-click an editable cell or press **F2** to edit without toggling selection; checkbox changes also require the explicit edit gesture. `SelectOnMouseUp` controls when selection is applied. Keep these behaviors consistent when flags change after binding; avoid adding a second mouse selection handler that processes the same click.

`AutoPageSize = true` uses the measured viewport, header, row heights, and horizontal scrollbar to show complete rows. The manual page-size control accepts **1–9999**, in increments of one. Changing width can change page capacity when a horizontal scrollbar appears. Await `RefreshGrid()` when a caller needs the completed binding; it coalesces an active refresh. Do not force a separate read loop from resize handlers.

Use [user-settings](../user-settings/SKILL.md) if page size, filters, or other preferences must survive sessions; do not write settings on every keystroke or layout event.

## Existing references and owned properties

Use `CrudReferenceBinding<TEntity>(Source, Columns, DisplayText)` for an existing related record, such as an employee's manager. Supply the full eligible data source, not the current visible page. The picker masks operations to read/search/sort/page, disables inline editing and command columns, and enables automatic page sizing. Choosing a record preserves its identity; cancelling preserves the original reference. `AllowNull` controls the Clear action.

For an inline reference column, set `DataType` to the referenced entity type, `DisplayType = CrudCellDisplayType.DropDownList`, `ReferenceBinding`, and its value getter/setter. Set `PropertyName` so the same binding reaches the default entity editor. For properties without a grid column, configure `CrudGrid.ReferenceBindings["Manager"]` or a nested property path; grid-level entries override column bindings. When using `DefaultCrudEntityEditor` directly, populate its `ReferenceBindings` before `SetEntity`.

Exact self-reference properties can automatically use the enclosing grid's source in the default editor when the model has no custom converter/editor. Prefer an explicit binding to choose eligible records and useful columns. Keep display delegates shallow; rendering a manager must not recursively traverse the manager graph.

Owned objects such as an address can expand in `DefaultCrudEntityEditor`. Writable concrete reference types with a public parameterless constructor can offer `(Create new)`; cycle expansion stops, and existing model converters/editors are respected. Keep Cancel/Undo behavior and reference identity intact. Accept the edit baseline only after successful data-source persistence; do not globally register type descriptors to customize a single editing session.

## Popup dimensions and column stretching

`CrudReferenceBinding.MaximumDropDownSize` sets the configured content size in device pixels; `DropDownSize` is its compatibility alias. `CrudComboBox.MaximumDropDownSize` and the optional `size` in `SetCrudParameters` use the same fixed-size intent. Defaults are **760 × 380**; the tester's Manager picker uses **520 × 300**. Hosts cap dimensions to available monitor space and account for borders/grips. Loading, filtering, paging, and short result sets must not resize the popup to the data.

The grid fills the popup. Choose the stretch column explicitly through `ExpandsToFit`; for Manager the columns are **ID**, **Name**, and **Unsigned Int Field**, with only **Name** stretching. Natural minimum widths still apply, so long content may scroll. There is no implicit last-column fallback. If popup and main-grid layouts differ, create a separate column binding instead of mutating a shared binding instance.

Keep popup grids' `MinimumSize` compatible with their hosts; an oversized child can clip search/paging buttons even when each button fits its immediate panel. Preserve user resizing while the popup is open.

## Lifetime and verification

Screens own their control subscriptions, timers, and popup lifetime. Stop callbacks and detach handlers when closing/disposal ends that lifetime; asynchronous reads may finish afterward, so check disposal before touching controls. Do not marshal through a control without a usable handle. Respect ownership of any shared data source.

Use [unit-testing](../unit-testing/SKILL.md) for meaningful NUnit checks. UI tests use STA and run without concurrent native popup tests. Exercise the affected capabilities, actual edit commit/undo, selection gestures, complete-row paging, and disposal during delayed reads. For layout changes, verify the grid and every ancestor fit the host, fixed popup bounds remain stable across different result sets, and only configured columns absorb width changes. Inspect native PropertyGrid/SourceGrid hosts when their behavior changes.

Relevant implementations are [CrudGrid](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudGrid.cs), [CrudReferenceBinding](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudReferenceBinding.cs), [CrudReferencePicker](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudReferencePicker.cs), and [DefaultCrudEntityEditor](../../../src/Sphere10.Framework.Windows.Forms/Crud/DefaultCrudEntityEditor.cs). Existing regression fixtures in [WinForms tests](../../../tests/Sphere10.Framework.Windows.Forms.Tests) cover interaction, paging, reference pickers, entity editing, and dropdown sizing. See the [WinForms README](../../../src/Sphere10.Framework.Windows.Forms/README.md) for broader usage.
