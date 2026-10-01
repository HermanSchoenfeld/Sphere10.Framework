<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Web.AspNetCore.Blazor

Razor components for .NET 10 Blazor Web Apps using Interactive Server rendering. The library brings Sphere10's ApplicationBlock navigation and `IDataSource<T>` CRUD patterns to the browser.

## Features

- **CRUD grid:** typed columns, buffered editing, validation, confirmed deletion, search, sorting, paging and visible-row selection.
- **Custom presentation:** display/editor templates, existing-record reference pickers, async row commands, resizable columns and optional automatic page sizing.
- **Application workspace:** blocks, menus, retained screens, multiple-instance sessions and navigation guards built on the shared [Application.UI](../Sphere10.Framework.Application/README.md#shared-ui-framework) layer.
- **Additional components:** paged/streaming tables, dialogs, wizards and plugin integration.
- **Theme switching:** scoped Classic blue/Blue/Light/Dark selection, a shared provider and accessible controls, with semantic CSS tokens independent of layout.
- **Host integration:** scoped services, renderer-owned component lifetimes and isolated CSS/JavaScript assets.

Start with [hosting](#current-blazor-hosting-and-assets), then the [complete grid example](#crud-grid-quick-start). See [custom editors and references](#custom-editors-reference-pickers-and-actions) for a second runnable page, or [ApplicationBlock architecture](#applicationblock-architecture) for a navigation workspace.

## Installation

Requires the .NET 10 SDK and a Blazor Web App host. This package references the shared ASP.NET Core layer and `Microsoft.AspNetCore.App`; it is not a standalone WebAssembly package. See the [project dependency guide](../Sphere10.Framework.Web.AspNetCore/README.md) for the other web layers.

From the directory where you want to create the sample host:

```shell
dotnet new blazor -n GridExample --framework net10.0 --interactivity Server
dotnet add GridExample/GridExample.csproj package Sphere10.Framework.Web.AspNetCore.Blazor
```

Use a package version matching this checkout from your configured feed. For unreleased changes, use a project reference to `src/Sphere10.Framework.Web.AspNetCore.Blazor/Sphere10.Framework.Web.AspNetCore.Blazor.csproj`, or follow the repository's [local package workflow](../../README.md#building-and-publishing-packages). Installing the Blazor package also resolves its framework dependencies; do not manually register desktop services.

The examples use the framework's nullable-reference convention. In the generated `GridExample.csproj`, change the existing nullable setting to:

```xml
<Nullable>annotations</Nullable>
```

## Current Blazor hosting and assets

In the generated host, keep its existing configuration and ensure these registrations and endpoints are present. The minimal `Program.cs` equivalent is:

```csharp
using GridExample.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Web.AspNetCore.Blazor;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSphere10Blazor();

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
```

Keep the generated `Components/App.razor` HTML document and router. Its asset setup should include the host's isolated styles bundle, the import map and the Blazor script:

```razor
<link rel="stylesheet" href="@Assets["GridExample.styles.css"]" />
<ImportMap />
<script src="@Assets["_framework/blazor.web.js"]"></script>
```

The pages below explicitly use `@rendermode InteractiveServer`. Registering Interactive Server services/endpoints alone does **not** make a statically rendered page interactive. Alternatively, configure the generated `Routes` and `HeadOutlet` components for global Interactive Server rendering.

The grid loads its collocated `BlazorGrid.razor.js` module on demand, and its isolated CSS is included through the host styles bundle. No global grid script, jQuery or grid-specific service registration is needed. `AddSphere10Blazor()` also registers the surrounding application, table, dialog and wizard services. A grid can be used without registering an ApplicationBlock or adding a modal host.

Modal components import their own JavaScript module. Legacy `css/BlazorGrid.css` and `js/BlazorGrid.js` are compatibility assets; new hosts do not need them. The old DataTables helper still requires an application-provided DataTables plugin.

## Theme switching

Theme state is separate from component markup, navigation and data operations. `AddSphere10Blazor()` registers a scoped `Theming.IThemeService` with Classic blue, Blue, Light and Dark modes. `ThemeMode.ClassicBlue` maps to `classic-blue`; `ThemeMode.Blue` maps to `blue`. Each Interactive Server circuit has its own selection; it survives in-app navigation. The library defaults to Light; the tester chooses Classic blue when a new circuit starts. Applications can replace the service or extend its decorator to integrate their own preferences.

To choose Classic blue as a host's default, register the scoped service before `AddSphere10Blazor()` in `Program.cs`:

```csharp
using Sphere10.Framework.Web.AspNetCore.Blazor.Theming;

builder.Services.AddScoped<IThemeService>(_ => new ThemeService(ThemeMode.ClassicBlue));
builder.Services.AddSphere10Blazor();
```

Load the theme definitions once in the host's `App.razor`, after any base framework stylesheet and before application overrides:

```razor
<link rel="stylesheet" href="@Assets["_content/Sphere10.Framework.Web.AspNetCore.Blazor/css/themes.css"]" />
```

Wrap your interactive layout content, including its modal hosts, in `ThemeProvider`. Place `ThemeSelector` in the layout header to expose all four skins. For example:

```razor
@using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Theming
@inherits LayoutComponentBase

<ThemeProvider>
	<header class="app-header">
		<strong>My application</strong>
		<ThemeSelector />
	</header>
	<main>@Body</main>
</ThemeProvider>
```

The layout's isolated CSS controls positioning, independently of the theme:

```css
.app-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 1rem;
	padding: 1rem;
	color: var(--sphere10-header-text);
	background: var(--sphere10-header-background);
	box-shadow: var(--sphere10-header-shadow);
}
```

The provider changes `data-sphere10-theme` and `data-bs-theme` on a stable wrapper. Classic blue restores the original Loader appearance: a white topbar, pale `#f8f9fc` content background, the `#4e73df` to `#224abe` sidebar gradient, and a `Roboto, Helvetica, Arial, sans-serif` font stack with regular-weight headings. It uses locally available fonts without requesting an external font and maps to Bootstrap's light mode. It does not recreate descendant components: a grid draft, open workspace screen or dialog remains intact when the palette changes. **Blue** keeps the newer blue gradient on both the header and sidebar, with the system font stack and normal heading weights. **Classic blue** uses the white header and original font stack described above. The selector offers Classic blue, Blue, Light and Dark; both blue modes share the same palette tokens and apply only their visual differences. The optional `ThemeToggle` remains a keyboard-operable Light/Dark toggle. A component can also inject `IThemeService` and call `SetTheme(ThemeMode.Dark)` or `Toggle()`.

Semantic `--sphere10-*` variables define backgrounds, surfaces, text, borders, accents, selection, validation and focus colors. Layouts can consume `--sphere10-font-family`, `--sphere10-heading-text`, `--sphere10-heading-weight`, `--sphere10-header-background`, `--sphere10-header-text` and `--sphere10-header-shadow`; sidebar styles use the `--sphere10-navigation-*` background, text, active-text, muted, icon, hover, selected and border tokens. Grid/workspace CSS consumes these tokens while retaining layout rules in isolated stylesheets. Define palette overrides under the provider's theme attribute; keep layout measurements and CRUD logic out of those overrides. The stylesheet also maps its colors and typography into Bootstrap 5 variables for tables, forms and dialogs. Bootstrap is optional for the grid/workspace themselves and is supplied by the host for Bootstrap-based components.

The [tester](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md) demonstrates one shared shell with the selector at the top right and Classic blue selected by default. It loads one Bootstrap base plus the library theme definitions. Do not add archived admin/light/dark stylesheets to individual pages: doing so introduces competing global rules.

## Header search

Reuse `Components.SearchInput` for route search in the layout header. Its provider returns `Models.SearchResult` records containing a display name and a relative or absolute `Uri`. The tester supplies its actual navigation routes through this component:

```razor
@using Sphere10.Framework.Web.AspNetCore.Blazor.Components
@using Sphere10.Framework.Web.AspNetCore.Blazor.Models

<SearchInput SearchProvider="FindPagesAsync" ResultsCount="5" SearchFreqLimitMs="100" />

@code {
	private Task<IEnumerable<SearchResult>> FindPagesAsync(string term) {
		var pages = new[] {
			new SearchResult("Home", new Uri("/", UriKind.Relative)),
			new SearchResult("Grid", new Uri("/components/grid", UriKind.Relative))
		};
		return Task.FromResult(pages.Where(page => page.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
	}
}
```

This Razor example also needs `System`, `System.Collections.Generic`, `System.Linq` and `System.Threading.Tasks` imports, normally supplied by the host's `_Imports.razor`. The input searches as you type; Enter or the search button repeats the current query. Results are ordinary keyboard-accessible links, and Escape closes them. Empty queries clear results. Overlapping requests keep the newest response; changing the provider, dismissing results or disposing the component prevents pending responses from reopening the dropdown. A provider failure displays a retry message; the provider owns application-specific error logging. The component uses the host's scoped styles bundle and theme tokens without requiring Bootstrap JavaScript.

## CRUD grid quick start

Create the following three files in `GridExample`. They provide a complete page-local, in-memory example with validation and source-side searching/sorting. Data resets when the page instance is recreated; use an application data service for persistent storage.

### 1. Define the entity

**Models/Person.cs**

```csharp
using System.ComponentModel;

namespace GridExample.Models;

public class Person {
	[ReadOnly(true)]
	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public int Age { get; set; }

	public bool IsActive { get; set; } = true;

	public string Notes { get; set; } = string.Empty;

	public Person Manager { get; set; }
}
```

Editable rows must be reference types. `[ReadOnly(true)]` prevents the generated ID editor; the source still assigns IDs when creating drafts. `Manager` is used by the custom reference example later.

### 2. Implement the data source

**Services/PeopleDataSource.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using GridExample.Models;
using Sphere10.Framework;

namespace GridExample.Services;

public class PeopleDataSource : ListDataSource<Person> {
	private int _nextId = 4;

	public PeopleDataSource()
		: base(new ExtendedList<Person>()) {
		Future.Value.AddRange(new[] {
			new Person { Id = 1, Name = "Ada", Age = 36 },
			new Person { Id = 2, Name = "Grace", Age = 42 },
			new Person { Id = 3, Name = "Alan", Age = 31, IsActive = false }
		});
	}

	public override DataSourceItems<Person> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		IEnumerable<Person> query = Future.Value;
		if (!string.IsNullOrWhiteSpace(searchTerm))
			query = query.Where(person => person.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

		// Map allowed sort keys explicitly, before taking the requested page.
		Func<Person, object> sortKey = sortProperty switch {
			nameof(Person.Name) => person => person.Name,
			nameof(Person.Age) => person => person.Age,
			nameof(Person.IsActive) => person => person.IsActive,
			nameof(Person.Notes) => person => person.Notes,
			_ => person => person.Id
		};
		query = sortDirection == SortDirection.Descending
			? query.OrderByDescending(sortKey).ThenBy(person => person.Id)
			: query.OrderBy(sortKey).ThenBy(person => person.Id);

		var matches = query.ToArray();
		page = Math.Clamp(page, 0, Math.Max(0, (matches.Length - 1) / pageLength));
		return new DataSourceItems<Person> {
			Items = matches.Skip(page * pageLength).Take(pageLength).ToArray(),
			Page = page,
			TotalCount = matches.Length
		};
	}

	public override Result ValidateRange(IEnumerable<(Person entity, CrudAction action)> actions) {
		var result = new Result();
		foreach (var (person, action) in actions) {
			if (action == CrudAction.Delete)
				continue;
			if (string.IsNullOrWhiteSpace(person.Name))
				result.AddError("Name is required.");
			if (person.Age < 0 || person.Age > 150)
				result.AddError("Age must be between 0 and 150.");
			if (ReferenceEquals(person, person.Manager))
				result.AddError("A person cannot be their own manager.");
		}
		return result;
	}

	public override void CreateRange(IEnumerable<Person> entities) {
		var items = entities.ToArray();
		EnsureValid(items, CrudAction.Create);
		base.CreateRange(items);
	}

	public override void UpdateRange(IEnumerable<Person> entities) {
		var items = entities.ToArray();
		EnsureValid(items, CrudAction.Update);
		base.UpdateRange(items);
	}

	public override void DeleteRange(IEnumerable<Person> entities) {
		var items = entities.ToArray();
		EnsureValid(items, CrudAction.Delete);
		base.DeleteRange(items);
	}

	protected override Person NewMethod() => new() { Id = _nextId++ };

	private void EnsureValid(IEnumerable<Person> entities, CrudAction action) {
		var result = ValidateRange(entities.Select(person => (person, action)));
		Guard.Ensure(result.IsSuccess, string.Join("; ", result.ErrorMessages));
	}
}
```

`ListDataSource<T>` supplies the single-item and asynchronous wrappers. Its default read implementation only pages: it does not implement search or sort despite advertising those capabilities. This example overrides `ReadRange` for that reason. The mutation overrides also enforce validation for callers outside the grid.

For database/HTTP-backed sources, derive from `AsyncBatchDataSourceBase<T>` and implement its asynchronous range operations instead of wrapping blocking I/O. Keep persistence, permission enforcement and concurrency control in the source/service. The sample mutable list is intended for one page instance, not a shared application-wide data store.

### 3. Render the grid

**Components/Pages/People.razor**

```razor
@page "/people"
@rendermode InteractiveServer
@using System.Collections.Generic
@using Microsoft.AspNetCore.Components
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using GridExample.Models
@using GridExample.Services
@using Sphere10.Framework.Web.AspNetCore.Blazor
@using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid

<h1>People</h1>
<p role="status">@_status</p>
<p>@_selection</p>

<BlazorGrid TItem="Person" DataSource="_source" Columns="_columns" PageSize="2"
	Caption="People" SelectedItemChanged="SelectPerson"
	ItemCreated="PersonSaved" ItemUpdated="PersonSaved" ItemDeleted="PersonDeleted" />

@code {
	private readonly PeopleDataSource _source = new();
	private readonly BlazorGridColumn<Person>[] _columns = new[] {
		BlazorGridColumn<Person>.For(person => person.Id, "ID"),
		BlazorGridColumn<Person>.For(person => person.Name),
		BlazorGridColumn<Person>.For(person => person.Age),
		BlazorGridColumn<Person>.For(person => person.IsActive, "Active")
	};
	private string _status = "Ready.";
	private string _selection = "Select a person to edit or delete.";

	private void SelectPerson(Person person) =>
		_selection = person == null ? "Selection cleared." : $"Selected {person.Name}.";

	private void PersonSaved(Person person) => _status = $"Saved {person.Name}.";

	private void PersonDeleted(Person person) => _status = $"Deleted {person.Name}.";
}
```

Run the host and open `/people` at the address printed by the terminal:

```shell
dotnet run --project GridExample/GridExample.csproj
```

Select a row, choose **Edit**, change a value and choose **Save** or **Cancel**. Try an empty name to see validation without losing the draft. **New** creates a draft and persists only after Save. **Delete** requires a visible selection and confirmation. Search by name, click a column header to sort, and use the pager to view the filtered results.

## Grid configuration and behavior

### Capabilities and a read-only grid

The grid uses the intersection of `DataSource.CapabilitiesAsync` and `AllowedCapabilities`, whose default is `DataSourceCapabilities.Default`. The source/service must enforce permissions independently. To expose browsing only, replace the binding in the page above with:

```razor
@using Sphere10.Framework

<BlazorGrid TItem="Person" DataSource="_source" Columns="_columns"
	AllowedCapabilities="BrowseCapabilities" AllowCellEditing="false" />

@code {
	private const DataSourceCapabilities BrowseCapabilities = DataSourceCapabilities.CanRead
		| DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;
}
```

For a plain `ListDataSource<T>` without custom searching/sorting, use a positive mask containing only the operations actually implemented, such as `CanCreate | CanRead | CanUpdate | CanDelete | CanPage`. Capability members such as `CanUpdate`, `CanSearch` and `CanSort` include the `CanRead` bit: use full flag checks, and avoid subtracting composite flags with bitwise negation.

### Typed and automatic columns

`BlazorGridColumn<T>.For(person => person.Name)` configures a direct public property's getter, setter, type and sort key. Nested expressions such as `person.Manager.Name` are not accepted by `For`; use explicit delegates for projections.

| Column member | Purpose |
| --- | --- |
| `ColumnName` | Visible header and default editor label. |
| `PropertyName` | Property metadata for the column. |
| `SortName` | Source-specific sort key; leave unset for an unsortable computed column. |
| `DataType` | Actual value type for conversions, including nullable value types. |
| `PropertyValue` / `SetPropertyValue` | Read/write delegates for a value. Omit the setter for display-only columns. |
| `CanEditCell` | Allows the column's editor when a setter exists. |
| `PropertyHasValue` | Per-row availability predicate; false also suppresses the default display value. |
| `Template` / `EditorTemplate` | Typed display and buffered editor fragments. |
| `Format` | Display format for `IFormattable` values; it does not change editor parsing. |
| `Width` / `ExpandsToFit` | Initial pixel width and whether the column can share spare space. Narrow viewports scroll horizontally instead of collapsing configured columns. |

Explicit `Columns` replace automatic generation. Without them, `AutoGenerateColumns` defaults to true and reflects readable public instance properties, excluding indexers and `[Browsable(false)]` members. Generated definitions honor `[DisplayName]`, `[ReadOnly]`, private setters and init-only properties. Complex values remain read-only unless you configure an editor.

Create column definitions once, for example in a field or `OnInitialized`. Replacing column instances during an edit cancels that draft. `PropertyHasValue = false` makes a value unavailable for editing; do not use it merely because an editable reference is currently null.

Default editors support strings/chars, booleans (including nullable tri-state values), numeric types, enums, GUIDs, `DateOnly`, `TimeOnly`, `DateTime`, `DateTimeOffset` and `TimeSpan`. Dates/times use the appropriate HTML inputs; offset timestamps and durations use text to preserve their full values. Built-in editor conversions use invariant culture, while default cell display uses the current culture. `Tools.BlazorGrid` provides the underlying conversion helpers.

### Selection, editing and validation

- Click a row to select it. With `LeftClickToDeselect = true` (default), clicking it again clears selection. Only current-page items can be selected.
- Choose **Edit**, double-click a row or press **F2** on a focused row. `AllowCellEditing` controls the double-click/F2 gestures; the Edit command remains available when updates are permitted.
- Editing buffers values through `BlazorGridCellEditContext<T>.ValueChanged`. Built-in inputs convert before buffering. Cancel or **Escape** discards a draft without persisting it.
- Save applies changed values, calls `ValidateAsync`, then calls `CreateAsync` or `UpdateAsync`. Rejected validation or failed persistence restores changed fields and keeps the draft for correction/retry.
- Setters must support restoring the original value. Custom editors must publish replacements through `ValueChanged`, rather than mutating `Item` or a referenced object directly. This rollback is not a transaction for external side effects inside a setter or source.
- Delete validates `CrudAction.Delete` before calling `DeleteAsync`. Built-in mutation events fire after successful persistence. A later refresh can fail independently; inspect `ErrorMessage` when handling such a result.
- Paging, search and other grid commands are disabled during editing or an active operation. Replacing the source, permission mask, page-size parameter or column definitions discards a pending draft.

`SelectedItemChanged`, `ItemCreated`, `ItemUpdated` and `ItemDeleted` are `EventCallback<TItem>` parameters. Selection clearing passes the default value, normally null for entity classes. Async handlers should return `Task`.

### Paging and external refresh

`PageSize` defaults to 10 and accepts 1–9999. `CurrentPage` and `SetPageAsync` are **zero-based**; the visible pager starts at one. The source returns the actual zero-based `DataSourceItems.Page` and the filtered `TotalCount`, not the count of items on that page. Filter and sort before paging.

Add `@ref="_grid"` to a grid to call its public component methods from the owning renderer context:

```csharp
private BlazorGrid<Person> _grid;

private async Task RefreshPeopleAsync() {
	if (_grid.Controller.IsEditing || _grid.Controller.IsSaving)
		return;
	await _grid.RefreshAsync();
}
```

This fragment belongs in the page's `@code` block and requires `@using System.Threading.Tasks`. The component also exposes `SetPageAsync(int)`, `SetPageSizeAsync(int)`, `BeginCreate()`, `BeginEdit(item)`, `CancelEdit()` and `SaveAsync()`; Save returns a boolean.

For a grid sized to its viewport:

```razor
<BlazorGrid TItem="Person" DataSource="_source" Columns="_columns"
	AutoPageSize="true" Height="420" />
```

Automatic sizing measures the header and rows within the fixed-height viewport, hides the manual page-size input and uses a conservative row height to avoid repeated changes in page capacity. Column widths can be changed by dragging their header edges.

Each grid owns its controller and browser subscriptions, but **does not own the data source**. If you supply a disposable source, its creator or DI scope must dispose it. Reads are serialized/coalesced and stale results are ignored after a newer request, source replacement or disposal. `IDataSource<T>` does not expose read cancellation tokens, so an already-running source call may still finish.

## Custom editors, reference pickers and actions

The following second page reuses `Person` and `PeopleDataSource`. It demonstrates a custom Notes editor, choosing an existing Manager, a display template and an awaited row command.

**Components/Pages/PeopleCustom.razor**

```razor
@page "/people-custom"
@rendermode InteractiveServer
@using System.Collections.Generic
@using System.Threading.Tasks
@using Microsoft.AspNetCore.Components
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using GridExample.Models
@using GridExample.Services
@using Sphere10.Framework.Web.AspNetCore.Blazor
@using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid

<h1>People with custom editors</h1>
<p role="status">@_message</p>

<BlazorGrid TItem="Person" DataSource="_source" Columns="_columns" Actions="_actions" PageSize="2" />

@code {
	private readonly PeopleDataSource _source = new();
	private BlazorGridColumn<Person>[] _columns = Array.Empty<BlazorGridColumn<Person>>();
	private readonly BlazorGridColumn<Person>[] _managerColumns = new[] {
		BlazorGridColumn<Person>.For(person => person.Id, "ID"),
		BlazorGridColumn<Person>.For(person => person.Name)
	};
	private GridAction<Person>[] _actions = Array.Empty<GridAction<Person>>();
	private string _message = "Edit a person to choose their manager.";

	protected override void OnInitialized() {
		var name = BlazorGridColumn<Person>.For(person => person.Name);
		name.ExpandsToFit = true;
		name.Template = person => @<strong>@person.Name</strong>;

		var notes = BlazorGridColumn<Person>.For(person => person.Notes);
		notes.EditorTemplate = context => @<input aria-label="Notes" maxlength="120"
			value="@context.Value" @onchange="args => context.ValueChanged.InvokeAsync(args.Value)" />;

		var manager = new BlazorGridColumn<Person> {
			ColumnName = "Manager",
			PropertyName = nameof(Person.Manager),
			DataType = typeof(Person),
			PropertyValue = person => person.Manager,
			SetPropertyValue = (person, value) => person.Manager = (Person)value,
			CanEditCell = true,
			Width = 220,
			Template = person => @<span>@(person.Manager?.Name ?? "(None)")</span>,
			EditorTemplate = context => @<BlazorGridReferencePicker TItem="Person"
				DataSource="_source" Columns="_managerColumns" Value="@(context.Value as Person)"
				ValueChanged="person => context.ValueChanged.InvokeAsync(person)"
				DisplayText="ManagerText" Label="manager" AllowNull="true" />
		};
		_columns = new[] { BlazorGridColumn<Person>.For(person => person.Id, "ID"), name, notes, manager };
		_actions = new[] { new GridAction<Person>("Details", person => person, string.Empty) {
			ActionWorkAsync = ShowDetailsAsync,
			IsActionAvailable = person => person.IsActive
		} };
	}

	private static string ManagerText(Person person) => $"{person.Name} (ID {person.Id})";

	private Task ShowDetailsAsync(Person person) {
		_message = $"{person.Name}, age {person.Age}.";
		return Task.CompletedTask;
	}
}
```

`EditorTemplate` receives `Item`, `Column`, buffered `Value`, `ValueChanged` and `IsNewItem`. Forward changes through the callback, as above; do not bind directly to `context.Item.Notes`. You can return a task from custom event handlers and commands when they call application services.

The picker displays the current value and a dropdown arrow in a compact cell editor. Its dropdown contains another grid for browsing/searching the eligible records; the panel uses the browser's top layer so a parent grid's scrolling viewport cannot clip it. Escape or clicking outside closes the panel without assigning a new value.

The picker receives the **full eligible data source**, not `grid.Controller.Items` (the current page). It internally masks writes, edits and row commands. **Use selected** returns the selected existing object; **Cancel** preserves the current draft value; `AllowNull` enables Clear. The parent grid's Save/Cancel controls persistence of that chosen reference. Use shallow display delegates to avoid recursively traversing related entities.

Custom row actions are application commands. They do not automatically run CRUD validation or enforce `CanUpdate`/`CanDelete`; use the built-in commands for normal persistence. `ActionWorkAsync` is awaited and takes precedence over the legacy synchronous `ActionWork`. Errors appear in the grid, and successful actions request a refresh.

## Grid architecture and WinForms correspondence

| Responsibility | Implementation |
| --- | --- |
| Rendering and component parameters | [BlazorGrid.razor](UI/Controls/BlazorGrid/BlazorGrid.razor) and its [code-behind](UI/Controls/BlazorGrid/BlazorGrid.razor.cs) |
| Query, selection and edit workflow | [IBlazorGridController<T>](Grids/IBlazorGridController.cs), abstract base, concrete controller and decorator |
| Value access and templates | [BlazorGridColumn<T>](Grids/BlazorGridColumn.cs) and [BlazorGridCellEditContext<T>](Grids/BlazorGridCellEditContext.cs) |
| Scalar conversion | [Tools.BlazorGrid](Grids/GridTool.cs) |
| Existing-reference selection | [BlazorGridReferencePicker](UI/Controls/BlazorGrid/BlazorGridReferencePicker.razor) |
| Browser layout/lifetime | Collocated isolated CSS and JavaScript |

Both the WinForms `CrudGrid` and Blazor grid consume the same core `IDataSource<T>`, `DataSourceCapabilities`, `CrudAction` and `Result`. WinForms column bindings correspond to typed Blazor columns; entity/property editors correspond to the buffered row editor and Razor templates; native reference dropdowns correspond to `BlazorGridReferencePicker`. Browser rendering and interaction remain in this library. Blazor does not provide a recursive native PropertyGrid; use explicit editors for owned objects.

Sequence-valued data properties use arrays: grid `Columns`, `Actions`, `ColumnDefinitions`, controller `Items` and `ValidationErrors`, application block `Menus`, menu `Items`, catalog/host collections, table `Items`/`Page`, and `ItemsResponse<T>.Items`. Materialize queries with `.ToArray()` before assigning them. Controller and registered application collections return array snapshots; replacing entries in a returned array does not change stored membership. Use configuration setters, builders or host operations to make changes.

`grid.Controller` exposes additional query/edit operations, current items, validation errors and custom entity comparison. Calls made directly on it bypass the component's mutation/selection callbacks and its delete-confirmation UI. Use component methods and controls when you need those behaviors. The component creates and disposes its own controller; do not dispose that instance or share it between grids.


## ApplicationBlock architecture

The application APIs use explicit `Blazor*` and `IBlazor*` names in the root `Sphere10.Framework.Web.AspNetCore.Blazor` namespace as facades over `Sphere10.Framework.Application.UI`. Shared contracts and models retain unprefixed names, so both namespaces can be imported together without aliases. WinForms and Blazor share block/menu storage, builder state and validation, menu notifications, action execution and activation-policy checks. Blazor also delegates registration snapshots and catalog traversal to shared algorithms. The shared library has no ASP.NET Core or desktop UI dependency.

`IBlazorApplicationScreen` extends the shared `IApplicationScreen` and Blazor's `IComponent`. Derive from `BlazorApplicationScreen` for automatic renderer attachment, or implement that interface directly and attach the renderer-created instance to the host. Host/session APIs and `IBlazorApplication.ActiveScreen` expose `IBlazorApplicationScreen`.

| WinForms concept | Blazor implementation |
| --- | --- |
| Plugin configuration | `BlazorPluginBuilder` groups blocks and service registrations through `AddSphere10BlazorPlugin` |
| ApplicationBlock and fluent builders | `BlazorApplicationBlockBuilder`, `BlazorApplicationMenuBuilder`, `BlazorApplicationMenuItemBuilder` |
| Screen host interface/base/concrete/decorator | `IBlazorApplicationScreenHost`, `BlazorApplicationScreenHostBase`, `BlazorApplicationScreenHost`, `BlazorApplicationScreenHostDecorator<TConcrete>` |
| Registered blocks | `IBlazorApplicationBlockCatalog` with immutable structural snapshots, base class and decorators |
| BlazorApplicationScreen | `BlazorApplicationScreen : ComponentBase, IBlazorApplicationScreen` |
| Single-instance screens | One retained, keyed component per screen type in a circuit, including selection through another block |
| Multi-instance screens | Independent sessions and component instances with unique IDs |
| Screen hide/close cancellation | Awaited `CanDeactivateAsync` before changing state |
| Screen display/hide lifecycle | `OnActivatedAsync` / `OnDeactivatedAsync`; disposal stays with the Razor renderer |
| Desktop images and control menus | Browser icon URLs/CSS classes and Razor menu components |
| Desktop screen tabs | `UI.Application.ApplicationShell` with open-screen selection and close controls |
| Screen navigation | Registered block/screen IDs and optional session IDs in the URL, with Back/Forward and bookmark support |

The platform facade supplies component-type validation, icon/parameter metadata, Razor rendering, URL navigation and circuit-local screen state. The shared `IApplicationScreen` supplies help metadata and asynchronous lifecycle contracts; the WinForms extension adapts its synchronous notifications to that contract. Image/control ownership remains in WinForms, while Blazor leaves component disposal to its renderer. Both platforms use `Sphere10.Framework.Application.UI.ScreenActivationMode` directly; there is one activation-policy enum.

## Register a workspace

A grid does not require a workspace. To add navigation, first create a screen component:

**Components/Screens/OverviewScreen.razor**

```razor
@namespace GridExample.Components.Screens
@inherits Sphere10.Framework.Web.AspNetCore.Blazor.BlazorApplicationScreen

<h1>Overview</h1>
<p>This component is retained while you switch between workspace screens.</p>
<button type="button" @onclick="() => _count++">Count: @_count</button>

@code {
	private int _count;
}
```

Add these imports at the top of `Program.cs`, then place the registration before `builder.Build()` in the host configured earlier:

```csharp
using GridExample.Components.Screens;
using Sphere10.Framework.Web.AspNetCore.Blazor;

builder.Services.AddSphere10BlazorPlugin(plugin => plugin
	.WithName("Workspace")
	.AddBlock(block => block
		.WithId("work")
		.WithName("Workspace")
		.WithDefaultScreen<OverviewScreen>()
		.AddMenu(menu => menu.WithId("main").WithText("Work").WithIcon("fas fa-desktop")
			.ConfigureItem(item => item
				.WithId("overview")
				.WithText("Overview")
				.WithIcon("fas fa-home")
				.WithScreen<OverviewScreen>()))));
```

`BlazorPluginBuilder` composes `BlazorApplicationBlockBuilder`, `BlazorApplicationMenuBuilder` and `BlazorApplicationMenuItemBuilder`. Add more blocks with `AddBlock(...)`, and register plugin dependencies with `ConfigureServices(registry => ...)`. For example, a plugin can call `registry.AddScoped<WorkspaceStatus>()` for its own application-defined state service. Configuration runs during host startup; scoped services are created by the host for each circuit. Repeated configuration callbacks compose in registration order.

For reusable definitions, call `new BlazorPluginBuilder().WithName(...).AddBlock(...).Build()` and pass the result to `AddSphere10BlazorPlugin(plugin)`. Existing blocks can be supplied to `AddBlock(block)`, including blocks created with `new BlazorApplicationBlockBuilder()...Build()`. `AddApplicationBlock(...)` remains available when a plugin grouping is unnecessary. The fluent registration calls `Build()` for you and exposes the plugin through `IBlazorPlugin` alongside the existing block catalog. It does not create a child service provider.

For named plugin definitions, place the fluent configuration in `public static void Configure(BlazorPluginBuilder plugin)` and register it with `services.AddSphere10BlazorPlugin(MyPlugin.Configure)`. The running tester uses this pattern in [Sphere10Plugin](../../utils/Sphere10.Framework.Utils.BlazorTester/Loader/Sphere10Plugin.cs) and [WidgetGalleryPlugin](../../utils/Sphere10.Framework.Utils.BlazorTester/WidgetGallery/WidgetGalleryPlugin.cs); their dependencies and block/menu definitions live together, and `Program` registers those definitions directly.

Plugin names must be nonblank and unique within a host. IDs must be unique within their scope; item IDs are unique across all menus in a block. Register a concrete Razor component implementing `IBlazorApplicationScreen`, normally by inheriting `BlazorApplicationScreen`. Conflicting single/multiple instance policies for the same type are rejected when the catalog is constructed. A default screen need not also appear in a menu.

`AddApplicationBlock` also registers the shared `IApplicationBlock` view, and `AddSphere10Blazor` exposes the same catalog through `IApplicationBlockCatalog<IApplicationBlock>`. Application services can inspect registrations without referencing Blazor:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;

var catalog = app.Services.GetRequiredService<IApplicationBlockCatalog<IApplicationBlock>>();
var workspaceName = catalog.Get("work").Name;
```

Block definitions are application-wide snapshots. Open sessions, active selections, event aggregation and modal services are scoped to the user circuit. Action delegates receive that circuit's `IServiceProvider`: do not capture a scoped service in a singleton registration. Registration-time menu event handlers are retained by the snapshots and must also avoid capturing per-user state. Parameter dictionaries are copied, but their values are shallow copies; use immutable parameter values and inject mutable per-user services into the component.

Host the shell in an interactive routable page, for example **Components/Pages/Workspace.razor**:

```razor
@page "/application"
@rendermode InteractiveServer
@using System
@using Microsoft.AspNetCore.Components
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application

<ApplicationShell Title="My application" NavigationPath="application"
	BlockId="@BlockId" ScreenId="@ScreenId" InstanceId="InstanceId" />

@code {
	[SupplyParameterFromQuery(Name = "block")] public string BlockId { get; set; }
	[SupplyParameterFromQuery(Name = "screen")] public string ScreenId { get; set; }
	[SupplyParameterFromQuery(Name = "instance")] public Guid? InstanceId { get; set; }
}
```

`NavigationPath` is relative to the host's base URI. The shell also accepts `Header`, `Sidebar`, `Footer` and `EmptyContent` fragments. For an existing layout, compose `ApplicationBlockMenu`, `ApplicationMenus` and `ApplicationScreenHostView` directly with `IBlazorApplicationScreenHost`. A menu selection awaits its action and exposes failures to the containing Blazor error boundary.

Bookmarks identify registered screens, never arbitrary component type names. A session ID restores an existing screen in the current workspace; a fresh circuit recreates the screen from its registered definition. Retained component state lasts while the workspace remains mounted. Refreshing or leaving the workspace disposes its components; use a scoped state service or persistence when longer retention is required.

### Compact block menus

`UI.MainFrame.ApplicationBlockMenu` uses `Blocks`, `ActiveBlock` and `OnSelect` for its default command-button presentation. Set `Compact="true"` to show icons with accessible block names. Icons come from each block's `IconUrl`; a missing icon falls back to the title's first character. `Tooltip` supplies the hover text, defaulting to the block title. The image backdrop can be customized with `--sphere10-block-icon-background` for different artwork.

Supply `Href`, a `Func<IBlazorApplicationBlock, string>`, to render ordinary navigation links instead of `OnSelect` buttons:

```razor
<ApplicationBlockMenu Blocks="ScreenHost.Blocks" ActiveBlock="ScreenHost.ActiveBlock"
	Compact="true" Href="GetBlockHref" />

@code {
	private string GetBlockHref(IBlazorApplicationBlock block) =>
		$"{Navigation.ToAbsoluteUri("application")}?block={Uri.EscapeDataString(block.Id)}";
}
```

Here `ScreenHost` is the injected `IBlazorApplicationScreenHost`, and `Navigation` is the injected Blazor `NavigationManager`; import `Sphere10.Framework.Web.AspNetCore.Blazor` and its `UI.MainFrame` namespace. The destination page passes its query parameters to `ApplicationShell`, which activates the block and applies its existing screen guards and history handling. `Href` only computes the URL; it does not change host state.

A dock rendered independently of the shell should observe `ScreenHost.Changed` and `Navigation.LocationChanged`, dispatch updates through `InvokeAsync`, and unsubscribe when disposed. Pass `ActiveBlock = null` outside the workspace if selection should reflect only the currently displayed workspace. The tester's [DemoBlockDock](../../utils/Sphere10.Framework.Utils.BlazorTester/Layouts/DemoBlockDock.razor) implements this integration using the same scoped host and registered blocks.

## Screen lifecycle and unsaved changes

Override `CanDeactivateAsync(CancellationToken)` to veto screen switching, closure or workspace navigation. Override `HasUnsavedChanges` to enable the browser's confirmation when leaving the page. Call the protected `NotifyScreenChanged()` after changing dirty state so the shell updates its navigation lock.

Override `OnActivatedAsync`, `OnDeactivatedAsync` and `DisposeAsyncCore` for lifecycle work. If overriding `OnInitializedAsync`, await the base implementation so the component attaches to its session. The renderer creates and disposes screen components; never instantiate or dispose them through the host.

Host transitions are serialized. Lifecycle callbacks must not await another transition on the same host; menu actions run outside the transition gate and may navigate. Cancellation and guard vetoes leave the selection unchanged before commit. A post-commit activation failure is propagated with the selected screen still active. Unregistering a block preflights all its open screens before removing any session. `CanNavigateAsync` checks hidden screens too.

## Dialogs and other components

The `Modal.ViewService` API needs a rendered `UI.Dialogs.ModalHost` in the same interactive scope. This page provides its own host; do not add another if your interactive layout already provides one. Retain the generated host's Bootstrap stylesheet for the dialog's Bootstrap classes.

**Components/Pages/Dialogs.razor**

```razor
@page "/dialogs"
@rendermode InteractiveServer
@using System.Threading.Tasks
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Sphere10.Framework.Web.AspNetCore.Blazor.Modal
@using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Dialogs
@inject ModalService Modals
@inject ViewService Views

<button type="button" @onclick="ConfirmAsync">Confirm an action</button>
<p role="status">@_message</p>
<ModalHost @ref="_host" />

@code {
	private ModalHost _host;
	private string _message;

	protected override void OnAfterRender(bool firstRender) {
		if (firstRender)
			Modals.Initialize(_host);
	}

	private async Task ConfirmAsync() {
		_message = await Views.ConfirmDialogAsync("Confirm", "Continue with this action?", "Continue")
			? "Confirmed" : "Cancelled";
	}
}
```

`Views.DialogAsync(title, message)` displays an information dialog. `Views.WizardDialogAsync(wizard)` displays a wizard built with `Wizard.DefaultWizardBuilder<TModel>`. See the reusable [wizard demo](../../utils/Sphere10.Framework.Utils.BlazorTester/Demos/WizardsDemo.razor) for steps and finish callbacks, and the [table demo](../../utils/Sphere10.Framework.Utils.BlazorTester/Demos/TablesDemo.razor) for paged, virtual-paged and streaming examples.


Wizards expose `IsCancellable` and builders support `WithCancellation(bool)`. The current step's cancellation flag also applies. The wizard host enables Cancel and the modal close affordance only when cancellation is permitted; `CancelAsync()` still performs any application-specific guard. Both demos include an **Allow cancellation** option before opening the wizard.

Removing a modal host while a dialog is pending completes the interaction with `ModalResult.Cancel`, including cancellation during rendering or JavaScript startup. It does not surface an unhandled task cancellation through the originating UI event. Unrelated errors and cancellations still propagate. This lifecycle cleanup is independent of whether the user is permitted to press Cancel.

`PagedTable<TItem>` displays an in-memory collection, `VirtualPagedTable<TItem>` uses an asynchronous items provider, and `RapidTable<TItem>` consumes an asynchronous stream. Choose the CRUD grid when you need persistence, validation and editing rather than display-only row templates.

## Existing components and migration

- `Components`, `ViewModels`, `Models`, `Services`: original MVVM components, tables and modal/wizard framework.
- `Plugins`: root-namespace `BlazorPlugin` definitions/builders and legacy routed navigation with `BlazorRoutedApplication`, `BlazorRoutedApplicationBlock`, `BlazorRoutedApplicationPage`, `BlazorRoutedPlugin` and their `IBlazorRouted*` contracts. Routed pages describe URLs; they do not implement the shared hosted-screen block contract.
- `Application`, `Grids`: application workspace contracts, builders and grid behavior in the root namespace.
- `Modal`, `Wizard`: dialog and wizard behavior in the corresponding domain namespaces.
- `UI`: application workspace, dialog, wizard, table and editable-grid rendering. Component-specific support types live beside their `.razor` files in the same namespace.

Both generations contain names such as `ModalHost` and `PagedTable`; import the generation locally or use fully qualified component tags. When hosting both dialog APIs in one layout, give their hosts distinct `Id` values (for example, `legacy-modal` and `modern-modal`) and initialize each corresponding service. The default ID remains `modal` for existing hosts. `AddSphere10BlazorPlugins<TPluginLocator>()` retains routed plugin/navigation registration through `IBlazorRoutedPluginLocator`. Reusable library layouts no longer contain sample profile, endpoint or placeholder menu entries.

`UI.Controls.BlazorGrid.BlazorGrid<TItem>` uses `IDataSource<TItem>` asynchronous CRUD. Component parameters are auto-properties synchronized through lifecycle methods; virtual paging rejects stale responses and streaming tables cancel/restart enumeration when the source changes. Wizards validate the final step and complete once.

The former modern `ApplicationBlock`, `MenuBuilder`, `MenuItemBuilder`, `PluginBuilder` and `ApplicationScreen` types are now `BlazorApplicationBlock`, `BlazorApplicationMenuBuilder`, `BlazorApplicationMenuItemBuilder`, `BlazorPluginBuilder` and `BlazorApplicationScreen`. Their interfaces, catalog, session and host follow the same platform prefix. Implement shared lifecycle members through `Application.UI.IApplicationScreen` when using explicit interface implementations; `IBlazorApplicationScreen` is the sole Blazor screen contract. The former routed `App`, `AppBlock` and `AppBlockPage` types use the distinct `BlazorRoutedApplication*` names above.

Replace old `Sphere10.Framework.DApp.Presentation*` namespaces with this library's namespace. Inject scoped modal/view services instead of using static services. Legacy `BlazorApplication.Initialize(IServiceCollection)` remains a startup/plugin configuration API; `AttachScreenHost` projects its active state from a scoped host. Do not share that attached application instance across circuits.

## Build, test and package

Run these commands from the repository root:

```shell
dotnet build src/Sphere10.Framework.Web.AspNetCore.Blazor/Sphere10.Framework.Web.AspNetCore.Blazor.csproj -c Release -p:BuildRevision=0
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0
```

The [test project README](../../tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/README.md) lists the grid fixtures and focused test commands. The [Blazor tester guide](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md) explains how to run `/components/grid` for CRUD examples, the other `/components/*` pages, `/application` for the workspace and the retained legacy gallery routes. `/modern` remains a compatibility route for the grid.

For isolated package-consumer verification, run [validate-packages.ps1](../../validate-packages.ps1) from the repository root. It packs the public framework graph, builds external consumers and checks a published Blazor host and its Razor assets. It does not publish packages to a registry.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| The grid renders but buttons do nothing | The page or its containing `Routes` must use Interactive Server rendering; service registration alone is insufficient. Check that `blazor.web.js` loads and the circuit connects. |
| The grid is unstyled or resizing is unavailable | Include the generated host `*.styles.css` bundle and `ImportMap`, and map static assets. Check requests for the library's collocated JS module. |
| Search or sorting changes nothing | Implement filtering/sorting in the source before paging, or mask unsupported capabilities. Base `ListDataSource<T>` only pages. |
| An edit disappears after a parent render | Keep the source and column instances stable; changing bindings intentionally discards a pending draft. |
| Save rejects an otherwise valid custom editor value | Set the column's actual `DataType` and pass a compatible replacement through `ValueChanged`; a reference picker should pass the entity object. |
| The grid still looks stale after an external write | Await `RefreshAsync()` on the component once editing/saving has finished. |
| A modal service says no host is initialized | Render and initialize the matching modal host in the same interactive scope before calling the service. Do not mix the two modal API generations. |
| A tester cannot bind its port | Stop the existing instance or choose another port; see the [tester startup guide](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md). |

## Related projects

- [Sphere10.Framework.Application](../Sphere10.Framework.Application/README.md): shared UI contracts, metadata, builders and application services.
- [Sphere10.Framework.Web.AspNetCore](../Sphere10.Framework.Web.AspNetCore/README.md): hosting, HTTP, logging and middleware.
- [Sphere10.Framework.Web.AspNetCore.MVC](../Sphere10.Framework.Web.AspNetCore.MVC/README.md): controller/view and server-form APIs.
- [Sphere10.Framework.Windows.Forms](../Sphere10.Framework.Windows.Forms/README.md): desktop ApplicationBlock and CrudGrid counterparts.
- [Blazor tester](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md) and [regression tests](../../tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/README.md): runnable examples and verification.

## License

Distributed under the MIT License; see [LICENSE](../../LICENSE).
