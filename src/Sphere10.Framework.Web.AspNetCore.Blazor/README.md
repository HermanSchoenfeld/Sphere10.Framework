<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Web.AspNetCore.Blazor

Razor components for .NET 10 Blazor Web Apps using Interactive Server rendering. The library brings Sphere10's ApplicationBlock navigation and `IDataSource<T>` CRUD patterns to the browser.

## Features

- **CRUD grid:** typed columns, buffered editing, validation, confirmed deletion, search, sorting, paging and visible-row selection.
- **Custom presentation:** display/editor templates, existing-record reference pickers, async row commands, resizable columns and optional automatic page sizing.
- **Application workspace:** blocks, menus, retained screens, multiple-instance sessions and navigation guards built on the shared [Application.UI](../Sphere10.Framework.Application/README.md#shared-ui-framework) layer.
- **Additional components:** paged/streaming tables, dialogs, wizards and plugin integration.
- **Theme switching:** 20 scoped presets, including Classic blue and Blue plus styles adapted from owned templates, with one stylesheet per template and shared accessible controls.
- **Host integration:** scoped services, renderer-owned component lifetimes and isolated CSS/JavaScript assets.
- **Application lifecycle:** circuit-local startup/shutdown tasks, application logging and owned resources, and confirmed exit coordinated with screen and block guards.

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
builder.Services
	.BuildBlazorApplication()
	.WithTitle("Grid example")
	.Build();

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

### Application branding and startup builder

`BuildBlazorApplication()` collects host branding, plugins and scoped application configuration. Its terminal `Build()` registers them in the same `IServiceCollection`, using the existing `AddSphere10Blazor`, `AddSphere10BlazorPlugin` and `AddSphere10BlazorApplication` APIs. It does not create a service provider. Call `Build()` once; the builder rejects later changes or a second build so plugin startup cannot run twice accidentally.

For example, place your SVG icon at `wwwroot/favicon.svg` and configure the application before `builder.Build()`:

```csharp
builder.Services
	.BuildBlazorApplication()
	.WithTitle("Grid example")
	.WithFavicon("favicon.svg", "image/svg+xml")
	.ConfigurePlugin(plugin => plugin
		.WithName("Components")
	)
	.Build();
```

`WithFavicon` accepts a URL and an optional content type. Omit it to leave the icon unconfigured. `AddPlugin(plugin)` registers an existing plugin definition, while `ConfigurePlugin(plugin => ...)` configures and builds a nested `BlazorPluginBuilder`; `ConfigureApplication` composes callbacks that run once for each circuit's application. Branding is an immutable singleton `BlazorApplicationOptions`, so the document head can resolve it without creating a scoped screen host. `AddSphere10Blazor()` supplies default options and preserves previously configured branding.

In `Components/App.razor`, add these directives and replace the template's title/favicon elements inside `<head>` with the configured values. `Assets` maps local icon paths to their fingerprinted static assets:

```razor
@using Sphere10.Framework.Web.AspNetCore.Blazor
@inject BlazorApplicationOptions ApplicationOptions

@* Inside the existing head element: *@
<title>@ApplicationOptions.Title</title>
@if (ApplicationOptions.FaviconUrl != null) {
	<link rel="icon" href="@Assets[ApplicationOptions.FaviconUrl]" type="@ApplicationOptions.FaviconContentType" />
}
```

The host emits these elements; the builder does not modify its HTML. In the component containing `ApplicationShell`, inject the same options and use `<PageTitle>@ApplicationOptions.Title</PageTitle>` and `Title="@ApplicationOptions.Title"` for consistent browser and shell titles. Retain the generated `HeadOutlet`.

The pages below explicitly use `@rendermode InteractiveServer`. Registering Interactive Server services/endpoints alone does **not** make a statically rendered page interactive. Alternatively, configure the generated `Routes` and `HeadOutlet` components for global Interactive Server rendering.

The grid loads its collocated `BlazorGrid.razor.js` module on demand, and its isolated CSS is included through the host styles bundle. No global grid script, jQuery or grid-specific service registration is needed. `AddSphere10Blazor()` also registers the surrounding application, table, dialog and wizard services. A grid can be used without registering an ApplicationBlock or adding a modal host.

Modal components import their own JavaScript module. Legacy `css/BlazorGrid.css` and `js/BlazorGrid.js` are compatibility assets; new hosts do not need them. The old DataTables helper still requires an application-provided DataTables plugin.

### JavaScript interop names

Authored JavaScript entry points use PascalCase. The grid module exports `Initialize`, whose returned behavior exposes `Update`; the reference picker exports `Show` and `Focus`; the modal module exports `Show` and `Hide`. C# callers and test doubles use those exact, case-sensitive names. Browser APIs such as `import`, `focus` and `invokeMethodAsync` retain their platform spelling.

Compatibility globals use `ShowModal`, `HideModal`, `DispatchContentLoadedEvent`, `InitDataTableById` and `ClipboardCopy.CopyText`. Update custom interop calls when upgrading. Component modules own and clean up their observers and event handlers; the grid coalesces measurements into animation frames to avoid resizing feedback loops.

## Theme switching

`AddSphere10Blazor()` registers the scoped `Theming.IThemeService`. Theme selection changes presentation while retaining screen instances, grid drafts, modal state and navigation. The library defaults to Light; the tester starts with Classic blue. `ThemeCatalog.All` exposes immutable definitions (mode, stable ID, display name, stylesheet path and dark/light classification) as an array. The service, provider and selector all use that catalogue.

The **Theme** selector offers 20 presets:

| Family | Presets |
| --- | --- |
| Framework | Classic blue, Blue, Light, Dark |
| Artificial Reason | Artificial Reason, Artificial Reason Green, Artificial Reason Orange, Artificial Reason Violet |
| Boost | Boost |
| HUD | HUD, HUD Light |
| Landkit | Landkit |
| MacAdmin bundle | MacAdmin, MacBeath, MacKart |
| Raleway | Raleway, Raleway Purple, Raleway Green |
| AppStack | AppStack, AppStack Dark |

The [template source audit](../../docs/theming-template-sources.md) records the reviewed distributions, palette and style mappings, font fallbacks and duplicate versions. These are adaptations to the existing framework controls, not alternate copies of the application shell. Original template names identify their presets. Fonts use local fallbacks unless the application supplies the named fonts; no external font requests are made.

Choose a default before registering the framework services:

```csharp
using Sphere10.Framework.Web.AspNetCore.Blazor.Theming;

builder.Services.AddScoped<IThemeService>(_ => new ThemeService(ThemeMode.Hud));
builder.Services.AddSphere10Blazor();
```

Wrap interactive layout content, including modal hosts, in `ThemeProvider`. It includes **one stylesheet for the active template** automatically. The selector is driven by the same catalogue, so there is no separate list of options to maintain:

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

Template files live in `wwwroot/css/themes/<template>.css`; color variants share their template's file. Each file imports `../themes.css`, which contains shared native-control defaults and imports `theme-bootstrap.css` for optional Bootstrap component integration. Hosts using `ThemeProvider` should remove their old explicit `css/themes.css` include. The old URL remains available for existing consumers. No aggregate stylesheet imports every template.

For a static host that does not use `ThemeProvider`, include the chosen template once and apply its scope attribute:

```html
<link rel="stylesheet" href="_content/Sphere10.Framework.Web.AspNetCore.Blazor/css/themes/hud.css" />
<div class="sphere10-theme" data-sphere10-theme="hud" data-bs-theme="dark">...</div>
```

`ThemeProvider` changes the stylesheet and `data-sphere10-theme`/`data-bs-theme` attributes on a stable wrapper. It supports fingerprinted static assets through Blazor's `Assets` collection. `ThemeMode.ClassicBlue` retains the white header, pale content background, blue sidebar gradient and original font stack; `ThemeMode.Blue` retains the newer blue header and sidebar. `ThemeToggle` switches any dark preset to Light and any light preset to Dark. Call `IThemeService.SetTheme(...)` for a specific preset. Selection lasts for the service scope; persistence across reloads belongs to the host's preference service.

Keep layout in component styles and consume semantic tokens for appearance:

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

The theme contract separates semantic colors from component treatments:

| Tokens | Purpose |
| --- | --- |
| `--sphere10-accent`, `secondary`, `success`, `info`, `warning`, `danger`, `light`, `dark` | Semantic fills. Each has a `-contrast` foreground for filled buttons/badges and a readable `-text` foreground for links/status text. |
| `--sphere10-surface`, `surface-raised`, `background`, `text`, `muted`, `border`, `focus` | Shared surfaces, content, boundaries and focus states. |
| `--sphere10-control-padding-x/y`, `control-border-width`, `control-radius`, `input-radius`, `control-shadow` | Input and control dimensions, shape and depth. |
| `--sphere10-button-padding-x/y`, `button-font-weight`, `button-shadow`, `button-text-transform`, `button-letter-spacing` | Button treatment independent of form inputs; Boost's uppercase, raised actions use these. |
| `--sphere10-panel-radius`, `card-border-width`, `card-header-background`, `card-shadow` | Panels/cards, including gradient headers. |
| `--sphere10-table-striped-background`, `table-hover-background`, `grid-cell-padding` | Table and CRUD grid surfaces/density. |
| `--sphere10-font-family`, `heading-font-family`, `heading-weight`, `letter-spacing`, `tab-radius`, `toolbar-background`, `menu-background` | Typography and application chrome. |

All abbreviated names in the table retain the `--sphere10-` prefix. Components consume these tokens; layout and behavior stay in their existing Blazor components. Template files contain values rather than copies of Bootstrap controls or application layouts.

When the host includes Bootstrap 5, the shared integration styles solid/outline buttons, semantic text/background/border utilities, alerts/badges, cards, inputs and input groups, switches/ranges, validation, striped/contextual tables, dropdowns, tabs/pills, pagination, lists, accordions, progress and overlay surfaces. Hover, focus, active, read-only and disabled states share the same palette. Text, background and border utilities compose independently, including opacity utilities. Explicit literal utilities such as `bg-white` still mean white: use `bg-body`/`bg-body-secondary` for themed surfaces. Use `background`, rather than `background-color`, when consuming header tokens that can contain gradients.

Keep modal, toast, tooltip and popover content beneath its `ThemeProvider`; elements portalled outside that scope cannot inherit its palette. The framework modal hosts in the tester already live inside that provider. This CSS does not install Bootstrap JavaScript.

The tester's **Components → Themes** screen (`/components/themes`) demonstrates the complete control palette, nested providers, retained form state and framework dialogs. Change themes there to inspect each source template's component treatments, or use the CRUD grid to check inline editors and reference pickers.

To add a built-in preset, add its `ThemeMode` and catalogue definition, then provide its scoped CSS in the appropriate family file. The selector and provider adopt the definition automatically. Use `.sphere10-theme[data-sphere10-theme="id"]` for preset declarations; common defaults remain on `.sphere10-theme`. Avoid global template resets or template JavaScript in application pages.

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

`PageSize` defaults to 10 and accepts 1–9999. The pager uses accessible First/Previous/Next/Last icon buttons with tooltips and a **Rows per page** dropdown. `PageSizeOptions="new[] { 5, 10, 25, 50, 100 }"` customizes the choices; those are also the defaults. The active size is included automatically even when it is not listed (for example, `PageSize="6"`). Paging and page-size changes remain disabled during editing/loading. Automatic page sizing hides the dropdown. `CurrentPage` and `SetPageAsync` are **zero-based**; the visible pager starts at one. The source returns the actual zero-based `DataSourceItems.Page` and the filtered `TotalCount`, not the count of items on that page. Filter and sort before paging.

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

Sequence-valued data properties use arrays: grid `Columns`, `Actions`, `ColumnDefinitions`, controller `Items` and `ValidationErrors`, application block `Menus`, menu `Items`, UI-service/host collections, table `Items`/`Page`, and `ItemsResponse<T>.Items`. Materialize queries with `.ToArray()` before assigning them. Controller and registered application collections return array snapshots; replacing entries in a returned array does not change stored membership. Use configuration setters, builders or host operations to make changes.

`grid.Controller` exposes additional query/edit operations, current items, validation errors and custom entity comparison. Calls made directly on it bypass the component's mutation/selection callbacks and its delete-confirmation UI. Use component methods and controls when you need those behaviors. The component creates and disposes its own controller; do not dispose that instance or share it between grids.


## ApplicationBlock architecture

The application APIs use explicit `Blazor*` and `IBlazor*` names in the root `Sphere10.Framework.Web.AspNetCore.Blazor` namespace as facades over `Sphere10.Framework.Application.UI`. Shared contracts and models retain unprefixed names, so both namespaces can be imported together without aliases. WinForms and Blazor share block/menu storage, builder state and validation, menu notifications, action execution and activation-policy checks. Blazor also delegates registration snapshots and block traversal to shared algorithms. The shared library has no ASP.NET Core or desktop UI dependency.

`IBlazorApplicationScreen` extends the shared `IApplicationScreen` and Blazor's `IComponent`. Derive from `BlazorApplicationScreen` for automatic renderer attachment, or implement that interface directly and attach the renderer-created instance to the host. The Blazor host retains a `BlazorApplicationScreenSession` implementing that screen contract as a logical proxy. The proxy delegates lifecycle, commands, help and dynamic title to its renderer-created `Screen` component. `IApplication.ScreenHost.ActiveScreen` exposes that screen identity; only Blazor retains tab IDs, immutable component parameters and renderer attachment.

| WinForms concept | Blazor implementation |
| --- | --- |
| Plugin configuration | `BlazorPluginBuilder` groups blocks and service registrations through `AddSphere10BlazorPlugin` |
| Shared application hierarchy | `IApplication.Blocks` → `IApplicationBlock` → menus/screens; `LoadedPlugins` records successful startup contributors, each of which can supply several blocks |
| ApplicationBlock and fluent builders | `BlazorApplicationBlockBuilder`, `BlazorApplicationMenuBuilder`, `BlazorApplicationMenuItemBuilder` |
| Shared screen host | `IApplicationScreenHost` for navigation; `ISingleApplicationScreenHost` for the active screen; `IMultiApplicationScreenHost` for screen collections, with generic projections and shared decorators |
| Blazor host projection | `IBlazorApplicationScreenHost`, `BlazorApplicationScreenHostBase`, `BlazorApplicationScreenHost`, `BlazorApplicationScreenHostDecorator<TConcrete>` |
| Application interaction and commands | `IUserInterfaceServices`, `ApplicationUserInterfaceServices` and `BlazorUserInterfaceServices` |
| BlazorApplicationScreen | `BlazorApplicationScreen : ComponentBase, IBlazorApplicationScreen` |
| Single-instance screens | One retained, keyed component per screen type in a circuit, including selection through another block |
| Multi-instance screens | Independent sessions and component instances with unique IDs |
| Permanent singleton screens | Automatically opened normal screens whose tabs cannot be closed by users |
| Empty-workspace screens | Real tabless components displayed when no normal screens remain |
| Screen hide/close cancellation | Awaited `CanDeactivateAsync` before changing state |
| Screen display/hide lifecycle | `OnActivatedAsync` / `OnDeactivatedAsync`; disposal stays with the Razor renderer |
| Desktop images and control menus | Browser icon URLs/CSS classes and Razor menu components |
| Desktop screen tabs | `UI.Application.ApplicationShell` with open-screen selection and close controls |
| Screen navigation | Registered block/screen IDs and optional session IDs in the URL, with Back/Forward and bookmark support |

`IApplicationScreenHost` owns available and registered blocks, active block and screen selection, activation, screen mode, navigation guards and `UserInterfaceServices`. `ISingleApplicationScreenHost` adds close and close-check operations for the active screen. `IMultiApplicationScreenHost` adds retained/open screen arrays, ordering and operations on particular screen references or batches. Each contract has shared generic projections; `IBlazorApplicationScreenHost` supports both capabilities while its layout changes dynamically. The portable layer has no Blazor session, GUID or renderer-attachment model. `IUserInterfaceServices` borrows the host for command execution and supplies platform interaction and UI dispatch.

Each block owns its `ScreenDefinitions` and optional `DefaultScreenDefinition`. An `IScreenMenuItem` references one of those exact definitions. Definition metadata is immutable; `Activators` returns a defensive array of attached commands. The definition owns activation mode and screen kind; there is no separate `IScreenActivationPolicyRegistry`. Menu builders retain supplied definition prototypes until the final block is built, so `WithScreen(definition)` / `WithDefaultScreen(definition)` let several commands share one registration. Each block or circuit snapshot clones the definition graph once, preserving canonical references within that graph. Different definition objects remain distinct aliases even when they declare the same type. Opening a definition directly requires the host's owning block and its canonical definition: `host.OpenScreenAsync(block, definition)`. Definitions from other graphs are rejected.

Ownership is explicit: `item.Parent` is its menu, `menu.Parent` is its block, and `block.Parent` is the circuit's application. `block.Source` retains the original successfully loaded plugin; standalone blocks have no source. Unregistering a block clears its application parent while preserving its source and graph for registration again. Toolbar-only and generated screen commands use nonvisual owner menus. `ExecuteMenuItemAsync(item, cancellationToken)` accepts the command object. Screen command execution follows `item.Parent.Parent` and requires the current circuit's canonical block and definition references; detached registration prototypes and commands from other circuits are rejected. Application menus, toolbars and merged presentation menus can borrow block-owned commands while retaining their original references and parents. Browser route IDs are resolved separately through the screen host's navigation APIs.

Platform interfaces specialize these ownership properties and project them to the shared contracts through inline default interface members in `#region Base Interface Bridges`. An attached screen can use `Block.Parent.Logger` for its application's logger and its own `Host` for navigation. Use these object relationships instead of searching all blocks or resolving command IDs during ordinary execution.

The type convenience `WithDefaultScreen<TScreen>()` resolves matching menu metadata at build time, keeping the command's readable caption and canonical definition when no override is supplied. A null activation mode on a definition is unspecified and inherits a lifetime declared for the same component type. The older direct `BlazorApplicationBlock.DefaultScreen = typeof(...)` initializer keeps its fallback to a generated multi-instance definition when no matching menu exists; use the builder or an explicit definition for portable declarations.

Browser IDs belong to `IBlazorApplicationBlock`, `IBlazorApplicationMenu` and `IBlazorApplicationMenuItem`; they support URLs and browser command merging without entering the shared metadata contracts. `BlazorApplicationMenuItemBase` is the abstract storage/notification base for screen, action, group and separator items. Toolbar-only screen commands also register their definitions with the block. Component parameters and entity reuse keys stay in the Blazor activation API.

Definitions registered without a menu command receive a browser-local `__screen:<index>` route when opened, so their tabs can also be bookmarked and restored. The index follows the block's definition order. The adapter reserves this prefix and `__default` for generated navigation; use application-defined IDs for ordinary menu commands. These browser route IDs are absent from the shared definition.

Inject the smallest host contract needed: `IApplicationScreenHost` for navigation, `ISingleApplicationScreenHost` to close the current screen, or `IMultiApplicationScreenHost` for collections and instance operations. Use `IBlazorApplicationScreenHost` when renderer attachment, tab IDs or runtime component parameters are needed. `AddSphere10Blazor` aliases all host contracts to one circuit-local instance, with a separate scoped UI service exposed by `ScreenHost.UserInterfaceServices`. `IApplication` exposes runtime blocks, loaded plugin metadata, application commands and lifecycle without duplicating selection or interactions. `IApplication<TPlugin, THost>` defines the typed aggregate projections once. The shell uses the Blazor extension for tabs and routing.

`ScreenSelected` distinguishes explicit tab selection from general screen state changes. Browsing a block keeps the displayed screen intact; dirty-state notifications and component attachment do not reset that navigation choice. Selecting a tab restores its owning block, including when the selected tab was already active. Retained sessions and attached components expose typed `Host` and `Block` references before activation callbacks. Detachment, failed attachment, closure and host disposal clear component ownership; the renderer still creates and disposes the component. Screen menus and unparented toolbar commands receive their block ownership during attachment. The application does not duplicate `ScreenHost.ActiveBlock`.

```csharp
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Application;

public class WorkspaceNavigator {
	private readonly IApplicationScreenHost _screenHost;

	public WorkspaceNavigator(IApplicationScreenHost screenHost) {
		Guard.ArgumentNotNull(screenHost, nameof(screenHost));
		_screenHost = screenHost;
	}

	public Task<IApplicationScreen> OpenOverviewAsync(IApplicationBlock block, IScreenMenuItem overview, CancellationToken cancellationToken = default) =>
		_screenHost.ActivateScreenAsync(block, overview, cancellationToken);
}
```

Host calls run on the platform UI context. Guard and lifecycle callbacks must not await another transition on the same host; command actions can navigate because they execute outside the transition gate. Cancellation is honored before selection/removal commits. Views remain owned by the renderer or native host, and closing a logical Blazor session never directly disposes its component.

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

builder.Services
	.AddSphere10BlazorPlugin(plugin => plugin
		.WithName("Workspace")
		.ConfigureBlock(block => block
			.WithId("work")
			.WithName("Workspace")
			.WithDefaultScreen<OverviewScreen>()
			.ConfigureMenu(menu => menu
				.WithId("main")
				.WithText("Work")
				.WithIcon("fas fa-desktop")
				.ConfigureItem(item => item
					.WithId("overview")
					.WithText("Overview")
					.WithIcon("fas fa-home")
					.WithScreen<OverviewScreen>()
				)
			)
		)
	);
```

`BlazorPluginBuilder` inherits `WithName`, `AddBlock(block)`, `ConfigureBlock(block => ...)`, `ConfigureServices`, `RegisterInitializer<T>()` and `RegisterFinalizer<T>()` from the shared typed `ApplicationPluginBuilderBase`; it supplies the Blazor plugin factory and composes `BlazorApplicationBlockBuilder`, `BlazorApplicationMenuBuilder` and `BlazorApplicationMenuItemBuilder`. Its product derives from the shared `ApplicationPlugin`, which owns name/block storage, startup service callbacks and Loaded/Unloaded notifications. Configure more blocks with `ConfigureBlock(block => ...)` or register existing definitions with `AddBlock(block)`, and register plugin dependencies with `ConfigureServices(registry => ...)`. For example, a plugin can call `registry.AddScoped<WorkspaceStatus>()` for its own application-defined state service. Configuration runs during host startup; scoped services are created by the host for each circuit. Repeated configuration callbacks compose in registration order.

For reusable definitions, call `new BlazorPluginBuilder().WithName(...).ConfigureBlock(block => ...).Build()` and pass the result to `AddSphere10BlazorPlugin(plugin)`. Existing blocks can be supplied to `AddBlock(block)`, including blocks created with `new BlazorApplicationBlockBuilder()...Build()`. `AddApplicationBlock(...)` remains available for standalone blocks, which appear directly in the running application's `Blocks`. The fluent registration calls `Build()` for you and exposes the same plugin object through both `IBlazorPlugin` and the shared `IApplicationPlugin`, alongside shared host navigation and UI interaction services. It does not create a child service provider.

`BlazorPlugin` uses `ApplicationPluginBase<IBlazorApplicationBlock>`, preserving the existing `ApplicationPlugin` class contract. Its single compatibility projection keeps typed virtual getters authoritative through both class and interface views. Direct interface implementations use the default projection on `IApplicationPlugin<TBlock>`; plugin classes no longer repeat that adapter.

For named plugin definitions, place the fluent configuration in `public static void Configure(BlazorPluginBuilder plugin)` and register it with `services.AddSphere10BlazorPlugin(MyPlugin.Configure)`. The running tester uses this pattern in [Sphere10Plugin](../../utils/Sphere10.Framework.Utils.BlazorTester/Loader/Sphere10Plugin.cs) and [WidgetGalleryPlugin](../../utils/Sphere10.Framework.Utils.BlazorTester/WidgetGallery/WidgetGalleryPlugin.cs); their dependencies and block/menu definitions live together, and `Program` supplies those definitions through the application builder's `ConfigurePlugin` method.

Plugin names must be nonblank and unique within a host. IDs must be unique within their scope; item IDs are unique across all menus in a block. Register a concrete Razor component implementing `IBlazorApplicationScreen`, normally by inheriting `BlazorApplicationScreen`. Conflicting activation policies or screen kinds for the same type are rejected when UI registrations are validated. A default screen need not also appear in a menu.

`AddApplicationBlock` also registers the shared `IApplicationBlock` view. The Blazor screen host captures validated structural snapshots and exposes `AvailableBlocks`, `Blocks` and `ActiveBlock`; there is no separate public block catalogue service. Resolve it from the current circuit scope to inspect or change navigation:

```csharp
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;

var host = scopedServices.GetRequiredService<IApplicationScreenHost>();
var workspace = host.AvailableBlocks.Single(block => block.Name == "Work");
await host.SelectBlockAsync(workspace, cancellationToken);
```

Definitions and plugin registrations are application-wide. Their UI snapshots, open sessions, active selections, event aggregation and modal services belong to the user circuit. Action delegates receive that circuit's `IServiceProvider`: do not capture a scoped service in a singleton registration. Registration-time menu event handlers are retained by snapshots and must also avoid capturing per-user state. Parameter dictionaries are copied, but their values are shallow copies; use immutable values and inject mutable per-user services into the component.

Await `ReportInfoAsync`, `ReportErrorAsync` and `AskYNAsync` for modal interaction. `BlazorUserInterfaceServices` uses the existing `Modal.ViewService` and the shell's renderer dispatcher; completion waits for dismissal or confirmation. Synchronous `AskYN` is unsupported because blocking a Blazor circuit prevents interaction. For the same reason, synchronous application `Exit` rejects a renderer synchronization context before starting an exit request; use `ExitAsync` there. Worker threads can use synchronous `Exit`, and an already exited application returns true on either context. `ExecuteInUIFriendlyContextAsync` awaits renderer dispatch and propagates errors to its caller. The shell attaches its controller for its own lifetime; the coordinator borrows the screen host and never disposes it.

For runtime entity editors, register the component with `AsMultiInstance()` and open its canonical definition with immutable entity IDs and an optional reuse key. Inside an attached `BlazorApplicationScreen`, use its existing `Block` and `Host` references:

```csharp
using System.Collections.Generic;
using System.Linq;
using Sphere10.Framework.Web.AspNetCore.Blazor;

var activation = new BlazorApplicationScreenActivation(
	new Dictionary<string, object> { [nameof(ProjectScreen.ProjectId)] = projectId },
	instanceKey: projectId.ToString("N")
);
var definition = Block.ScreenDefinitions.Single(definition => definition.ScreenType == typeof(ProjectScreen));
var session = await Host.OpenScreenAsync(Block, definition, activation, cancellationToken);
```

`BlazorApplicationScreenActivation` copies the parameter dictionary immediately; new sessions merge those overrides with the registered defaults using case-insensitive parameter names. Values are shallow copies, so pass immutable identifiers rather than mutable draft objects. A nonnull key reuses a multi-instance session for the same owning block object, registered screen menu ID and component type in the current circuit. Reuse selects the existing session without replacing its original parameters or draft. Keys use ordinal, case-sensitive equality. Null keys retain ordinary multi-instance behavior. `SingleInstance` and `PermanentSingleton` still reuse by component type regardless of the supplied key; runtime activation cannot replace the registered screen type or policy.

Key lookup, creation, switching and removal all run through the existing serialized transition path and navigation guards. Closing a session, unregistering its block or releasing it in single-view mode also releases its key. The session exposes its effective `InstanceKey` (null for singleton sessions), while its random `Id` remains the renderer/tab identity. Use stable entity IDs in application deep links and resolve them again after refresh; circuit session IDs and retained drafts are not durable storage. Components remain owned and disposed by the renderer.

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

For stable entity URLs, optionally supply `NavigationUriFactory="GetScreenUri"`, where `GetScreenUri(BlazorApplicationScreenSession session)` returns a path such as `project/{projectId:D}` using the session's immutable parameters, or `application` for ordinary/null sessions. The callback is evaluated against the active session inside location synchronization, avoiding a parent-render race when tabs change. Direct programmatic host activation also schedules URL synchronization after rendering, while in-flight route requests and shell interactions retain ownership of their own navigation. Its path must remain within the application base and contain no query or fragment. When supplied, the shell uses that path directly and omits its default ephemeral block/screen/instance query. The containing routable page owns resolving that stable URL to a keyed activation on entry or refresh. Generated selection URLs do not repeat the host's already-accepted leave guards; other internal navigation still checks unsaved screens.

`NavigationPath` is relative to the host's base URI. `ApplicationShell` supplies the application layout itself: menus and toolbar span the top, active-block navigation fills the left sidebar, compact block icons sit at its bottom, and tabs sit directly above the scrolling screen area. Place it inside a theme/modal provider layout without a second navigation shell. `Header` supplies branding beside the compact application title; `ToolBarContent` places search, theme or identity controls beside the command buttons in the toolbar below the file menus. The toolbar renders when that fragment is supplied even if there are no command items. `ApplicationCommandBar` exposes the same `ToolBarContent` slot for independent layouts. `NavigationActiveItemId` optionally selects a native sidebar command for a guarded subview inside the active screen; leave it null to select the screen registration as before. Update the value only after the subview accepts navigation. `SidebarHeader` supplies branding or an endpoint selector, `Sidebar` adds content below the block menus, and `Footer` supplies the status bar. `EmptyContent` is a fallback fragment when no screen is selected; a registered `ScreenKind.Empty` screen supplies a real component with its own lifetime instead. Narrow screens expose the navigation through the Menu button. For an existing layout, compose `ApplicationBlockMenu`, `ApplicationMenus` and `ApplicationScreenHostView` directly with `IBlazorApplicationScreenHost`. A menu selection awaits its action and exposes failures to the containing Blazor error boundary.

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

A dock rendered independently of the shell should observe `ScreenHost.Changed` and `Navigation.LocationChanged`, dispatch updates through `InvokeAsync`, and unsubscribe when disposed. Pass `ActiveBlock = null` outside the workspace if selection should reflect only the currently displayed workspace. `ApplicationShell` already supplies this integration and renders its dock as command buttons, so the tester consumes it directly instead of maintaining another dock component.

### IdentityControl

`IdentityControl` in `Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls` is reusable in any Blazor layout. It displays a fixed 32-pixel circular avatar and a dropdown of existing `IBlazorApplicationMenuItem` commands. The avatar keeps the same dimensions for authenticated and anonymous users, so signing in or out does not resize the toolbar. It accepts identity data and delegates command handling; it does not require an authentication service or perform sign-in/sign-out itself.

```razor
@using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls

<IdentityControl User="currentUser" Items="accountCommands" OnSelect="SelectAccountCommandAsync" />
```

Here `currentUser` is a `System.Security.Claims.ClaimsPrincipal`, `accountCommands` is an `IBlazorApplicationMenuItem[]`, and `SelectAccountCommandAsync` accepts a menu item and returns `Task`. Build commands with `BlazorApplicationMenuItemBuilder`, and use `BlazorMenuSeparator` between groups. The callback can await `IUserInterfaceServices.ExecuteMenuItemAsync(item)` for existing application commands, or dispatch to the host's own account/profile services. Selection awaits the callback and suppresses duplicate clicks while it runs; failures propagate to the containing error boundary.

For an authenticated host, bind the principal already supplied by its authentication provider, for example inside an `AuthorizeView`:

```razor
@using Microsoft.AspNetCore.Components.Authorization

<AuthorizeView>
	<Authorized Context="authentication">
		<IdentityControl User="authentication.User" Items="accountCommands" OnSelect="SelectAccountCommandAsync" />
	</Authorized>
	<NotAuthorized>
		<IdentityControl Items="guestCommands" OnSelect="SelectAccountCommandAsync" />
	</NotAuthorized>
</AuthorizeView>
```

The host supplies its normal authentication-state configuration and provider-specific account actions. An authenticated principal uses its identity name, or **Signed-in user** if unnamed; an anonymous or omitted principal uses **Guest**. The name supplies the avatar button's accessible label and tooltip and appears in the dropdown header. `DisplayName` overrides that name. `SecondaryText` overrides the email claim shown below it. `AvatarUrl` fills the circle with a cropped image; otherwise the control shows the bundled Font Awesome person silhouette without requiring an icon-font stylesheet. These display values do not grant permissions or alter the authenticated principal.

`AvatarTemplate`, `IdentityTemplate`, `MenuHeader` and `MenuFooter` are `RenderFragment<ClaimsPrincipal>` slots receiving the current principal. `AvatarTemplate` customizes content inside the fixed circular trigger. `IdentityTemplate` customizes the dropdown identity details; `MenuHeader` replaces the complete header when supplied, and `MenuFooter` adds content after its commands. In derived controls, override `EffectiveDisplayName` / `EffectiveSecondaryText`, `OnItemHover` or `OnItemSelectedAsync` to customize behavior. The non-virtual selection wrapper retains async duplicate protection around the command hook. Escape and outside clicks close the menu and return focus to the trigger. Arrow keys and Home/End move among menu commands. Its collocated JavaScript suppresses native scrolling only for those handled navigation keys; Tab and native Enter/Space behavior remain available.

Place the control in `ApplicationShell.ToolBarContent` beside search and theme selection. Changing its principal or account menu does not replace retained application screens. The tester demonstrates a local sample principal and simulated sign-out; a production host should wire its existing authentication flow to these same inputs and callbacks.

## Running application, tabs and merged commands

By default, the bottom block dock browses navigation independently of the displayed screen. `SelectBlockAsync` changes `ActiveBlock` and its left-hand menus without activating a screen, invoking screen guards or adding browser history. `ActivateBlockAsync` explicitly opens a block's default screen. Choosing a menu screen or a tab goes through the existing guards and restores that screen's owning block in the navigation pane. The active tab, its toolbar and its menu contributions stay in place while another block is browsed.

Set `ApplicationShell.ActivateBlockOnSelection="true"` when selecting a dock button should enter that block immediately. The shell then calls the screen host's guarded `ActivateBlockAsync` through its normal interaction path: an accepted transition activates the default screen, refreshes command scope, closes the mobile navigation drawer and adds the corresponding browser-history entry. Action-only blocks can become active with no selected screen. If a leave guard rejects the transition, the current block, screen, menus, drawer and URL remain unchanged. The parameter defaults to false to preserve navigation browsing for existing consumers.

`AddSphere10Blazor` registers one `BlazorApplication` per circuit through both `IBlazorApplication : IApplication` and the shared `IApplication`. The runtime hierarchy is **Application → Blocks → Menus/screens**. `Blocks` projects the live screen host's canonical block snapshots. `LoadedPlugins` returns the original plugin instances whose startup registration completed successfully. One plugin can contribute several independently selectable blocks; each bottom-left navigation icon represents a block. Every collection property returns a membership array that callers can change without changing stored membership.

### Application lifecycle

`BlazorApplication` derives from `AsyncApplicationBase`, which implements asynchronous lifecycle operations and supplies synchronous adapters. The neutral `ApplicationBase` retains shared state and leaves initialization/finalization pairs abstract. `IApplication` provides paired `Initialize()` / `InitializeAsync()` and `Finalize()` / `FinalizeAsync()` methods with shared once-only lifecycle state. An interactive circuit awaits `InitializeAsync()` when it opens and `FinalizeAsync()` when it closes. Temporary disconnects and reconnects retain that application. Server prerendering can construct and render the scoped application without running its lifecycle tasks. The shell continues to initialize screen selection through `ScreenHost.InitializeAsync`; application task initialization does not select a screen or reload plugins.

Register plugin-owned application tasks with `BlazorPluginBuilder.RegisterInitializer<MyInitializer>()` and `RegisterFinalizer<MyFinalizer>()`. The helpers register transient tasks keyed by the built plugin's `Name`; loading rejects duplicate names, and custom plugin names must remain stable. Each circuit's application resolves and caches tasks for its successful `LoadedPlugins` through that circuit's service provider, giving applications their own task instances and scoped dependencies. Host registrations using `services.AddInitializer<MyInitializer>(typeof(IApplication))` and `services.AddFinalizer<MyFinalizer>(typeof(IApplication))` remain supported for application-owned tasks. The combined inventory starts with host-owned registrations in registration order, followed by plugins in `LoadedPlugins` order and each plugin's tasks in declaration order. Initializer priorities apply across this inventory within the existing sequential and parallelizable phases; finalizers execute in combined inventory order. The existing unkeyed registrations remain process tasks and are not run by a circuit. The application keeps its task collections protected; `Initializing`, `Initialized`, `Finalizing`, `Finalized` and `Progress` report their execution. `Logger` resolves the registered `Sphere10.Framework.ILogger`, falling back to `SystemLog.Logger`. Put resources owned by this application in `Disposables`; finalization releases them while the scoped host and DI services retain their existing owners. When managing a service scope yourself, await `application.FinalizeAsync()` before disposing that scope, then use asynchronous disposal for the scope and application. The circuit handler already finalizes before its scope is released. Application disposal supplies fallback cleanup for cached tasks and owned resources, but DI may have disposed task dependencies before reaching the application during scope disposal. Completed tasks run once.

Call `WithName(...)` before either lifecycle registration helper. Both type and factory overloads throw `InvalidOperationException` immediately when the plugin has no name.

Task contracts declare both synchronous and asynchronous methods. Derive synchronous tasks from `SyncApplicationInitializerBase` or `SyncApplicationFinalizerBase` and override `Initialize()` or `Finalize()`; their asynchronous calls use `Task.Run`. Derive asynchronous tasks from `AsyncApplicationInitializerBase` or `AsyncApplicationFinalizerBase` and override `InitializeAsync(...)` or `FinalizeAsync(...)`; their synchronous calls wait for the asynchronous operation. The neutral task bases leave both methods abstract, following the repository hierarchy.

For example, using the tester's existing task and status types, a plugin declares its name before supplying factories for its circuit-local dependencies:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.BlazorTester.Application;
using Sphere10.Framework.Utils.BlazorTester.Lifecycle;
using Sphere10.Framework.Web.AspNetCore.Blazor;

services
	.AddSphere10BlazorPlugin(plugin => plugin
		.WithName("Lifecycle example")
		.ConfigureServices(registry => registry.AddScoped<WorkspaceStatus>())
		.RegisterInitializer(provider => new DemoPluginInitializer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), "Lifecycle example"))
		.RegisterFinalizer(provider => new DemoPluginFinalizer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), "Lifecycle example"))
	);
```

This is a service-only plugin; add blocks through the same builder when it also contributes UI. The [running tester](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md#plugin-initialization-and-shutdown) registers four plugins, each with one initializer and one finalizer. Their tasks log through the application logger and record startup/shutdown in circuit-local state. Screens can reach that same application through `Block.Parent`.

### Exit and shutdown

The paired `Exit(...)` and `ExitAsync(...)` methods return whether exit completed. Blazor interactions await `application.ExitAsync(cancellationToken: token)` to confirm exit and check the host's retained screen guards, the active screen's block and the browsed block. It returns false when confirmation or a guard declines. Application subclasses can override the confirmation, permission and completion hooks. Override `BlazorApplicationBlock.CanExitAsync(IApplication application, CancellationToken cancellationToken)` for block permission; snapshots preserve this policy and pass the current circuit's application, so use that argument for runtime context rather than the shared prototype's `Parent`. `force: true` bypasses confirmation and guards, and dismisses a pending exit confirmation. `IsExiting` reports the pending request; `HasExited` reports successful completion.

The shell keeps its screens mounted during confirmation. Successful exit awaits local task finalization and owned resource cleanup, removes screen components through their renderer, and displays terminal content. Once approved cleanup starts, cancellation stops only the caller's wait; cleanup continues and other callers can await the completed exit. Cleanup errors are logged and the application still reaches its terminal state. The borrowed host, shared plugins and web server keep their existing lifetimes. The shell accepts navigation away after exit and rejects further workspace commands or route activation; it does not redirect or terminate the circuit. The tester's File → Exit application command demonstrates this path. Legacy `IUserInterfaceServices.Exit` delegates to the same scoped application and observes failures; await `IApplication.ExitAsync` when the result matters.

### Startup contributions and global commands

Standalone `AddApplicationBlock(...)` registrations appear directly in `Application.Blocks`. An application containing only standalone blocks has an empty `LoadedPlugins` array. Temporarily unregistering/re-registering any block updates the live application block list while successful startup metadata remains available. The Blazor screen host creates immutable block snapshots and maps startup contributions by block ID; the application's block references are the host's canonical snapshots rather than the plugin's source definitions.

Plugin `Load(IServiceCollection)` runs during registration, before the service provider is built. Successful loading, post-load validation and block registration publish an `ApplicationPluginRegistration<IBlazorPlugin>` containing the original plugin reference and captured block contributions. The application factories and startup screen ordering read these receipts, so ordinary DI registration alone does not add an unloaded plugin to `LoadedPlugins`. Service-only plugins appear in `LoadedPlugins` even though they contribute no blocks. Registration rejects both duplicate successful plugin names and recursive attempts to load the same name. Failed registration publishes no receipt and discards newly added lifecycle task descriptors under the attempted name, preventing a later successful plugin with that name from inheriting abandoned tasks. Ordinary `ConfigureServices` side effects retain their existing behavior, and a `Loaded` event may have fired before final validation fails. The shared and Blazor DI aliases resolve the same successfully registered plugin instance. Resolving or disposing a circuit's application does not load/unload singleton plugins or dispose their blocks. `IoCContainer` remains null for compatibility; command delegates resolve scoped dependencies from the executing circuit. Selection and interactions belong to `ScreenHost`; the application exposes live blocks, successful startup metadata and commands. Use the application builder when configuring application-level commands:

```csharp
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor;

services
	.BuildBlazorApplication()
	.ConfigureApplication(application => {
		var closeAll = new BlazorApplicationMenuItemBuilder()
			.WithId("close-all")
			.WithText("Close all screens")
			.WithIcon("fas fa-times")
			.WithAction(async (provider, token) => {
				var host = provider.GetRequiredService<IMultiApplicationScreenHost<IBlazorApplicationBlock, BlazorApplicationScreenSession>>();
				await host.CloseScreensAsync(host.Screens.Where(screen => !screen.IsPermanent && screen.ScreenKind == ScreenKind.Normal), token);
			})
			.Build();
		application.SetMenus(new[] {
			new BlazorApplicationMenuBuilder()
				.WithId("work")
				.WithText("Work")
				.AddItem(closeAll)
				.Build()
		});
		application.SetToolBarItems(new[] { closeAll });
	})
	.Build();
```

Configuration runs once per scope. The callback configures that scope's application; command callbacks resolve their services from the executing circuit. `SetMenus` and `SetToolBarItems` snapshot ordinary action definitions, freezing their registration metadata. Block-owned commands borrowed for application presentation retain their canonical references and parents; screen commands must already belong to the circuit's block graph. A later `AddSphere10BlazorApplication` call replaces the earlier application configuration. The builder delegates to that same API. Its inherited `ConfigureApplication` calls compose before `Build()`; the shared `ApplicationBuilderBase` also owns title/plugin composition and the terminal build guard. Blazor retains favicon settings and the actual DI registration. The tester uses the builder with [WorkspaceCommands.Configure](../../utils/Sphere10.Framework.Utils.BlazorTester/Application/WorkspaceCommands.cs) and its existing fluent plugin definitions.

`ApplicationShell` composes command layers from the application, the active screen's owning block and the active screen using `Tools.UI.GetCommandLayers`, `MergeApplicationMenus` and `MergeApplicationToolBarItems`. When no screen is selected, the contextual layer comes from the navigation block. Application commands remain available even when a contextual command has the same local ID. A screen can replace a matching block command and extend File while keeping application commands. Register block toolbar commands with `BlazorApplicationBlockBuilder.AddToolBarItem`, or build them with `ConfigureToolBarItem`; override the typed arrays on `BlazorApplicationScreen` for screen contributions. `BlazorMenuSeparator` supplies boundaries; composition removes redundant separators and keeps Help last. Toolbar screen commands may reference any definition owned by their block.

Keep commands shared by every workspace at application level. A block command belongs to the active screen's owning block; a screen command disappears when that screen is deactivated. Browsing another sidebar does not change the displayed screen's command context. Within the contextual layer, matching uses local IDs and a narrower action group replaces the matching block group as one command. Application groups remain separate. `ApplicationCommandBar.CommandKeySelector` lets independent layouts namespace renderer identities when different levels share an ID; the shell supplies this automatically without changing command registrations. Hidden items remain registered but are omitted from rendered controls, and menus containing only hidden items are omitted.

Tabs retain each renderer-created component and its state. `ApplicationScreenTabs` provides selection, close buttons, middle-click closure, context commands for closing one/other/all screens, drag reordering, and keyboard interaction. Arrow keys, Home and End select; Ctrl+Shift with those keys reorders; Delete closes; Shift+F10 opens tab commands. Override `Title` and call `NotifyScreenChanged()` to update a tab's caption. Long titles are clipped with an ellipsis and retain a full tooltip; dirty screens display an indicator.

The host defaults to `ScreenMode.MultiView`. The tester's toolbar toggle and View menu await `TrySetScreenModeAsync`; `ApplicationShell.ShowScreenModeSelector` provides an optional header selector. Entering `SingleView` preflights every ordinary tab that would close before removing any; permanent sessions are retained. The synchronous `ScreenMode` setter accepts transitions requiring no closures and rejects transitions requiring async guards; use the awaited method for runtime changes. During single-view navigation, hidden single-instance screens remain mounted and multi-instance screens are released. The multi-screen contract's `Screens` includes retained hidden sessions and `OpenScreens` supplies normal tab membership in presentation order; empty screens are excluded. `MoveScreenAsync` changes that order. `CloseScreensAsync` preflights a batch and preserves every screen if a guard rejects it. Components remain owned by the renderer. Docking is not part of the Blazor host.

Custom Sidebar content can capture its ApplicationShell with @ref and await shell.ExecuteCommandAsync(item). This follows the same guarded command path as the native command bar, closes the navigation drawer after success and synchronizes browser history. Do not bypass the shell by calling its screen host directly from custom navigation controls.

## Command presentation and availability

BlazorApplicationMenuItemBuilder.WithVisibility(false) hides a command from the sidebar, top menus and toolbar while retaining its registered identity. A hidden multi-instance screen can still be opened through ActivateScreenAsync with runtime parameters and an instance key, or through a deep link. Visibility is presentation metadata, not authorization.

EnabledWhen(Func<bool>) evaluates current command availability in both the rendered controls and `ExecuteMenuItemAsync(item, cancellationToken)`. Return the screen's dirty state to make Save available only while changes exist, and call NotifyScreenChanged after that state changes. Predicates on screen-owned commands may refer to that screen; shared registrations must not capture a circuit or user. Explicit programmatic screen activation remains independent of command availability.

WithChildren(routeCommand, segmentCommand, summaryCommand) creates a BlazorApplicationMenuGroup. Return the group from a screen's ToolBarItems to show one Export dropdown containing its action commands. The native command bar retains command execution coalescing, disabled child controls, outside-click dismissal, Escape and focus restoration. Groups inside dropdowns are expandable nested lists with labelled buttons and aria-expanded state. Click or Enter/Space expands a group; ArrowRight enters its first enabled command. Escape or ArrowLeft closes the innermost group and restores its opener; another Escape closes the top menu. Tab follows the visible native buttons. Long lists scroll inside the dropdown, which fits small viewports. Supply a disabled descriptive child such as No active sites for an empty application group. Replacing the menu data prunes unavailable expanded groups. Register screens as top-level block menu items rather than inside action groups; keep child command IDs unique.

## Default, permanent and empty screens

`ScreenActivationMode` and `ScreenKind` come from `Sphere10.Framework.Application.UI`. Lifetime and presentation role are independent.

Screen registrations default to `MultiInstance`: each menu activation opens an independent session. Use `AsSingleInstance()` or pass `ScreenActivationMode.SingleInstance` explicitly for settings and other screens that should retain one instance. This applies to direct menu items, `BlazorScreenMenuItem.For`, both menu builders and default-only screen registrations.

| Configuration | Behavior |
| --- | --- |
| `SingleInstance` + `Normal` | Reuses one retained session per component type. |
| `MultiInstance` + `Normal` | Creates a new tab/session on each activation. |
| `PermanentSingleton` + `Normal` | Opens automatically at initialization and remains open across layout changes. |
| `SingleInstance` + `Empty` | Displays without a tab when no normal tabs remain, retaining its hidden component between visits. |
| `MultiInstance` + `Empty` | Displays without a tab when no normal tabs remain; leaving it releases the component, and the next empty workspace creates a new instance. |

`Empty` with `PermanentSingleton` is invalid: a permanent normal screen means the workspace is never empty. A placeholder cannot replace an existing normal tab. Existing screen guards still apply when entering or leaving an empty screen.

For a menu screen, configure the lifetime, kind and startup marker through the same item builder:

```csharp
menu
	.ConfigureItem(item => item
		.WithId("home")
		.WithText("Home")
		.WithScreen<HomeScreen>()
		.AsSingleInstance()
		.AsDefault()
	);

menu
	.ConfigureItem(item => item
		.WithId("monitor")
		.WithText("Monitor")
		.WithScreen<MonitorScreen>()
		.AsPermanentSingleton()
	);

menu
	.ConfigureItem(item => item
		.WithId("empty")
		.WithText("Empty workspace")
		.WithScreen<EmptyWorkspaceScreen>()
		.WithScreenKind(ScreenKind.Empty)
		.AsSingleInstance()
	);
```

The component types in these fragments are your own classes deriving from `BlazorApplicationScreen`. A screen need not appear in explorer menus. For example, a dedicated block can provide only the empty-workspace default:

```csharp
plugin
	.ConfigureBlock(block => block
		.WithId("empty")
		.WithName("Empty workspace")
		.WithDefaultScreen<EmptyWorkspaceScreen>("Nothing open", ScreenActivationMode.SingleInstance, ScreenKind.Empty)
	);
```

`WithDefaultScreen<TScreen>(title, activationMode, screenKind)` marks that block's default. When the same type already has a menu item in the block, its lifetime and kind are inherited unless explicitly supplied; conflicting settings are rejected. A default without a matching menu and without an explicit lifetime uses `MultiInstance`, just like an ordinary screen registration. `AsDefault()` marks a menu definition without needing `WithDefaultScreen`.

`ScreenHost.InitializeAsync` opens all permanent sessions once and chooses the first marked default in plugin registration order, then each plugin's block array order, then menu order. Directly registered blocks follow plugin-owned blocks. If no definition is marked, the first block's legacy default remains the fallback. This startup order is independent of `Position`, which still controls the visible block list. An explicit bookmark is selected before host initialization, so opening a bookmark does not add an unrelated startup-default tab. `ApplicationShell` initializes the host; custom shells should await `ScreenHost.InitializeAsync` themselves. This is separate from the circuit's application task initialization. Later block registration opens its permanent screens without rerunning startup default selection or replacing an active normal screen.

Permanent tabs have no close button; middle-click, Delete and tab context close commands preserve them. A direct `CloseScreenAsync`/`CloseScreensAsync` request containing a permanent session returns false without removing any session. Application-level Close all commands should select only closable normal sessions:

```csharp
await host.CloseScreensAsync(host.Screens
	.Where(screen => !screen.IsPermanent && screen.ScreenKind == ScreenKind.Normal), cancellationToken);
```

Administrative `UnregisterBlockAsync` may remove the block and its permanent screens after preflighting their guards. Re-registering it creates its permanent sessions again. Navigating away from the application still checks every attached screen, including permanent and hidden empty screens. The renderer owns component disposal throughout.

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

`Views.DialogAsync(title, message)` displays an information dialog. `Views.WizardDialogAsync(wizard)` displays a wizard built with `Wizard.BlazorWizardBuilder<TModel>`. See the reusable [wizard demo](../../utils/Sphere10.Framework.Utils.BlazorTester/Demos/WizardsDemo.razor) for steps and finish callbacks, and the [table demo](../../utils/Sphere10.Framework.Utils.BlazorTester/Demos/TablesDemo.razor) for paged, virtual-paged and streaming examples.


Both wizard generations adapt `Application.UI.IWizard<TModel, Type>` and reuse the shared workflow and fluent wizard builder. Common title/model/cancellation, step composition, reset and callback methods live in `Sphere10.Framework.Application`; Blazor supplies component constraints and factories. Component types, options, validation and modal rendering stay in Blazor. Types use the `Blazor` prefix, including `IBlazorWizard`, `IBlazorWizardBuilder`, `BlazorWizard`, `BlazorWizardBuilder`, `BlazorWizardHost` and step types; `WizardStepUpdateType` comes directly from `Sphere10.Framework.Application.UI`.

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

`UI.Controls.BlazorGrid.BlazorGrid<TItem>` uses `IDataSource<TItem>` asynchronous CRUD. For a data source that updates a project draft, set SaveButtonText="Apply row" and EditNotice="Apply updates the draft." to distinguish row application from project persistence. Defaults preserve the existing Save button and notice. Component parameters are auto-properties synchronized through lifecycle methods; virtual paging rejects stale responses and streaming tables cancel/restart enumeration when the source changes. Wizards validate the final step and complete once.

The former modern `ApplicationBlock`, `MenuBuilder`, `MenuItemBuilder`, `PluginBuilder` and `ApplicationScreen` types are now `BlazorApplicationBlock`, `BlazorApplicationMenuBuilder`, `BlazorApplicationMenuItemBuilder`, `BlazorPluginBuilder` and `BlazorApplicationScreen`. Their platform interfaces, session and host follow the same prefix; typed contracts and navigation coordination live in the shared Application library. Implement shared lifecycle members through `Application.UI.IApplicationScreen` when using explicit interface implementations; `IBlazorApplicationScreen` is the sole Blazor screen contract. The former routed `App`, `AppBlock` and `AppBlockPage` types use the distinct `BlazorRoutedApplication*` names above.

Replace old `Sphere10.Framework.DApp.Presentation*` namespaces with this library's namespace. Inject scoped modal/view services instead of using static services. `BlazorApplication` is the scoped runtime projection of the screen host's live blocks and successfully loaded startup plugins. Resolve `IBlazorApplication` or the shared `IApplication` to obtain that same running instance. Read `Blocks` for runtime navigation and `LoadedPlugins` for startup metadata; the former runtime `Plugins` and `ActivePlugin` properties have been removed. Replace the former `Initialize(IServiceCollection)`/`AttachScreenHost` lifecycle with startup registration through `AddSphere10BlazorPlugin`; plugin configuration runs once at startup rather than once per circuit. The direct application constructor accepts an already-loaded plugin set and does not perform loading.

## Build, test and package

Run these commands from the repository root:

```shell
dotnet build src/Sphere10.Framework.Web.AspNetCore.Blazor/Sphere10.Framework.Web.AspNetCore.Blazor.csproj -c Release -p:BuildRevision=0
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0
```

The [test project README](../../tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/README.md) lists the grid fixtures and focused test commands. The [Blazor tester guide](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md) explains the full-window application demo. `/` and `/application` enter the same shell; component and legacy URLs select registered screens and resolve to the canonical `/application?block=...&screen=...` address. The old `/components/grid` and `/modern` URLs both open the CRUD grid tab. Theme, endpoint selection, commands and retained screen state belong to the same running host.

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

### Conditional wizard screens

The modern wizard host observes the shared wizard's Changed event, so a step can insert a temporary screen with UpdateSteps and navigate to it through Next. Both Next and Back invoke the current step's validation callback. If that callback explicitly navigates the wizard (for example an Import step applying its preview and returning to the route screen with Previous), the host renders that destination without advancing a second time. Pending Back validation blocks competing navigation and cancellation; failed validation preserves the current screen.
