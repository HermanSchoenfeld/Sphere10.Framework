<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Web.AspNetCore.Blazor

Razor components for .NET 10 Blazor Web Apps using Interactive Server rendering. The library brings Sphere10's ApplicationBlock navigation and `IDataSource<T>` CRUD patterns to the browser.

## Features

- **CRUD grid:** typed columns, buffered editing, validation, confirmed deletion, search, sorting, paging and visible-row selection.
- **Custom presentation:** display/editor templates, existing-record reference pickers, async row commands, resizable columns and optional automatic page sizing.
- **Hierarchical rows:** source-owned child views, recursive expansion, independent sibling paging and custom detail templates for hierarchical or ordinary rows.
- **Power-user screens:** icon-only toolbars, a standard File menu that screens extend, command state that follows each screen render, compact grids with icon row commands, a header reload icon and bound selection, a draggable `SplitContainer` and `FieldGrid` field layouts (label above control, or inline).
- **Application workspace:** blocks, menus, retained screens, multiple-instance sessions and navigation guards built on the shared [Application.UI](../Sphere10.Framework.Application/README.md#shared-ui-framework) layer.
- **Additional components:** paged/streaming tables, dialogs, wizards and plugin integration.
- **Theme switching:** 20 scoped presets, including Classic blue and Blue plus styles adapted from owned templates, with one stylesheet per template and shared accessible controls.
- **Host integration:** scoped services, renderer-owned component lifetimes and isolated CSS/JavaScript assets.
- **Application lifecycle:** circuit-local startup/shutdown tasks, application logging and owned resources, and confirmed exit coordinated with screen and block guards.

Start with [hosting](#current-blazor-hosting-and-assets), then the [complete grid example](#crud-grid-quick-start). See [custom editors and references](#custom-editors-reference-pickers-and-actions) for a second runnable page, or [ApplicationBlock architecture](#applicationblock-architecture) for a navigation workspace.

## Installation

Requires the .NET 10 SDK and a Blazor Web App host. The package targets `net10.0`, plus `net11.0` when a .NET 11 SDK builds it, because it builds on the .NET 10 ASP.NET Core static web asset support; the `Web`, `Web.AspNetCore` and `Web.AspNetCore.MVC` packages also target `net8.0` and `net9.0`. This package references the shared ASP.NET Core layer and `Microsoft.AspNetCore.App`; it is not a standalone WebAssembly package. See the [project dependency guide](../Sphere10.Framework.Web.AspNetCore/README.md) for the other web layers.

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

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Web.AspNetCore.Blazor` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [Application](Application/README.md) | The Blazor application workspace model of blocks, menus, screen registrations, the circuit-scoped screen host and the per-circuit application aggregate. | [source](Application/) |
| [Components](Components/README.md) | The original view-model component generation with route search, a loading overlay, modal dialogs, display tables and a wizard host. | [source](Components/) |
| [Events](Events/README.md) | A legacy event-arguments type carried over from the original Blazor presentation code. | [source](Events/) |
| [Grids](Grids/README.md) | The controller, column model, settings and value conversions behind the `BlazorGrid<TItem>` CRUD grid. | [source](Grids/) |
| [Modal](Modal/README.md) | The current dialog services that show modal components, confirmations, information messages and wizards through `UI.Dialogs.ModalHost`. | [source](Modal/) |
| [Models](Models/README.md) | Small records and classes used by the original component generation for search results and paged table requests. | [source](Models/) |
| [Plugins](Plugins/README.md) | Blazor plugin definitions and builders for the application workspace. | [source](Plugins/) |
| [Services](Services/README.md) | Services of the original component generation for modal dialogs, wizard building, typed event publishing and example endpoint and node data. | [source](Services/) |
| [Theming](Theming/README.md) | The scoped theme service and the catalogue of built-in theme presets with their stylesheet paths and dark or light classification. | [source](Theming/) |
| [UI](UI/README.md) | The Razor components of the current generation, from the application shell and CRUD grid to dialogs, tables, layout pieces, theme controls and the wizard host. | [source](UI/) |
| [ViewModels](ViewModels/README.md) | Base view models for the original component generation, including the modal view model that completes a dialog's result. | [source](ViewModels/) |
| [Wizard](Wizard/README.md) | The current Blazor wizard model and fluent builder whose steps are Blazor component types rendered by `UI.Wizard.BlazorWizardHost`. | [source](Wizard/) |

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`IServiceCollectionExtensions`](IServiceCollectionExtensions.cs) | static class | `BuildBlazorApplication`, `AddSphere10Blazor`, `AddSphere10BlazorApplication`, `AddSphere10BlazorPlugin`, `AddApplicationBlock` and `AddViewModelsFromAssembly` register the Blazor application, plugins, blocks, screen host, dialog, wizard, theme and settings services. |
<!-- domains:end -->

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
	.AddPlugin(plugin => plugin
		.WithName("Components")
	)
	.Build();
```

`WithFavicon` accepts a URL and an optional content type. Omit it to leave the icon unconfigured. `AddPlugin(plugin)` registers an existing plugin definition, while `AddPlugin(plugin => ...)` configures and builds a nested `BlazorPluginBuilder`; `OnApplicationCreated` composes callbacks that run once for each circuit's application. Branding is an immutable singleton `ApplicationOptions`, so the document head can resolve it without creating a scoped screen host. `AddSphere10Blazor()` supplies default options and preserves previously configured branding.

In `Components/App.razor`, add these directives and replace the template's title/favicon elements inside `<head>` with the configured values. `Assets` maps local icon paths to their fingerprinted static assets:

```razor
@using Sphere10.Framework.Web.AspNetCore.Blazor
@inject ApplicationOptions ApplicationOptions

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

### Icons

The library ships no icon font. Command `Icon` values, `GridAction.Icon` and the grid's `OpenCommandIcon`, `EditCommandIcon` and `DeleteCommandIcon` are CSS classes that the host's icon set defines, rendered as `<span class="...">`. The grid's built-in reload and row-command icons, the pager and the identity avatar are inline SVG and need no font. Copy an icon set's stylesheet and font files into the host's `wwwroot` and load the stylesheet in `Components/App.razor`. The tester uses Font Awesome Free 5.15 from `wwwroot/modern/css/icons.css` and `wwwroot/modern/fonts`:

```razor
<link rel="stylesheet" href="@Assets["modern/css/icons.css"]" />
```

Its classes are then used directly, for example `.WithIcon("fas fa-save")` or `new GridAction<Person>("Delete", null, null) { Icon = "fas fa-trash-alt" }`. Keep the icon set's licence notice with its files; Font Awesome Free's fonts are SIL OFL 1.1, its icons CC BY 4.0 and its CSS MIT.

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

`ThemeProvider` changes the stylesheet and `data-sphere10-theme`/`data-bs-theme` attributes on a stable wrapper. It supports fingerprinted static assets through Blazor's `Assets` collection. `ThemeMode.ClassicBlue` retains the white header, pale content background, blue sidebar gradient and original font stack; `ThemeMode.Blue` retains the newer blue header and sidebar. `IThemeService.Toggle()` switches any dark preset to Light and any light preset to Dark. Call `IThemeService.SetTheme(...)` for a specific preset. Selection lasts for the service scope; persistence across reloads belongs to the host's preference service.

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
| `Width` / `ExpandsToFit` | Initial/minimum pixel width and whether the column shares spare viewport space. A grid always spans its viewport: when no column is marked, the widest configured column takes the spare width. |
| `Hidden` | Omits the column from the rendered table by default. An accepted column preference can show it, or hide a column that is visible by default. |
| `ImageSource` / `ImageAlternativeText` | Optional image URL and accessible alternative text; ordinary display templates remain available. |
| `ImageWidth` / `ImageHeight` | Image presentation box in CSS pixels (24 × 24 by default). |
| `ImageAlignment` / `ImageResizeMethod` | Shared `ResizeAlignment` (default `CenterCenter`) and optional `ResizeMethod`; null retains original-size presentation. |

Explicit `Columns` replace automatic generation. Without them, `AutoGenerateColumns` defaults to true and reflects readable public instance properties, excluding indexers and `[Browsable(false)]` members. Generated definitions honor `[DisplayName]`, `[ReadOnly]`, private setters and init-only properties. Complex values remain read-only unless you configure an editor.

A hidden column stays in the declared schema and in captured preferences, so Options lists it with its **Hidden** choice and its width. Changing which columns are displayed replaces the rendered column set, which cancels an active row draft in the same way as replacing column instances. A nested detail grid receives its parent's displayed columns with `ShowHiddenColumns="true"`, so that it shows every column it is given.

Create column definitions once, for example in a field or `OnInitialized`. Replacing column instances during an edit cancels that draft and starts a new column-layout measurement. `PropertyHasValue = false` makes a value unavailable for editing; do not use it merely because an editable reference is currently null.

`AutoSizeColumns` defaults to true. The first nonempty rendered result establishes natural widths, including header text and reserved sort-indicator space; an empty result measures the headers alone, provisionally, so a header never breaks mid-word, and the first nonempty result measures again. Cell text wraps between words and breaks a word only when it alone is wider than its cell. Paging, filtering, sorting and refresh retain those widths; a longer value on a later page does not change the table layout. Header-edge dragging establishes a manual width that takes precedence over automatic stretching. Columns marked `ExpandsToFit` share spare viewport width (when none is marked, the widest configured column does), and a manually resized stretch column keeps its chosen width. Narrow viewports scroll horizontally rather than collapsing those minimums. Set `AutoSizeColumns="false"` to start from configured `Width` values without measuring cell content.

`Caption` is the visible table title and accessible caption. `CaptionAlignment` uses `BlazorGridHorizontalAlignment.Left`, `Center` or `Right`, with Left as the default. It does not reserve space for absent toolbar buttons. An empty or null `Caption` renders no caption element at all.

Image columns use the same portable `Sphere10.Framework.ResizeAlignment` and `ResizeMethod` enums as Drawing. The browser maps those values to image-box fit and position; it does not load `System.Drawing`, resample server-side bitmaps or assume ownership of the URL's underlying resource. Use `ImageResizeMethod = ResizeMethod.AspectFit` for proportional fitting, `AspectFill` for cropping or `Stretch` to fill the box. `AspectFitPadded` fits within the presentation box. Supply trusted application image URLs and meaningful alternative text when the image conveys information.

Default editors support strings/chars, booleans (including nullable tri-state values), numeric types, enums, GUIDs, `DateOnly`, `TimeOnly`, `DateTime`, `DateTimeOffset` and `TimeSpan`. Dates/times use the appropriate HTML inputs; offset timestamps and durations use text to preserve their full values. Built-in editor conversions use invariant culture, while default cell display uses the current culture. An enum cell shows `GetDisplayText()`, the member's `[Description]` or its name as Title Case words ("PaymentFailed" reads "Payment Failed"), unless its column sets a `Format` such as `"D"`; the enum editor's options show the same text while their values stay the member names. `Tools.BlazorGrid` provides the underlying conversion helpers.

### Selection, editing and validation

- Click a row to select it. With `LeftClickToDeselect = true` (default), clicking it again clears selection. Only current-page items can be selected.
- Choose **Edit**, double-click a row or press **F2** on a focused row. `AllowCellEditing` controls the double-click/F2 gestures; the Edit command remains available when updates are permitted.
- Editing buffers values through `BlazorGridCellEditContext<T>.ValueChanged`. Built-in inputs convert before buffering. Cancel or **Escape** discards a draft without persisting it.
- Save applies changed values, calls `ValidateAsync`, then calls `CreateAsync` or `UpdateAsync`. Rejected validation or failed persistence restores changed fields and keeps the draft for correction/retry.
- Setters must support restoring the original value. Custom editors must publish replacements through `ValueChanged`, rather than mutating `Item` or a referenced object directly. This rollback is not a transaction for external side effects inside a setter or source.
- Delete validates `CrudAction.Delete` before calling `DeleteAsync`. Built-in mutation events fire after successful persistence. A later refresh can fail independently; inspect `ErrorMessage` when handling such a result.
- Paging, search and other grid commands are disabled during editing or a foreground operation. An explicitly suppressed background refresh retains usable rows and navigation until its replacement is ready. Replacing the source, permission mask, page-size parameter or column definitions discards a pending draft.

`SelectedItemChanged`, `ItemCreated`, `ItemUpdated` and `ItemDeleted` are `EventCallback<TItem>` parameters. Selection clearing passes the default value, normally null for entity classes. Async handlers should return `Task`.

`SelectedItem` reads the current selection and is also a parameter, so `@bind-SelectedItem="_selected"` drives a detail panel from the grid and the grid from the screen. A supplied value is applied when it changes: null clears the selection, and an item is selected once the displayed page holds it and no row edit is open, so assigning a just-created record after `RefreshAsync()` selects it. An unchanged value never takes back a selection the user made later. `ItemOpened` (`EventCallback<TItem>`) opens a row: with it, double-click and Enter select and open the row instead of starting an inline edit, and F2 still edits. `OpenItemAsync(item)` does the same from code.

Row events (click, keys, Open, Edit, Delete and custom actions) carry the item their row was rendered with, and a read or source notification can replace the rows before the event is handled. The grid therefore finds the item among the displayed rows by the controller's `EntityComparer` and acts on that row, and ignores the event when the item is no longer displayed, instead of throwing out of the handler and ending the circuit. With the default comparer a row replaced by a new instance no longer matches, so set `Controller.EntityComparer` (for example `new ProjectionEqualityComparer<OrderDTO, int>(order => order.ID)`) when a click made during a replacement should still open its record.

### Paging and external refresh

Sources advertising `DataSourceCapabilities.CanNotify` automatically update a
bound grid through `IDataSource<T>.Changed`. The controller subscribes once,
captures its owning renderer synchronization context during binding, and
coalesces worker notifications into a silent current-query read. Notification
support is checked independently of the allowed CRUD mask. Data collection stays
inside the source; notification refresh never invokes its acquisition/reload API.

The current page, filter, sort, stable selected identity and measured column widths
are retained. A source change immediately invalidates an older pending read.
Notifications received during a buffered edit or save wait until commit/cancel;
they do not overwrite the draft. A successful write's normal page read also
satisfies its source notification. Replacing or disposing a binding removes the
handler without disposing the borrowed source. Non-notifying sources retain the
existing explicit refresh API below.

`PageMode` selects `BlazorGridPageMode.Paged`, `Auto` or `Single`; the footer presents **Paged**, **Auto Paged** and **Single Paged**. Paged uses the remembered manual size. Auto measures the viewport, headers, row heights and scrollbar space to fit complete rows while keeping navigation. Single asks for page zero and `int.MaxValue` page length, showing every matching row with vertical scrolling and no navigator. Sources without `CanPage` retain complete-result behavior and hide the paging controls. Search and sorting apply in every mode. `AutoPageSize` remains a compatibility alias: true selects Auto; false selects Paged.

`PageSize` defaults to 10 and accepts 1–9999. Paged uses accessible First/Previous/Next/Last icon buttons with tooltips and a **Rows per page** dropdown. `PageSizeOptions="new[] { 5, 10, 25, 50, 100 }"` customizes the choices; those are also the defaults. The active size is included automatically even when it is not listed (for example, `PageSize="6"`). Auto and Single preserve the manual size for returning to Paged; they hide its editor. Paging and page-size changes remain disabled during editing/foreground loading. `CurrentPage` and `SetPageAsync` are **zero-based**; the visible pager starts at one. The source returns the actual zero-based `DataSourceItems.Page` and filtered `TotalCount`, not the count on that page. Filter and sort before slicing. A source may collect its full inventory once and still implement paging over that published snapshot; changing pages must not recollect it.

Sortable headers cycle **Ascending → Descending → None**, then ascending again. None requests the source's natural order with `SortDirection.None`; sources must implement that value rather than treating it as Ascending. Changing to another column starts ascending. Sorting retains established column widths.

Add `@ref="_grid"` to a grid to call its public component methods from the owning renderer context:

```csharp
private BlazorGrid<Person> _grid;

private async Task RefreshPeopleAsync() {
	if (_grid.Controller.IsEditing || _grid.Controller.IsSaving)
		return;
	await _grid.RefreshAsync();
}
```

This fragment belongs in the page's `@code` block and requires `@using System.Threading.Tasks`. The component also exposes `SetPageAsync(int)`, `SetPageSizeAsync(int)`, `SetPageModeAsync(mode)`, `BeginCreate()`, `BeginEdit(item)`, `CancelEdit()` and `SaveAsync()`; Save returns a boolean.

For a grid sized to its viewport:

```razor
<BlazorGrid TItem="Person" DataSource="_source" Columns="_columns"
	PageMode="BlazorGridPageMode.Auto" Height="420" />
```

Automatic sizing measures the header and rows within the fixed-height viewport, hides the manual page-size input and uses a conservative row height to avoid repeated changes in page capacity. Browser measurements use CSS pixels, including fractional zoom and device scaling. Resizing requests another page only when capacity changes; it does not start an independent refresh loop.

For periodic or event-driven snapshots, set `SuppressLoadingIndicator="true"`, or call `RefreshAsync(showLoadingIndicator: false)` for an individual refresh. The initial binding retains the full-table loading surface; later suppressed reads retain the previous rows, selection and usable query controls while the source is awaited. `Controller.IsRefreshing` tracks all refresh work, while `IsLoading` identifies foreground loading. Reads still share serialization/coalescing and reject stale results after a newer query, source replacement or disposal. A plain `RefreshAsync()` follows the component's suppression setting. Source work must use its native asynchronous API or the framework's synchronous-source worker adapter; wrapping a blocking query in an async method alone does not move its synchronous prefix off the renderer.

For a row action, await `RefreshRowsAsync(rows)`, `Controller.RefreshEntityAsync(entity)` or `Controller.RefreshSelectedEntityAsync()` rather than rereading the entire page. The source's `RefreshRangeAsync` returns fresh objects through its supplied array; null removes a row that disappeared. Core's `ListDataSource<T>` leaves provide this behavior through their `RefreshMethod` hook over the served list. An `ArrayDataSource<T>` does not implement row refresh. Set `Controller.EntityComparer` to stable identity when replacement objects should retain selection. `RemoveRows(rows)` removes caller-known rows from the displayed collection; update the caller-owned source too so its next read does not restore them. Row updates retain the query, unaffected rows and established widths.

`await grid.ReadFilteredEntitiesAsync(cancellationToken)` captures all matching rows across pages in the current sort order. It asks the bound source for page zero and `int.MaxValue`, without recollecting a snapshot, changing the visible page/filter/selection or starting a loading indicator. The typed array is suitable for the caller's CSV/JSON/export policy; reporting and download UI remain with the application. Cancellation is checked before queued work and after the source read completes; it does not interrupt an in-flight `IDataSource<T>` read, which has no cancellation-token contract.

Opt into standalone preferences with `PersistSettings="true"` and a stable `SettingsKey`, distinct for independent tables. The grid awaits `UserSettings.HasAsync<BlazorGridSettings>` / `GetAsync<BlazorGridSettings>` and saves the actual settings object with `SaveAsync()`. Framework circuit initialization binds the specialized browser provider; controls do not assign a global provider or write browser storage themselves. Paging mode, manual page size, page number, sort state and explicit widths are retained. Search text is not saved. Auto viewport measurements do not overwrite the remembered manual size or continuously save during resize. Existing untyped grid keys are a read-only fallback when no typed record exists. Persistence is optional, so isolated or short-lived grids can leave it disabled.

For a hosted screen, pass a non-null `BlazorGridSettings` through `Preferences` and await `PreferencesChanged` in the owning screen. This selects the screen's named settings role and disables the grid's separate browser record. `CaptureGridPreferences()` returns detached values; `ApplyGridPreferencesAsync(values)` restores them without saving or replacing an active row draft. Accepted query changes wait until that draft is saved or cancelled. The owning screen updates its settings root and uses its actual SettingsObject's save lifecycle, so Options, direct controls and shutdown share one authority. The tester's gallery demonstrates independent `Grid` and `LiveGrid` roles and a separate standalone grid.

Each persistent column needs a unique `PreferenceKey`, falling back to its `PropertyName` or `SortName`. Computed columns supply an explicit key; captions and positions are not persistent identities. Only a completed header drag records a manual CSS pixel width; natural and stretch widths stay unspecified. Reset removes overrides and returns to the configured natural/stretch layout. Page, sort and width values restore before the first data query and first browser layout. `ReadLegacyPreferencesAsync(runtime, key, columns)` reads an old standalone value without writing it; the screen consults it only when its typed settings record is absent. An existing typed record remains authoritative, including after Reset. A completed drag/page/sort change is an accepted save boundary, so browser reload does not depend on a teardown callback.


See [grids and screen hosts on both platforms](../../docs/guidelines/patterns/grids-and-screen-hosts.md)
for the WinForms API mapping and native/browser differences.

Each grid owns its controller and browser subscriptions, but **does not own the data source**. If you supply a disposable source, its creator or DI scope must dispose it. Reads are serialized/coalesced and stale results are ignored after a newer request, source replacement or disposal. `IDataSource<T>` does not expose read cancellation tokens, so an already-running source call may still finish.

### Power-user grids

Dense record screens put their commands in the shell's menus and toolbar and keep the grid to its data. These parameters opt in to that presentation; their defaults keep the grid as described above.

| Parameter | Default | Effect |
| --- | --- | --- |
| `Density` (`BlazorGridDensity?`) | null | `Compact` gives smaller paddings (rows keep .3rem above and below their text) and a smaller font. Null uses `ApplicationOptions.GridDensity`, which `BuildBlazorApplication().WithGridDensity(BlazorGridDensity.Compact)` sets for every grid of an application and which defaults to `Normal`. Default nested grids inherit the density. |
| `ActionStyle` (`BlazorGridActionStyle`) | `Text` | `Icons` shows row commands as small icon buttons whose names are their tooltips and accessible names, in a column sized to its icons with no header text. `Text` keeps the 120-pixel column headed "Actions". |
| `RowCommands` (`BlazorGridRowCommands` flags) | `None` | Built-in `Open`, `Edit` and `Delete` commands on each row, before the custom `Actions`. Open needs `ItemOpened`; Edit and Delete need the update and delete capabilities and replace the toolbar's Edit and Delete buttons. Delete selects the row and asks for confirmation: with `ActionStyle` Icons through the confirmation dialog of `ViewService` when a modal host is initialized, otherwise, and in the default text style, through the grid's own confirmation bar. |
| `OpenCommandIcon`, `EditCommandIcon`, `DeleteCommandIcon` | null | Icon CSS classes for the built-in row commands; null uses the grid's inline SVG icons. Opening a row opens its record for editing, so the built-in Open icon is the edit pencil, never a folder; its accessible name stays "Open". |
| `RowClick` (`BlazorGridRowClick`) | `Select` | `Open` with `ItemOpened`: a single click selects and opens the row, as record lists do. A click on a control inside the row (a row command, the detail toggle, an editor, a link, a button, an input or anything marked `data-grid-no-open` in a cell template) does only its own work, a click that ends a text selection opens nothing, a double-click opens once, Enter still opens and clicking the selected row keeps it selected. The grid's browser module reports the click. |
| `ReloadPlacement` (`BlazorGridReloadPlacement`) | `Toolbar` | `Header` places a small reload icon in the end cell of the column header row; `Hidden` offers none. |
| `Fill` | false | Stretches the grid to its container's height instead of `Height`; the rows scroll beneath the sticky header. The container needs a definite height, such as a `SplitContainer` panel. |
| `Footer` (`BlazorGridFooterMode`) | `Always` | `Paged` shows the footer with the item count and pager only while the results span more than one page; `Hidden` never shows it, so pair it with `BlazorGridPageMode.Single`. |
| `Caption` | `"Data grid"` | An empty caption renders no caption element. |
| `SelectedItem`, `ItemOpened` | | Two-way selection for a detail panel, and opening by double-click (or a single click with `RowClick` Open), Enter or the Open command, as described under selection above. |

The toolbar row is omitted when nothing remains in it: no search capability, reload not in the toolbar, no New button (no create capability or no `GridTypes.NewItem` trait), no Edit or Delete button and no bound `Preferences`. `GridAction<TItem>.Icon` is a custom action's icon class; an action without one shows its `IconURL` image, or its name.

```razor
@using Sphere10.Framework
@using Sphere10.Framework.Web.AspNetCore.Blazor
@using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid

<BlazorGrid TItem="Person" DataSource="_source" Columns="_columns" Actions="_actions" Caption=""
	AllowedCapabilities="BrowseCapabilities" AllowCellEditing="false" PageSize="50" Height="600"
	Density="BlazorGridDensity.Compact" ActionStyle="BlazorGridActionStyle.Icons" RowCommands="BlazorGridRowCommands.Open"
	RowClick="BlazorGridRowClick.Open" ReloadPlacement="BlazorGridReloadPlacement.Header" Footer="BlazorGridFooterMode.Paged"
	@bind-SelectedItem="_selected" ItemOpened="OpenAsync" />

@code {
	private const DataSourceCapabilities BrowseCapabilities = DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;
	private readonly PeopleDataSource _source = new();
	private readonly BlazorGridColumn<Person>[] _columns = { BlazorGridColumn<Person>.For(person => person.Name), BlazorGridColumn<Person>.For(person => person.Age) };
	private GridAction<Person>[] _actions;
	private Person _selected;

	protected override void OnInitialized() =>
		_actions = new[] { new GridAction<Person>("Delete Person", null, null) { Icon = "fas fa-trash-alt", ActionWorkAsync = DeleteAsync } };

	private Task OpenAsync(Person person) => Task.CompletedTask; // Open the person's record screen.

	private Task DeleteAsync(Person person) => Task.CompletedTask; // Confirm, then delete through the source.
}
```

The tester's **Application patterns → Power-user records** screen combines such a grid with icon toolbar commands, a File and a Product menu, and a detail panel in a `SplitContainer`, as described under [master-detail layouts](#master-detail-layouts-and-field-rows).

## Hierarchical sources and custom row details

Bind an `IHierarchicalDataSource<TNode>` to display a process tree, organizational graph or other source-owned hierarchy. The grid detects the interface and uses its stable node keys, child availability, synchronous `GetEntity` accessor and asynchronous `GetChildrenAsync` query. A node is still the row type used by columns, selection and CRUD. Its entity is the resolved payload supplied separately to a detail template.

This component can be added to the Interactive Server host described above:

```razor
@using Sphere10.Framework
@using Sphere10.Framework.Web.AspNetCore.Blazor
@using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@rendermode InteractiveServer

<BlazorGrid TItem="GraphNode<Record>" DataSource="_source" Columns="_columns"
	Caption="Teams" PageSize="2" DetailHeight="260" PersistSettings="false" GridType="0" />

@code {
	private readonly ActionGraphDataSource<Record> _source = new(new[] {
		new GraphNode<Record>(new Record(1, "Alpha team")) {
			Connections = new[] { new GraphNode<Record>(new Record(2, "Team member")) }
		}
	}, node => node.Entity.Id);

	private readonly BlazorGridColumn<GraphNode<Record>>[] _columns = {
		new() {
			ColumnName = "Name", PropertyName = nameof(Record.Name), SortName = nameof(Record.Name),
			DataType = typeof(string), PropertyValue = node => node.Entity.Name,
			CanEditCell = false, ExpandsToFit = true
		}
	};

	public sealed record Record(int Id, string Name);
}
```

`ActionGraphDataSource<TEntity>` serves an existing graph as read-only sibling pages. Each level searches, sorts and pages its own siblings. The graph source captures topology at construction; it does not fetch descendants from a server. A database or network implementation can instead supply child views through `IHierarchicalDataSource<TNode>` and resolve them asynchronously. The [data-source guide](../../docs/guidelines/patterns/data-sources.md) describes node identity, payloads and child-view ownership.

To render a panel instead of the default child grid, set `DetailTemplate`, a `RenderFragment<BlazorGridDetailContext<TNode>>`. Its context contains `Node`, resolved `Entity` and `ChildDataSource`. For an ordinary `IDataSource<T>`, a template also enables row details; `Node` and `Entity` are the row and `ChildDataSource` is null. Known hierarchical leaves also supply a null child source without fetching children. `HasDetail` adds eligible custom panels without suppressing source-provided hierarchy, and `DetailIdentity` identifies ordinary rows across object replacements. Hierarchical sources always provide their own identities. The complete [GridDemo](../../utils/Sphere10.Framework.Utils.BlazorTester/Demos/GridDemo.razor) shows both the default nested grid and a custom record panel.

`DetailsEnabled` defaults to true; disabling it closes details and cancels pending resolution. `MaximumDetailDepth` defaults to 8 and `MaximumExpandedDetails` to 16 across the root grid and its nested details. A cycle cannot expand again on the same path, while shared nodes can appear beneath independent parents. `DetailHeight` defaults to 240 CSS pixels and bounds a default child grid's viewport; browser layout remains independent of native WinForms control sizing.

Root and child grids have independent selection and CRUD contexts. Child selection does not fire the parent's `SelectedItemChanged` or change its `SelectedItem`. A custom template can handle the child grid's callbacks directly when the application needs a combined selection policy. Default child grids inherit columns and allowed operations, but do not persist separate settings.

The component borrows its bound root source and owns the child views returned by expansion. It releases each view through `IAsyncDisposable` or `IDisposable` after nested grid consumers finish their admitted work. Final disposal drains source I/O and detail resolution. Collapse, removal from the visible page, changed search/sort, replacement of the bound root and disposal cancel pending detail work and reject late results. A changed `GetChildQueryKey` reloads the child view while preserving the expanded row. With compatible node/query keys, pending acquisition finishes before a coalesced refresh uses the newest accepted node. Each accepted parent page refreshes expanded child captures even when the provider reuses node objects. The nested grid retains its controls, query, selection and compatible expansions as its source view is replaced. Retained rows stay paired with their accepted source, and CRUD waits for a page from the replacement source. A nested grid with a row draft keeps its current view and the draft until the edit is saved or cancelled, then adopts the replacement. The parent presents its accepted rows before expanded details reload, and those details reload together. Auto paging measures source rows only, so opening a detail scrolls the viewport instead of paging its row away. Custom templates borrow the context and must not retain or dispose its child source beyond the detail lifetime. `ExpandDetailAsync`, `CollapseDetailAsync` and `IsDetailExpanded` support application-driven expansion on the owning renderer context.

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

Custom row actions are application commands. They do not automatically run CRUD validation or enforce `CanUpdate`/`CanDelete`; use the built-in commands for normal persistence. `ActionWorkAsync` is awaited and takes precedence over the legacy synchronous `ActionWork`. Errors appear in the grid, and successful actions request a refresh. With `ActionStyle="BlazorGridActionStyle.Icons"` an action shows its `Icon` class with its `Name` as tooltip, as described under [power-user grids](#power-user-grids).

## Grid architecture and WinForms correspondence

| Responsibility | Implementation |
| --- | --- |
| Rendering and component parameters | [BlazorGrid.razor](UI/Controls/BlazorGrid/BlazorGrid.razor) and its [code-behind](UI/Controls/BlazorGrid/BlazorGrid.razor.cs) |
| Query, selection and edit workflow | [IBlazorGridController<T>](Grids/IBlazorGridController.cs), abstract base, concrete controller and decorator |
| Value access and templates | [BlazorGridColumn<T>](Grids/BlazorGridColumn.cs) and [BlazorGridCellEditContext<T>](Grids/BlazorGridCellEditContext.cs) |
| Scalar conversion | [Tools.BlazorGrid](Grids/GridTool.cs) |
| Existing-reference selection | [BlazorGridReferencePicker](UI/Controls/BlazorGrid/BlazorGridReferencePicker.razor) |
| Hierarchy and custom row details | [BlazorGrid.Details.cs](UI/Controls/BlazorGrid/BlazorGrid.Details.cs) and [BlazorGridDetailContext<TNode>](Grids/BlazorGridDetailContext.cs) |
| Browser layout/lifetime | Collocated isolated CSS and JavaScript |

Both the WinForms `VisualGrid` and Blazor grid consume the same core `IDataSource<T>`, `DataSourceCapabilities`, `CrudAction` and `Result`. WinForms column bindings correspond to typed Blazor columns; entity/property editors correspond to the buffered row editor and Razor templates; native reference dropdowns correspond to `BlazorGridReferencePicker`. Browser rendering and interaction remain in this library. Blazor does not provide a recursive native PropertyGrid; use explicit editors for owned objects.

Sequence-valued data properties use arrays: grid `Columns`, `Actions`, `ColumnDefinitions`, controller `Items` and `ValidationErrors`, application block `Menus`, menu `Items`, UI-service/host collections, table `Items`/`Page`, and `ItemsResponse<T>.Items`. Materialize queries with `.ToArray()` before assigning them. Controller and registered application collections return array snapshots; replacing entries in a returned array does not change stored membership. Use configuration setters, builders or host operations to make changes.

`grid.Controller` exposes additional query/edit operations, current items, validation errors and custom entity comparison. Direct mutation calls bypass the component's create/update/delete callbacks and its delete-confirmation UI; selection changes are still observed through the controller's change notifications. Use component methods and controls when you need the complete interaction. The component creates and disposes its own controller; do not dispose that instance or share it between grids.


## ApplicationBlock architecture

The application implementation types use names such as `Application`, `ApplicationScreen` and `IApplicationScreen` in the root `Sphere10.Framework.Web.AspNetCore.Blazor` namespace. Their namespace distinguishes them from `Sphere10.Framework.Application.UI` contracts and bases. When a platform extension and its shared contract have the same name, qualify the shared type with a namespace alias such as `using SharedUI = Sphere10.Framework.Application.UI;`. WinForms and Blazor share block/menu storage, builder state and validation, menu notifications, action execution and activation-policy checks. Blazor also delegates registration snapshots and block traversal to shared algorithms. The shared library has no ASP.NET Core or desktop UI dependency.

`IApplicationScreen` extends the shared `SharedUI.IApplicationScreen`, Blazor's `IComponent` and the typed web command provider. Shared applications, blocks, screens and plugins all inherit `IApplicationCommandProvider`; their portable command descriptors remain separate from native controls and web rendering. The typed web provider bridges its actual menu and toolbar arrays to that shared contract. Derive from `ApplicationScreen` for automatic renderer attachment, or implement that interface directly and attach the renderer-created instance to the host. The Blazor host retains a `ApplicationScreenSession` implementing that screen contract as a logical proxy. The proxy delegates lifecycle, commands, help and dynamic title to its renderer-created `Screen` component. `IApplication.ScreenHost.ActiveScreen` exposes that screen identity; only Blazor retains tab IDs, immutable component parameters and renderer attachment.

| WinForms concept | Blazor implementation |
| --- | --- |
| Plugin configuration | `BlazorPluginBuilder` groups blocks and service registrations through `AddSphere10BlazorPlugin` |
| Shared application hierarchy | `IApplication.Blocks` → `IApplicationBlock` → menus/screens; `LoadedPlugins` records successful startup contributors, each of which can supply several blocks |
| ApplicationBlock and fluent builders | `ApplicationBlockBuilder`, `ApplicationMenuBuilder`, `ApplicationMenuItemBuilder` |
| Shared screen host | `IApplicationScreenHost` for navigation; `ISingleApplicationScreenHost` for the active screen; `IMultiApplicationScreenHost` for screen collections, with generic projections and shared decorators |
| Blazor host projection | `IApplicationScreenHost`, `ApplicationScreenHostBase`, `ApplicationScreenHost`, `ApplicationScreenHostDecorator<TConcrete>` |
| Application interaction and commands | `IUserInterfaceServices`, `ApplicationUserInterfaceServices` and `BlazorUserInterfaceServices` |
| ApplicationScreen | `ApplicationScreen : ComponentBase, IApplicationScreen` |
| Single-instance screens | One retained, keyed component per screen type in a circuit, including selection through another block |
| Multi-instance screens | Independent sessions and component instances with unique IDs |
| Permanent singleton screens | Automatically opened normal screens whose tabs cannot be closed by users |
| Empty-workspace screens | Real tabless components displayed when no normal screens remain |
| Screen hide/close cancellation | Awaited `CanDeactivateAsync` before changing state |
| Screen display/hide lifecycle | `OnActivatedAsync` / `OnDeactivatedAsync`; disposal stays with the Razor renderer |
| Desktop images and control menus | Browser icon URLs/CSS classes and Razor menu components |
| Desktop screen tabs | `UI.Application.ApplicationShell` with open-screen selection and close controls |
| Screen navigation | Registered block/screen IDs and optional session IDs in the URL, with Back/Forward and bookmark support |

`IApplicationScreenHost` owns available and registered blocks, active block and screen selection, activation, screen mode, navigation guards and `UserInterfaceServices`. `ISingleApplicationScreenHost` adds close and close-check operations for the active screen. `IMultiApplicationScreenHost` adds retained/open screen arrays, ordering and operations on particular screen references or batches. Each contract has shared generic projections; `IApplicationScreenHost` supports both capabilities while its layout changes dynamically. The portable layer has no Blazor session, GUID or renderer-attachment model. `IUserInterfaceServices` borrows the host for command execution and supplies platform interaction and UI dispatch.

Each block owns its independently added `ScreenDefinitions` and optional `DefaultScreenDefinition`. Use `AddScreen<TScreen>(screen => ...)` on the block, then add presentation commands with `AddShowScreenItem<TScreen>()` or an exact registration reference. Type-based menu items choose the first matching registration in that block. An exact reference selects its specific registration, including when several registrations share a component type. Menus and toolbars never add registrations as a side effect. Defaults and programmatic type-based opening require an unambiguous registration.

The optional `AddScreen(configure, out registration)` overload keeps the callback first and returns the parent builder. The moniker is an identity, not a live component or a mutable pointer to the last graph built. A retained source moniker resolves against each independently built block or circuit. Each graph owns its canonical definition and reverse `Activators` membership; opening a foreign graph's definition is rejected. `host.OpenScreenAsync(block, registration)` and the canonical-definition overload use the same guards and lifecycle as ordinary navigation, including for menu-free screens.

Ownership is explicit: `item.Parent` is its menu, `menu.Parent` is its block, and `block.Parent` is the circuit's application. `block.Source` references its circuit’s successfully loaded plugin owner; settings-bearing owners are copied per circuit while startup registrations retain one-time loading ownership. Standalone blocks have no source. Unregistering a block clears its application parent while preserving its source and graph for registration again. Toolbar commands use a nonvisual owner menu. Menu-free opening does not fabricate a command. `ExecuteMenuItemAsync(item, cancellationToken)` accepts the command object. Screen command execution follows `item.Parent.Parent` and requires the current circuit's canonical block and definition references; commands from other circuits are rejected. Application menus, toolbars and merged presentation menus can borrow block-owned commands while retaining their original references and parents.

Platform interfaces specialize these ownership properties and project them to the shared contracts through inline default interface members in `#region Base Interface Bridges`. An attached screen can use `Block.Parent.Logger` for its application's logger and its own `Host` for navigation. Use these object relationships instead of searching all blocks or resolving command IDs during ordinary execution.

`WithDefaultScreen<TScreen>()` resolves an already registered screen; its exact-reference overload supports same-type registrations. An unspecified Blazor activation policy uses the platform's MultiInstance default independently of a sibling's explicit policy. SingleInstance and PermanentSingleton reuse belong to the registration and owning block, rather than the component type. Distinct registrations can have distinct titles, parameters, policies and activators.

The screen builder's `WithParameters` captures default component parameters. Its optional `WithActivator` prepares a `ApplicationScreenActivation` from the current services, owning block and canonical registration, only when a new session is needed. Configuration, graph copying and singleton selection do not execute the factory. The Razor renderer still constructs and disposes the component. Supply reuse keys to runtime opening rather than returning them from the registration activator.

Menu-free and automatically named commands use `__screen:<escaped-registration-key>` routes. A unique screen type gets a caption-independent key automatically; duplicate types need `WithStableKey` before persistent navigation or startup selection. Keys are validated within the block. Explicit command IDs remain supported as bookmark aliases. New startup choices store the registration key; legacy type-only choices migrate only when unambiguous, without using an old numeric route index to select a registration.

Inject the smallest host contract needed: `IApplicationScreenHost` for navigation, `ISingleApplicationScreenHost` to close the current screen, or `IMultiApplicationScreenHost` for collections and instance operations. Use `IApplicationScreenHost` when renderer attachment, tab IDs or runtime component parameters are needed. `AddSphere10Blazor` aliases all host contracts to one circuit-local instance, with a separate scoped UI service exposed by `ScreenHost.UserInterfaceServices`. `IApplication` exposes runtime blocks, loaded plugin metadata, application commands and lifecycle without duplicating selection or interactions. `IApplication<TPlugin, THost>` defines the typed aggregate projections once. The shell uses the Blazor extension for tabs and routing.

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
@inherits Sphere10.Framework.Web.AspNetCore.Blazor.ApplicationScreen

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
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor;

builder.Services
	.AddSphere10BlazorPlugin(plugin => plugin
		.WithName("Workspace")
		.AddBlock(block => block
			.WithId("work")
			.WithName("Workspace")
			.AddScreen<OverviewScreen>("Overview", out var overview, activationMode: ScreenActivationMode.SingleInstance)
			.WithDefaultScreen(overview)
			.AddMenu("Work", id: "main", icon: "fas fa-desktop", configure: menu => menu
				.AddItem(overview, "Overview", id: "overview", icon: "fas fa-home")
			)
		)
	);
```

Screen registration accepts a title directly: `AddScreen<OverviewScreen>("Overview")`.
Its optional named arguments cover policy, kind, stable key, settings/defaults,
parameters, activation preparation and the settings editor. Add `out var registration`
immediately after the title to retain an exact moniker. The runtime form is
`AddScreen(typeof(OverviewScreen), "Overview", out var registration)`.

`AddMenu(text, icon: ..., id: ..., configure: ...)` and `AddMenuItem(...)` create the
same owned menu; the latter is a convenience name for a top-level menu. Menu
`AddItem` and block `AddToolBarItem` accept actions, generic/runtime screen types,
exact registrations or definitions, and child-item arrays for groups. Their optional
arguments configure IDs, icons, visibility and enablement; an `out` overload captures
the configured command before snapshot construction. Existing callback overloads
remain useful for incremental configuration. Configuration never runs screen
activators or command actions, and repeated web builds keep independent ownership
snapshots.

`BlazorPluginBuilder` inherits `WithName`, `AddBlock(block)`, `AddBlock(block => ...)`, `AddServices`, `AddInitializer<T>()` and `AddFinalizer<T>()` from the shared typed `ApplicationPluginBuilderBase`; it supplies the Blazor plugin factory and composes the platform block, menu and item builders. Its product derives from the shared `ApplicationPlugin`, which owns name/block storage, startup service callbacks and Loaded/Unloaded notifications. Register plugin dependencies with `AddServices(registry => ...)`. Child configuration runs immediately; service callbacks run during plugin loading, with scoped dependencies created for each circuit. Repeated service callbacks compose in registration order.

For reusable definitions, call `new BlazorPluginBuilder().WithName(...).AddBlock(block => ...).Build()` and pass the result to `AddSphere10BlazorPlugin(plugin)`. Existing blocks can be supplied to `AddBlock(block)`, including blocks created with `new ApplicationBlockBuilder()...Build()`. `AddApplicationBlock(...)` remains available for standalone blocks, which appear directly in the running application's `Blocks`. The fluent registration calls `Build()` for you and exposes the same plugin object through both `IBlazorPlugin` and the shared `IApplicationPlugin`, alongside shared host navigation and UI interaction services. It does not create a child service provider.

`BlazorPlugin` uses `ApplicationPluginBase<IApplicationBlock>`, preserving the existing `ApplicationPlugin` class contract. Its single compatibility projection keeps typed virtual getters authoritative through both class and interface views. Direct interface implementations use the default projection on `IApplicationPlugin<TBlock>`; plugin classes no longer repeat that adapter.

For named plugin definitions, place the fluent configuration in `public static void Configure(BlazorPluginBuilder plugin)` and register it with `services.AddSphere10BlazorPlugin(MyPlugin.Configure)`. The running tester uses this pattern in [Sphere10Plugin](../../utils/Sphere10.Framework.Utils.BlazorTester/Loader/Sphere10Plugin.cs) and [WidgetGalleryPlugin](../../utils/Sphere10.Framework.Utils.BlazorTester/WidgetGallery/WidgetGalleryPlugin.cs); their dependencies and block/menu definitions live together, and `Program` supplies those definitions through the application builder's `AddPlugin` method.

Plugin names must be nonblank and unique within a host. Explicit IDs must be unique within their scope; item IDs are unique across all menus in a block. Register a concrete Razor component implementing `IApplicationScreen`, normally by inheriting `ApplicationScreen`. Each registration validates its own policy and kind. Registrations of the same type can differ, and a default screen need not appear in a menu.

`AddApplicationBlock` also registers the shared `IApplicationBlock` view. The Blazor screen host captures validated structural snapshots and exposes `AvailableBlocks`, `Blocks`, `SelectedBlock` and `ActiveBlock`; there is no separate public block catalogue service. Resolve it from the current circuit scope to inspect or change navigation:

```csharp
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;

var host = scopedServices.GetRequiredService<IApplicationScreenHost>();
var workspace = host.AvailableBlocks.Single(block => block.Name == "Work");
await host.SelectBlockAsync(workspace, cancellationToken);
```

The shared `IApplicationScreenHost.ShowScreenUndockedAsync(screen)` operation is optional platform presentation. Blazor throws `NotSupportedException` without changing the current session, retained instances or ownership; it does not turn that request into tab navigation or open a browser window. Use the ordinary shared navigation operations for browser screens.

Definitions and plugin registrations are application-wide. Their UI snapshots, open sessions, active selections, event aggregation and modal services belong to the user circuit. Action delegates receive that circuit's `IServiceProvider`: do not capture a scoped service in a singleton registration. Registration-time menu event handlers are retained by snapshots and must also avoid capturing per-user state. Parameter dictionaries are copied, but their values are shallow copies; use immutable values and inject mutable per-user services into the component.

Await `ReportInfoAsync`, every `ReportErrorAsync` overload, `ReportFatalErrorAsync` and `AskYNAsync` for modal interaction. `BlazorUserInterfaceServices` uses the existing `Modal.ViewService` and the shell's renderer dispatcher; completion waits for dismissal or confirmation, and reporting forwards cancellation to the modal host. The Application UI contract has no synchronous reporting aliases. Synchronous `AskYN` is unsupported because blocking a Blazor circuit prevents interaction. For the same reason, synchronous application `Exit` rejects a renderer synchronization context before starting an exit request; use `ExitAsync` there. Worker threads can use synchronous `Exit`, and an already exited application returns true on either context. `ExecuteInUIFriendlyContextAsync` awaits renderer dispatch and propagates errors to its caller. The shell attaches its controller for its own lifetime; the coordinator borrows the screen host and never disposes it.

For runtime entity editors, register the component with `AsMultiInstance()` and open its canonical definition with immutable entity IDs and an optional reuse key. Inside an attached `ApplicationScreen`, use its existing `Block` and `Host` references:

```csharp
using System.Collections.Generic;
using System.Linq;
using Sphere10.Framework.Web.AspNetCore.Blazor;

var activation = new ApplicationScreenActivation(
	new Dictionary<string, object> { [nameof(ProjectScreen.ProjectId)] = projectId },
	instanceKey: projectId.ToString("N")
);
var definition = Block.ScreenDefinitions.Single(definition => definition.ScreenType == typeof(ProjectScreen));
var session = await Host.OpenScreenAsync(Block, definition, activation, cancellationToken);
```

`ApplicationScreenActivation` copies its parameter dictionary immediately; new sessions merge runtime overrides with the registration's defaults and prepared parameters using case-insensitive names. Values are shallow copies, so pass immutable identifiers rather than mutable drafts. A nonnull key reuses a multi-instance session for the same owning block and registration in the current circuit. Two commands referencing that registration share its keyed instance. Reuse preserves its original parameters and draft. Keys use ordinal, case-sensitive equality; null keys retain ordinary multi-instance behavior. Singleton policies reuse their own registration regardless of runtime keys.

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

For stable entity URLs, optionally supply `NavigationUriFactory="GetScreenUri"`, where `GetScreenUri(ApplicationScreenSession session)` returns a path such as `project/{projectId:D}` using the session's immutable parameters, or `application` for ordinary/null sessions. The callback is evaluated against the active session inside location synchronization, avoiding a parent-render race when tabs change. Direct programmatic host activation also schedules URL synchronization after rendering, while in-flight route requests and shell interactions retain ownership of their own navigation. Its path must remain within the application base and contain no query or fragment. When supplied, the shell uses that path directly and omits its default ephemeral block/screen/instance query. The containing routable page owns resolving that stable URL to a keyed activation on entry or refresh. Generated selection URLs do not repeat the host's already-accepted leave guards; other internal navigation still checks unsaved screens.

`NavigationPath` is relative to the host's base URI. `ApplicationShell` supplies the application layout itself: menus and toolbar span the top, active-block navigation fills the left sidebar, compact block icons sit at its bottom, and tabs sit directly above the scrolling screen area. Place it inside a theme/modal provider layout without a second navigation shell. `Header` supplies branding beside the compact application title; `ToolBarContent` places search, theme or identity controls beside the command buttons in the toolbar below the file menus. The toolbar renders when that fragment is supplied even if there are no command items. `ApplicationCommandBar` exposes the same `ToolBarContent` slot for independent layouts. `NavigationActiveItemId` optionally selects a native sidebar command for a guarded subview inside the active screen; leave it null to select the screen registration as before. Update the value only after the subview accepts navigation. `SidebarHeader` supplies branding or an endpoint selector, `Sidebar` adds content below the block menus, and `Footer` supplies the status bar. `EmptyContent` is a fallback fragment when no screen is selected; a registered `ScreenKind.Empty` screen supplies a real component with its own lifetime instead. Narrow screens expose the navigation through the Menu button. For an existing layout, compose `ApplicationBlockMenu`, `ApplicationMenus` and `ApplicationScreenHostView` directly with `IApplicationScreenHost`. A menu selection awaits its action and exposes failures to the containing Blazor error boundary.

Bookmarks identify registered screens, never arbitrary component type names. A session ID restores an existing screen in the current workspace; a fresh circuit recreates the screen from its registered definition. Retained component state lasts while the workspace remains mounted. Refreshing or leaving the workspace disposes its components; use a scoped state service or persistence when longer retention is required.

### Offering a block to some users only

`AvailableWhen(services => ...)` on a block builder offers that block only to circuits whose scoped services satisfy the predicate. Each circuit's screen host asks once while it is built, before it copies the startup definition. A block that declines does not exist in that circuit. It is absent from `AvailableBlocks`, `Blocks`, the block dock, the sidebar, the Screens menu and Options. Its permanent screens are never opened, and startup selection skips it, so the first available block with a default supplies the startup screen. A `?block=` route to it shows the same "not found" message as an unknown ID, and `RegisterBlockAsync` rejects it. A block without a predicate is offered to every circuit.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor;

builder.Services.AddScoped<WorkspaceViewer>();
builder.Services
	.AddSphere10BlazorPlugin(plugin => plugin
		.WithName("Administration")
		.AddBlock(block => block
			.WithId("administration")
			.WithName("Administration")
			.AvailableWhen(services => services.GetRequiredService<WorkspaceViewer>().IsAdministrator)
			.AddScreen<UsersScreen>("Users", out var users, activationMode: ScreenActivationMode.SingleInstance)
			.WithDefaultScreen(users)
			.AddMenu("Administration", configure: menu => menu
				.AddItem(users, "Users", id: "users")
			)
		)
	);
```

`UsersScreen` is your own screen component. `WorkspaceViewer` stands for your own scoped service that already knows the signed-in user when the host is built, and `IsAdministrator` is its member. Resolve such state from the supplied provider and never capture a scoped service in the startup lambda. The predicate is synchronous. The host is scoped, so a prerendered request and the interactive circuit that follows each build one host and each ask once. The decision then holds for that circuit, and a change in a user's entitlement applies to their next circuit. A predicate that throws fails the construction of that circuit's host instead of showing or hiding the block.

Availability is presentation only. It removes navigation, and it does not authorize anything. Every service behind the block's screens must still authorize the signed-in user on the server, because a request can reach a service without passing through the shell.

The contributing plugin stays loaded in every circuit, so its services, lifecycle tasks and plugin-level preferences are unaffected. A directly constructed block declares the same rule as an `ActionApplicationBlock`, which takes the predicate in its constructor (`new ActionApplicationBlock(services => ...) { Id = "administration", Title = "Administration" }`). An `ApplicationBlock` subclass overrides the protected `DetermineAvailability(IServiceProvider)` hook instead, and a custom `IApplicationBlock` implements `CheckAvailability(IServiceProvider)`, which the host calls.

### Screen Options

A screen declares its public nested `SettingsObject` class with
`[Settings(typeof(...))]`; an optional covariant `ScreenSettings` override provides
typed access. Matching builder-supplied values retain their identity, while a
conflicting schema throws during registration. The registration owns the actual settings instance;
the screen, Options and its bound controls use that same instance. Block and
plugin builders likewise expose `UseSettings` for `BlockSettings` and
`PluginSettings`. Options enumerates the actual owners, including unopened,
menu-free screens with settings and service-only plugins. Screens without
settings are omitted. The active screen's page is preferred when Options opens;
an explicitly requested valid owner takes precedence. Screens access owner settings through
`GetBlockSettings<T>()` and `GetPluginSettings<T>()`.

`IApplication.Settings` is the provider captured from `UserSettings` during circuit initialization. Ordinary demo and component code uses the same `UserSettings` facade as native code: await `HasAsync<T>` / `GetAsync<T>`, then save the actual object with `SaveAsync()`. Framework circuit initialization and inbound activity bind the scoped browser provider without replacing the process-wide native provider. Interactive startup
restores each owner before operational screens or queries run. Each circuit has
its own settings-bearing plugin owners, block owners and screen definitions;
startup plugin registrations continue to own one-time service loading. The
scoped `BlazorSettingsProvider` uses awaited browser IO and preserves application,
registration and authenticated-user/anonymous-browser identities. A product can
supply its own provider for cross-device storage. Prerender does not access the
browser or construct operational screens with another user's defaults.

A successful `SettingsObject.Save()` or `SaveAsync()` raises its `OnChanged` event.
Screens observe their own, block and plugin settings directly and refresh on the
renderer. The existing accepted-close hook saves completed local edits, while a
veto or hiding a retained screen does not save. Failed saves keep the screen and
its existing ownership available for retry.

Use `WithSettingsEditor<TEditor>()` or `UseSettings<TSettings, TEditor>()` for a
custom `IApplicationSettingsEditor`. Its `Value` is the actual settings
object and `Changed` marks a completed editor input. An explicit registration
editor takes precedence over `[Editor(typeof(MyEditor), typeof(IApplicationSettingsEditor))]`
on the settings class, followed by the class-level `[UserEditable]` automatic
editor. Public properties are editable; `[Transient]` excludes runtime members
from both editing and storage.

**Options → Screen Options...** validates and saves the selected owner on Apply
or OK. Cancel restores the dialog's local serialized backup of unapplied edits;
it retains earlier successful Apply operations. Reset calls the actual settings object's existing `RestoreDefaultValues()` lifecycle,
using `[DefaultValue]` attributes and domain overrides for nested defaults. It stays
provisional until accepted. The dialog has no separate catalog, profile, revision
protocol or persistence service. Existing Options commands remain available.
See [Screen Options](../../docs/usage/screen-options.md) and the
[settings ownership practice](../../docs/guidelines/patterns/user-ui-settings.md).

### Compact block menus

`UI.MainFrame.ApplicationBlockMenu` uses `Blocks`, `ActiveBlock` and `OnSelect` for its default command-button presentation. Set `Compact="true"` to show icons with accessible block names. Icons come from each block's `IconUrl`; a missing icon falls back to the title's first character. `Tooltip` supplies the hover text, defaulting to the block title. The image backdrop can be customized with `--sphere10-block-icon-background` for different artwork.

Supply `Href`, a `Func<IApplicationBlock, string>`, to render ordinary navigation links instead of `OnSelect` buttons:

```razor
<ApplicationBlockMenu Blocks="ScreenHost.Blocks" ActiveBlock="ScreenHost.SelectedBlock"
	Compact="true" Href="GetBlockHref" />

@code {
	private string GetBlockHref(IApplicationBlock block) =>
		$"{Navigation.ToAbsoluteUri("application")}?block={Uri.EscapeDataString(block.Id)}";
}
```

Here `ScreenHost` is the injected `IApplicationScreenHost`, and `Navigation` is the injected Blazor `NavigationManager`; import `Sphere10.Framework.Web.AspNetCore.Blazor` and its `UI.MainFrame` namespace. The destination page passes its query parameters to `ApplicationShell`, which activates the block and applies its existing screen guards and history handling. `Href` only computes the URL; it does not change host state.

A dock rendered independently of the shell should observe `ScreenHost.Changed` and `Navigation.LocationChanged`, dispatch updates through `InvokeAsync`, and unsubscribe when disposed. Pass `ActiveBlock = null` outside the workspace if selection should reflect only the currently displayed workspace. `ApplicationShell` already supplies this integration and renders its dock as command buttons, so the tester consumes it directly instead of maintaining another dock component.

### IdentityControl

`IdentityControl` in `Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls` is reusable in any Blazor layout. It displays a fixed 32-pixel circular avatar and a dropdown of existing `IApplicationMenuItem` commands. The avatar keeps the same dimensions for authenticated and anonymous users, so signing in or out does not resize the toolbar. It accepts identity data and delegates command handling; it does not require an authentication service or perform sign-in/sign-out itself.

```razor
@using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls

<IdentityControl User="currentUser" Items="accountCommands" OnSelect="SelectAccountCommandAsync" />
```

Here `currentUser` is a `System.Security.Claims.ClaimsPrincipal`, `accountCommands` is an `IApplicationMenuItem[]`, and `SelectAccountCommandAsync` accepts a menu item and returns `Task`. Build commands with `ApplicationMenuItemBuilder`, and use `BlazorMenuSeparator` between groups. The callback can await `IUserInterfaceServices.ExecuteMenuItemAsync(item)` for existing application commands, or dispatch to the host's own account/profile services. Selection awaits the callback and suppresses duplicate clicks while it runs; failures propagate to the containing error boundary.

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

By default, the bottom block dock browses navigation independently of the displayed screen. `SelectBlockAsync` changes `SelectedBlock` and its left-hand menus without activating a screen, invoking screen guards or adding browser history. `ActivateBlockAsync` explicitly opens a block's default screen. Choosing a menu screen or a tab goes through the existing guards and restores that screen's owning block in the navigation pane. The active tab, its toolbar and its menu contributions stay in place while another block is browsed. `ActiveBlock` follows the visible screen's owner, with `SelectedBlock` as the fallback when no screen is active. Command owners cache membership on activation and return snapshots; checked/enabled predicates remain live.

Set `ApplicationShell.ActivateBlockOnSelection="true"` when selecting a dock button should enter that block immediately. The shell then calls the screen host's guarded `ActivateBlockAsync` through its normal interaction path: an accepted transition activates the default screen, refreshes command scope, closes the mobile navigation drawer and adds the corresponding browser-history entry. Action-only blocks can become active with no selected screen. If a leave guard rejects the transition, the current block, screen, menus, drawer and URL remain unchanged. The parameter defaults to false to preserve navigation browsing for existing consumers.

`AddSphere10Blazor` registers one `Application` per circuit through both the platform `IApplication` and the shared `SharedUI.IApplication`. The runtime hierarchy is **Application → Blocks → Menus/screens**. `Blocks` projects the live screen host's canonical block snapshots. `LoadedPlugins` returns the circuit’s plugin owners whose startup registration completed successfully; settings-bearing owners are isolated per circuit. One plugin can contribute several independently selectable blocks; each bottom-left navigation icon represents a block. Every collection property returns a membership array that callers can change without changing stored membership.

### Application lifecycle

`Application` derives from `AsyncApplicationBase`, which implements asynchronous lifecycle operations and supplies synchronous adapters. The neutral `ApplicationBase` retains shared state and leaves initialization/finalization pairs abstract. `IApplication` provides paired `Initialize()` / `InitializeAsync()` and `Finalize()` / `FinalizeAsync()` methods with shared once-only lifecycle state. An interactive circuit awaits `InitializeAsync()` when it opens and `FinalizeAsync()` when it closes. Temporary disconnects and reconnects retain that application. Server prerendering can construct and render the scoped application without running its lifecycle tasks. The shell continues to initialize screen selection through `ScreenHost.InitializeAsync`; application task initialization does not select a screen or reload plugins.

Register plugin-owned application tasks with `BlazorPluginBuilder.AddInitializer<MyInitializer>()` and `AddFinalizer<MyFinalizer>()`. The helpers register transient tasks keyed by the built plugin's `Name`; loading rejects duplicate names, and custom plugin names must remain stable. Each circuit's application resolves and caches tasks for its successful `LoadedPlugins` through that circuit's service provider, giving applications their own task instances and scoped dependencies. Host registrations using `services.AddInitializer<MyInitializer>(typeof(IApplication))` and `services.AddFinalizer<MyFinalizer>(typeof(IApplication))` remain supported for application-owned tasks. The combined inventory starts with host-owned registrations in registration order, followed by plugins in `LoadedPlugins` order and each plugin's tasks in declaration order. Initializer priorities apply across this inventory within the existing sequential and parallelizable phases; finalizers execute in combined inventory order. The existing unkeyed registrations remain process tasks and are not run by a circuit. The application keeps its task collections protected; `Initializing`, `Initialized`, `Finalizing`, `Finalized` and `Progress` report their execution. `Logger` resolves the registered `Sphere10.Framework.ILogger`, falling back to `SystemLog.Logger`. Put resources owned by this application in `Disposables`; finalization releases them while the scoped host and DI services retain their existing owners. When managing a service scope yourself, await `application.FinalizeAsync()` before disposing that scope, then use asynchronous disposal for the scope and application. The circuit handler already finalizes before its scope is released. Application disposal supplies fallback cleanup for cached tasks and owned resources, but DI may have disposed task dependencies before reaching the application during scope disposal. Completed tasks run once.

Call `WithName(...)` before either lifecycle registration helper. Both type and factory overloads throw `InvalidOperationException` immediately when the plugin has no name.

Task contracts declare both synchronous and asynchronous methods. Derive synchronous tasks from `SyncApplicationInitializerBase` or `SyncApplicationFinalizerBase` and override `Initialize()` or `Finalize()`; their asynchronous calls use `Task.Run`. Derive asynchronous tasks from `AsyncApplicationInitializerBase` or `AsyncApplicationFinalizerBase` and override `InitializeAsync(...)` or `FinalizeAsync(...)`; their synchronous calls wait for the asynchronous operation. The neutral task bases leave both methods abstract, following the repository hierarchy.

For example, using the tester's existing task and status types, a plugin declares its name before supplying factories for its circuit-local dependencies:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.BlazorTester.Application;
using Sphere10.Framework.Utils.BlazorTester.Lifecycle;
using Sphere10.Framework.Web.AspNetCore.Blazor;
using IApplication = Sphere10.Framework.Application.UI.IApplication;

services
	.AddSphere10BlazorPlugin(plugin => plugin
		.WithName("Lifecycle example")
		.AddServices(registry => registry.AddScoped<WorkspaceStatus>())
		.AddInitializer(provider => new DemoPluginInitializer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), "Lifecycle example"))
		.AddFinalizer(provider => new DemoPluginFinalizer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), "Lifecycle example"))
	);
```

This is a service-only plugin; add blocks through the same builder when it also contributes UI. The [running tester](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md#plugin-initialization-and-shutdown) registers four plugins, each with one initializer and one finalizer. Their tasks log through the application logger and record startup/shutdown in circuit-local state. Screens can reach that same application through `Block.Parent`.

### Exit and shutdown

The paired `Exit(...)` and `ExitAsync(...)` methods return whether exit completed. Blazor interactions await `application.ExitAsync(cancellationToken: token)` to confirm exit and check the host's retained screen guards, the active screen's block and the browsed block. It returns false when confirmation or a guard declines. Application subclasses can override the confirmation, permission and completion hooks. Override `ApplicationBlock.CanExitAsync(SharedUI.IApplication application, CancellationToken cancellationToken)` for block permission; snapshots preserve this policy and pass the current circuit's application, so use that argument for runtime context rather than the shared prototype's `Parent`. `force: true` bypasses confirmation and guards, and dismisses a pending exit confirmation. `IsExiting` reports the pending request; `HasExited` reports successful completion.

The shell keeps its screens mounted during confirmation. Successful exit awaits local task finalization and owned resource cleanup, removes screen components through their renderer, and displays terminal content. Once approved cleanup starts, cancellation stops only the caller's wait; cleanup continues and other callers can await the completed exit. Cleanup errors are logged and the application still reaches its terminal state. The borrowed host, shared plugins and web server keep their existing lifetimes. The shell accepts navigation away after exit and rejects further workspace commands or route activation; it does not redirect or terminate the circuit. The tester's File → Exit application command demonstrates this path. Legacy `IUserInterfaceServices.Exit` delegates to the same scoped application and observes failures; await `IApplication.ExitAsync` when the result matters.

### Startup contributions and global commands

Standalone `AddApplicationBlock(...)` registrations appear directly in `Application.Blocks`. An application containing only standalone blocks has an empty `LoadedPlugins` array. Temporarily unregistering/re-registering any block updates the live application block list while successful startup metadata remains available. The Blazor screen host creates immutable block snapshots and maps startup contributions by block ID; the application's block references are the host's canonical snapshots rather than the plugin's source definitions.

Plugin `Load(IServiceCollection)` runs during registration, before the service provider is built. Successful loading, post-load validation and block registration publish an `ApplicationPluginRegistration<IBlazorPlugin>` containing the original plugin reference and captured block contributions. The application factories and startup screen ordering read these receipts, so ordinary DI registration alone does not add an unloaded plugin to `LoadedPlugins`. Service-only plugins appear in `LoadedPlugins` even though they contribute no blocks. Registration rejects both duplicate successful plugin names and recursive attempts to load the same name. Failed registration publishes no receipt and discards newly added lifecycle task descriptors under the attempted name, preventing a later successful plugin with that name from inheriting abandoned tasks. Ordinary `AddServices` side effects retain their existing behavior, and a `Loaded` event may have fired before final validation fails. The shared and Blazor DI aliases resolve the same successfully registered plugin instance. Resolving or disposing a circuit's application does not load/unload singleton plugins or dispose their blocks. `IoCContainer` remains null for compatibility; command delegates resolve scoped dependencies from the executing circuit. Selection and interactions belong to `ScreenHost`; the application exposes live blocks, successful startup metadata and commands. Use the application builder when configuring application-level commands:

```csharp
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor;
using IApplicationBlock = Sphere10.Framework.Web.AspNetCore.Blazor.IApplicationBlock;

services
	.BuildBlazorApplication()
	.OnApplicationCreated(application => {
		var closeAll = new ApplicationMenuItemBuilder()
			.WithId("close-all")
			.WithText("Close all screens")
			.WithIcon("fas fa-times")
			.WithAction(async (provider, token) => {
				var host = provider.GetRequiredService<IMultiApplicationScreenHost<IApplicationBlock, ApplicationScreenSession>>();
				await host.CloseScreensAsync(host.Screens.Where(screen => !screen.IsPermanent && screen.ScreenKind == ScreenKind.Normal), token);
			})
			.Build();
		application.SetMenus(new[] {
			new ApplicationMenuBuilder()
				.WithId("work")
				.WithText("Work")
				.AddItem(closeAll)
				.Build()
		});
		application.SetToolBarItems(new[] { closeAll });
	})
	.Build();
```

Configuration runs once per scope. The callback configures that scope's application; command callbacks resolve their services from the executing circuit. `SetMenus` and `SetToolBarItems` snapshot ordinary action definitions, freezing their registration metadata. Block-owned commands borrowed for application presentation retain their canonical references and parents; screen commands must already belong to the circuit's block graph. A later `AddSphere10BlazorApplication` call replaces the earlier application configuration. The builder delegates to that same API. Its inherited `OnApplicationCreated` calls compose before `Build()`; the shared `ApplicationBuilderBase` also owns title/plugin composition and the terminal build guard. Blazor retains favicon settings and the actual DI registration. The tester uses the builder with [WorkspaceCommands.Configure](../../utils/Sphere10.Framework.Utils.BlazorTester/Application/WorkspaceCommands.cs) and its existing fluent plugin definitions.

`ApplicationShell` composes commands from the application, the active screen's source plugin, its owning block and the screen, in that order. The platform shell owns this composition; applications supply commands through those owners. With no active screen, only application commands appear. Browsing a different navigation block does not change the active owner path. Application commands remain available even when a contextual command has the same local ID. A screen can replace a matching block command and extend File while keeping application commands. The shell offers a standard File menu (ID `Application.FileMenuId`, `"file"`) first among the application menus: any application, block or screen menu with that ID extends it, application items first and separated from the block's and screen's items, and it is shown only while some layer contributes an item. A screen can also contribute a menu named after its record type, such as Product, which follows the application menus; the Screens, Options and Help placement is unchanged. Register block toolbar commands with `ApplicationBlockBuilder.AddToolBarItem`, or build them with `AddToolBarItem`; override the typed arrays on `ApplicationScreen` for screen contributions. `BlazorMenuSeparator` supplies boundaries; composition removes redundant separators and keeps Help last. Toolbar screen commands may reference any definition owned by their block.

Keep commands shared by every workspace at application level. A block command belongs to the active screen's owning block; a screen command disappears when that screen is deactivated. Browsing another sidebar does not change the displayed screen's command context. Within the contextual layer, matching uses local IDs and a narrower action group replaces the matching block group as one command. Application groups remain separate. `ApplicationCommandBar.CommandKeySelector` lets independent layouts namespace renderer identities when different levels share an ID; the shell supplies this automatically without changing command registrations. Hidden items remain registered but are omitted from rendered controls, and menus containing only hidden items are omitted.

Tabs retain each renderer-created component and its state. `ApplicationScreenTabs` provides selection, close buttons, middle-click closure, context commands for closing one/other/all screens, drag reordering, and keyboard interaction. Arrow keys, Home and End select; Ctrl+Shift with those keys reorders; Delete closes; Shift+F10 opens tab commands. Override `Title` to supply a dynamic tab caption; a render of the screen publishes a changed title. Registered icons, long-title ellipsis/full tooltips and dirty indicators retain their separate space. The shell's **Screens** menu, placed like the native main form's between the block menus and Options/Help once a screen is open, lists the exact open sessions including permanent screens, checks the active one, disambiguates duplicate titles with occurrence numbers, and offers **Close All** and **Close All Others** for the screens that permit closing. Choosing an entry calls the normal guarded selection path without opening another instance; hidden retained and Empty sessions are excluded. Menu commands that implement `IApplicationCheckableMenuItem` render as checkable items.

The host defaults to `ScreenMode.MultiView`. The tester's toolbar toggle and View menu await `TrySetScreenModeAsync`; `ApplicationShell.ShowScreenModeSelector` provides an optional header selector. Entering `SingleView` preflights every ordinary tab that would close before removing any; permanent sessions are retained. The synchronous `ScreenMode` setter accepts transitions requiring no closures and rejects transitions requiring async guards; use the awaited method for runtime changes. During single-view navigation, hidden single-instance screens remain mounted and multi-instance screens are released. The multi-screen contract's `Screens` includes retained hidden sessions and `OpenScreens` supplies normal tab membership in presentation order; empty screens are excluded. `MoveScreenAsync` changes that order. `CloseScreensAsync` preflights a batch and preserves every screen if a guard rejects it. Components remain owned by the renderer. Docking is not part of the Blazor host.

Closing the active tab selects the next surviving tab in its current visual order, then the nearest previous tab if none follows. Batch closure uses that order before removals, while closing an inactive tab keeps the active component selected. When no tab survives, `ActiveScreen` is null and the shell displays its passive `EmptyContent` or selection prompt. Closing tabs does not automatically activate an `Empty` screen; startup and explicit navigation to a configured empty-screen block still support that screen's normal lifetime and guards.

Custom Sidebar content can capture its ApplicationShell with @ref and await shell.ExecuteCommandAsync(item). This follows the same guarded command path as the native command bar, closes the navigation drawer after success and synchronizes browser history. Do not bypass the shell by calling its screen host directly from custom navigation controls.

## Command presentation and availability

ApplicationMenuItemBuilder.WithVisibility(false) hides a command from the sidebar, top menus and toolbar while retaining its registered identity. A hidden multi-instance screen can still be opened through ActivateScreenAsync with runtime parameters and an instance key, or through a deep link. Visibility is presentation metadata, not authorization. To leave a whole block out of a user's circuit, including its routes and screens, use [AvailableWhen](#offering-a-block-to-some-users-only) on the block builder.

EnabledWhen(Func<bool>) evaluates current command availability in both the rendered controls and `ExecuteMenuItemAsync(item, cancellationToken)`. Return the screen's dirty state to make Save available only while changes exist. Predicates on screen-owned commands may refer to that screen; shared registrations must not capture a circuit or user. Explicit programmatic screen activation remains independent of command availability.

Screen command state follows the screen. After each render an `ApplicationScreen` compares its title, unsaved state and the captions, icons, visibility, availability and checked state of its menus and toolbar items with the values it last published, and notifies its host only when they differ, so the shell's toolbar and menus refresh without the screen calling `NotifyScreenChanged`. Because values are compared rather than command objects, a screen may rebuild equivalent commands on each read without causing further renders. Call `NotifyScreenChanged()` only for a change that no render of the screen follows, such as one made by a service while the screen stays unchanged.

Toolbars are icon-first. A command with an `Icon` shows only its icon on a toolbar, with its caption as tooltip and accessible name; `ApplicationMenuItemBuilder.WithToolBarText()` (or `ShowToolBarText = true` on a command object) shows the caption beside the icon for a command that needs it, and a command without an icon always shows its caption. `BuildBlazorApplication().WithToolBarText()` (`ApplicationOptions.ToolBarText`) captions every toolbar command of the application. A captioned button shows `ToolBarCaption` (`WithToolBarCaption`, the caption by default); screen commands show their plain name there and keep the name with its blocking reason as the tooltip. On a desktop width long command lists wrap within the toolbar while the toolbar content stays at the right of the first row. Menus show the icon and caption. Disabled toolbar commands are dimmed and their icons greyed. Group toolbar commands with `BlazorMenuSeparator`, which renders a vertical rule.

```csharp
public override BlazorUI.IApplicationMenu[] Menus => _menus;
public override BlazorUI.IApplicationMenuItem[] ToolBarItems => _toolBarItems;

protected override void OnInitialized() {
	var newPerson = new ApplicationMenuItemBuilder()
		.WithId("person-new")
		.WithText("New Person")
		.WithIcon("fas fa-plus")
		.WithToolBarText()
		.WithAction((_, token) => NewPersonAsync(token))
		.Build();
	var deletePerson = new ApplicationMenuItemBuilder()
		.WithId("person-delete")
		.WithText("Delete Person")
		.WithIcon("fas fa-trash-alt")
		.EnabledWhen(() => _selected != null)
		.WithAction((_, _) => DeleteAsync(_selected))
		.Build();
	_menus = new BlazorUI.IApplicationMenu[] {
		new ApplicationMenuBuilder()
			.WithId(BlazorUI.Application.FileMenuId)
			.WithText("File")
			.AddItem(newPerson)
			.Build(),
		new ApplicationMenuBuilder()
			.WithId("person")
			.WithText("Person")
			.AddItem(deletePerson)
			.Build()
	};
	_toolBarItems = new BlazorUI.IApplicationMenuItem[] { newPerson, new BlazorMenuSeparator(), deletePerson };
}
```

Here `BlazorUI` aliases `Sphere10.Framework.Web.AspNetCore.Blazor`, `_selected` is bound to a grid's `SelectedItem`, and `NewPersonAsync` opens a one-step wizard. Selecting a row renders the screen, which enables Delete Person in the toolbar and the Person menu.

### Screen commands

A screen whose commands are plain actions declares them once with `ApplicationScreen.Command` and places them with `UseCommands` instead of building the menu items itself. The default `Menus` and `ToolBarItems` then publish a File menu (`Application.FileMenuId`) holding the `File` commands, a menu named after the record type (`MenuId`, with `MenuText` read on every refresh) holding the `Menu` groups, and the `ToolBar` groups, with a `BlazorMenuSeparator` between groups. One command is one menu item wherever it appears. `EnabledWhen` and `BlockedBy` disable a command, and the blocker's reason is added to its caption and tooltip, as in "Lock Product (Product: Locked)"; `VisibleWhen` leaves it out, and a group with no visible command leaves no separator. Menu items are rebuilt only when a caption, a visibility or the menu caption changes, so availability is read live.

```csharp
protected override void OnInitialized() {
	var newPerson = Command("person-new", "New Person", "fas fa-plus", NewPersonAsync);
	var savePerson = Command("person-save", "Save Person", "fas fa-save", SaveAsync).EnabledWhen(() => _draft.IsDirty);
	var deletePerson = Command("person-delete", "Delete Person", "fas fa-trash-alt", DeleteAsync)
		.BlockedBy(() => _person.CanDelete ? string.Empty : "Person: Has Orders");
	UseCommands(new ScreenCommandLayout {
		File = [newPerson, savePerson],
		MenuId = "person", MenuText = () => "Person",
		Menu = [[deletePerson]],
		ToolBar = [[newPerson], [savePerson], [deletePerson]]
	});
}

// A screen with a busy guard runs every command under it and disables them all while it is busy.
protected override bool IsBusy => _busy;

protected override Task RunCommandAsync(Func<Task> action) => RunGuardedAsync(action);
```

`RunCommandAsync` runs the action directly by default and `IsBusy` is false. A screen that overrides `Menus` or `ToolBarItems` publishes its own items instead.

#### Group box caption commands and state icons

A toolbar command that acts on one group box's feature also appears as an icon in that group box's caption. `UI.Controls.GroupBoxCaption` renders the heading (`Text`, `Level` 2 by default) with the group box's `Commands` as `ScreenCommandIcon` buttons on the right, after any state icons given as its child content. A `ScreenCommandIcon` is the toolbar button's twin: the same icon, disabled exactly when the toolbar item is (`ScreenCommand.IsEnabled`: its `EnabledWhen` and `BlockedBy`, and the screen's `IsBusy`), the caption with its blocker as tooltip ("Change License Template (Product: Locked)"), the command's text as accessible name and the blocker as accessible description. A click runs `ScreenCommand.RunAsync()`, the shell's path through `RunCommandAsync`, and then renders the screen, which publishes any changed command state to the toolbar. The icon renders with its screen and also refreshes when the screen calls `NotifyScreenChanged`. The icons never make the caption taller than its heading, and the caption takes over the heading's bottom spacing (`--sphere10-group-caption-spacing`, 1rem by default). `UI.Controls.StateIcon` shows a state such as Locked as an icon in a fixed slot that keeps its size while the state is off, so showing it never moves anything beside it.

```razor
<section class="card">
	<GroupBoxCaption Text="License Template" Commands="@(new[] { _changeTemplate })">
		<StateIcon Icon="fas fa-lock" Text="Locked" Shown="_product.IsLocked" />
	</GroupBoxCaption>
	...
</section>

@code {
	private ScreenCommand _changeTemplate;

	protected override void OnInitialized() {
		_changeTemplate = Command("template-change", "Change License Template", "fas fa-pen", ChangeTemplateAsync)
			.BlockedBy(() => _product.IsLocked ? "Product: Locked" : string.Empty);
		UseCommands(new ScreenCommandLayout { MenuId = "product", MenuText = () => "Product", Menu = [[_changeTemplate]], ToolBar = [[_changeTemplate]] });
	}
}
``` Commands that need nested groups (`WithChildren`) or checkable items are built with `ApplicationMenuItemBuilder` as above.

WithChildren(routeCommand, segmentCommand, summaryCommand) creates a ApplicationMenuGroup. Return the group from a screen's ToolBarItems to show one Export dropdown containing its action commands. The native command bar retains command execution coalescing, disabled child controls, outside-click dismissal, Escape and focus restoration. Groups inside dropdowns are expandable nested lists with labelled buttons and aria-expanded state. Click or Enter/Space expands a group; ArrowRight enters its first enabled command. Escape or ArrowLeft closes the innermost group and restores its opener; another Escape closes the top menu. Tab follows the visible native buttons. Long lists scroll inside the dropdown, which fits small viewports. Supply a disabled descriptive child such as No active sites for an empty application group. Replacing the menu data prunes unavailable expanded groups. Register screens as top-level block menu items rather than inside action groups; keep child command IDs unique.

## Default, permanent and empty screens

`ScreenActivationMode` and `ScreenKind` come from `Sphere10.Framework.Application.UI`. Lifetime and presentation role are independent.

Screen registrations default to `MultiInstance`: each activation opens an independent session. Configure `AsSingleInstance()` on the block's screen registration for screens that should retain one instance. Menus reference that registration without owning its lifetime.

| Configuration | Behavior |
| --- | --- |
| `SingleInstance` + `Normal` | Reuses one retained session per registration and owning block. |
| `MultiInstance` + `Normal` | Creates a new tab/session on each activation. |
| `PermanentSingleton` + `Normal` | Opens automatically at initialization and remains open across layout changes. |
| `SingleInstance` + `Empty` | Displays without a tab when no normal tabs remain, retaining its hidden component between visits. |
| `MultiInstance` + `Empty` | Displays without a tab when no normal tabs remain; leaving it releases the component, and the next empty workspace creates a new instance. |

`Empty` with `PermanentSingleton` is invalid: a permanent normal screen means the workspace is never empty. A placeholder cannot replace an existing normal tab. Existing screen guards still apply when entering or leaving an empty screen.

Configure lifetime and kind on the registration, then select the default and add navigation commands separately:

```csharp
block
	.AddScreen<HomeScreen>(screen => screen
		.WithTitle("Home")
		.AsSingleInstance()
	)
	.AddScreen<MonitorScreen>(screen => screen
		.WithTitle("Monitor")
		.AsPermanentSingleton()
	)
	.AddScreen<EmptyWorkspaceScreen>(screen => screen
		.WithTitle("Empty workspace")
		.WithScreenKind(ScreenKind.Empty)
		.AsSingleInstance()
	)
	.WithDefaultScreen<HomeScreen>()
	.AddMenu(menu => menu
		.WithText("Screens")
		.AddShowScreenItem<HomeScreen>(id: "home")
		.AddShowScreenItem<MonitorScreen>(id: "monitor")
		.AddShowScreenItem<EmptyWorkspaceScreen>(id: "empty")
	);
```

The component types in these fragments are your own classes deriving from `ApplicationScreen`. A screen need not appear in explorer menus. For example, a dedicated block can provide only the empty-workspace default:

```csharp
plugin
	.AddBlock(block => block
		.WithId("empty")
		.WithName("Empty workspace")
		.AddScreen<EmptyWorkspaceScreen>(screen => screen
			.WithTitle("Nothing open")
			.WithScreenKind(ScreenKind.Empty)
			.AsSingleInstance()
		)
		.WithDefaultScreen<EmptyWorkspaceScreen>()
	);
```

`WithDefaultScreen<TScreen>()` selects an existing registration; an exact definition or registration reference handles duplicate types. Title, kind and activation belong to `AddScreen` configuration, and a default does not require a menu item.

`ScreenHost.InitializeAsync` opens permanent registrations once and chooses the configured default in plugin contribution and block order. Directly registered blocks follow plugin-owned blocks; the first registered screen is the fallback when no default is selected. This is independent of `Position`, which controls the visible block list. An explicit bookmark wins over the startup default. `ApplicationShell` initializes the host; custom shells await `ScreenHost.InitializeAsync` themselves. Later block registration opens its permanent screens without repeating startup selection or replacing an active normal screen.

### Remembering the startup screen

Set an explicit application-qualified browser key to enable the tab context
command **Make Startup Screen**:

```razor
<ApplicationShell Title="My Application"
                  StartupScreenSettingsKey="MyCompany.MyApplication.Workspace"
                  BlockId="@BlockId" ScreenId="@ScreenId" InstanceId="@InstanceId" />
```

An ordinary root/blank route reads `ApplicationShell.StartupSettings` through `UserSettings` at the first interactive
render before choosing its default. Prerendering displays the workspace loading
content until storage is available. A route that explicitly specifies a screen
wins over the saved preference. Pass no implicit screen target for your root
route if it should honor the saved choice. Permanent screens still initialize.
Without a key the existing startup behavior is unchanged.

`ApplicationStartupScreen` stores plugin name, stable block/menu identity,
full screen type and simple assembly name. The host resolves this only against
its current registered definitions; it never loads a type from preference text.
Missing/obsolete records fall back without overwriting the browser's choice.
Storage failures are shown in the shell and do not replace an accepted choice.
The circuit-bound provider keeps this record local to the application and current browser/user; it does not save the component’s fields. The original untyped startup key remains a read-only fallback when no typed record exists.

`ApplicationScreen.CanRestoreAtStartup` can veto choosing a live screen.
Runtime activation parameters, keyed sessions, Empty screens and unattached
components are excluded automatically; registered menu parameters remain part
of the definition. Override the host's protected
`CanRestoreScreenAtStartup(block, definition)` when a definition must also be
excluded before constructing its renderer component, for example after an
application upgrade makes it require transient arguments. Custom hosts can use
`GetStartupScreenIdentity`, `SetStartupScreen`, `CanMakeStartupScreen`,
`GetStartupScreenUnavailableReason` and `IsStartupScreen` without adding another
screen registry. `ApplicationShell.MakeStartupScreenAsync(sessionId)` saves
through the same `SettingsObject.SaveAsync()` path as the context command.

The [cross-platform guide](../../docs/guidelines/patterns/grids-and-screen-hosts.md#application-screen-hosts)
maps all screen lifetimes, tabs, startup preferences and close/exit behavior to
WinForms. Native detached-window ownership and drag docking remain WinForms
features; Blazor retains components in its current document/circuit.

Permanent tabs have no close button; middle-click, Delete and tab context close commands preserve them. A direct `CloseScreenAsync`/`CloseScreensAsync` request containing a permanent session returns false without removing any session. Application-level Close all commands should select only closable normal sessions:

```csharp
await host.CloseScreensAsync(host.Screens
	.Where(screen => !screen.IsPermanent && screen.ScreenKind == ScreenKind.Normal), cancellationToken);
```

Administrative `UnregisterBlockAsync` may remove the block and its permanent screens after preflighting their guards. Re-registering it creates its permanent sessions again. Navigating away from the application still checks every attached screen, including permanent and hidden empty screens. The renderer owns component disposal throughout.

## Screen lifecycle and unsaved changes

Follow [application and screen lifetimes](../../docs/guidelines/patterns/application-ownership.md#application-and-screen-lifetimes)
for subscriptions, cancellation and shared workers. Keep screen activity in
activation/deactivation/disposal overrides and application-wide service startup
and shutdown in application initializers/finalizers. A retained hidden component
is still alive; a navigation guard only decides permission.

Override `CanDeactivateAsync(CancellationToken)` to veto screen switching, closure or workspace navigation. Override `HasUnsavedChanges` to enable the browser's confirmation when leaving the page. A render of the screen publishes a changed dirty state, so the shell updates its navigation lock; call the protected `NotifyScreenChanged()` when the state changes without a render of the screen.

Override `OnActivatedAsync`, `OnDeactivatedAsync` and `DisposeAsyncCore` for lifecycle work. If overriding `OnInitializedAsync`, await the base implementation so the component attaches to its session. The renderer creates and disposes screen components; never instantiate or dispose them through the host.

Host transitions are serialized. Lifecycle callbacks must not await another transition on the same host; menu actions run outside the transition gate and may navigate. Cancellation and guard vetoes leave the selection unchanged before commit. A post-commit activation failure is propagated with the selected screen still active. Unregistering a block preflights all its open screens before removing any session. `CanNavigateAsync` checks hidden screens too.

## Master-detail layouts and field rows

`UI.Controls.SplitContainer` shows two panels separated by a draggable splitter, like the WinForms SplitContainer. `Orientation` is `SplitOrientation.Horizontal` (the default, first panel above) or `Vertical` (first panel to the left). `Panel1` and `Panel2` are the panels; `SplitterPosition` is the first panel's share in percent (default 50) and binds two-way through `SplitterPositionChanged`. `Panel1MinSize` and `Panel2MinSize` (CSS pixels, default 40) bound the drag, and `SplitterWidth` (default 5) sets the splitter's thickness. The splitter is a focusable `separator`: arrow keys move it by 2 percent, 10 with Shift, and Home and End move it to the minimum sizes. With a `SettingsKey` the position is remembered for the browser's user through `UserSettings` as a `SplitContainerSettings`. Other attributes, such as a class or a style giving the container its height, are applied to the container element.

`UI.Controls.FieldGrid` lays out `FieldRow` children. `Layout` is `FieldLayout.Stacked` by default: each label above its control, the fields flowing across `Columns` equal columns (default 2, one on narrow screens), as on a record form. `FieldLayout.Inline` puts the labels in an aligned left column, reading "Label:" (the colon is added by the stylesheet with empty alternative text, so a control's accessible name stays the bare label), with the controls, sized to their values, on the right. A `FieldRow` with `For` labels its child control; without a child it shows `Value` as a read-only value labelled by its row. `Wide` makes a field, such as a text area, span every stacked column or stretch across the inline value column. `FieldGrid.Label` names the group, for example the record type of a form. `Compact`, true by default, tightens the gaps between fields (and, inline, trims Bootstrap controls to about a compact grid row); false uses the theme's control spacing. Use it for record forms, wizard steps and dialogs alike.

```razor
@using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls

<SplitContainer @bind-SplitterPosition="_split" Panel1MinSize="120" Panel2MinSize="120" SettingsKey="MyCompany.MyApplication.People.Split" style="height: 70vh">
	<Panel1>
		@* The power-user grid above, with Fill="true" and @bind-SelectedItem="_selected". *@
	</Panel1>
	<Panel2>
		@if (_selected != null) {
			<FieldGrid Label="Person">
				<FieldRow Label="Name" Value="@_selected.Name" />
				<FieldRow Label="Age" Value="@_selected.Age.ToString()" />
				<FieldRow Label="Notes" For="person-notes" Wide="true"><textarea id="person-notes" class="form-control form-control-sm" readonly>@_selected.Notes</textarea></FieldRow>
			</FieldGrid>
		}
	</Panel2>
</SplitContainer>

@code {
	private double _split = 55;
	private Person _selected;
}
```

The tester's wallet wizard steps and the New Product wizard of its Power-user records screen use the same rows for input: `<FieldRow Label="Wallet Name" For="wallet-name"><InputText id="wallet-name" ... /></FieldRow>`.

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

A dialog of the current generation is closed by its close button. What a click outside it, or the Escape key, does is its backdrop dismissal policy, `Modal.ModalOptions.BackdropDismissal`, which is `Close`, `Confirm` or `Ignore`. A wizard shown with `Views.WizardDialogAsync` or `ModalService.ShowAsync<BlazorWizardModal>` defaults to `Confirm`. It asks "Are you sure you want to close?" above the wizard, Yes cancels the wizard exactly as its close button does, and No returns to the wizard with its entries and focus intact. Escape is treated like the click outside, so it asks too. A wizard that cannot be cancelled shows no close button, is not asked about and stays open. Every other dialog defaults to `Close`, which keeps the earlier behavior. Pass the policy to change either default.

```csharp
await Views.WizardDialogAsync(wizard, new ModalOptions { BackdropDismissal = ModalBackdropDismissal.Ignore });
```

A custom dialog built on `UI.Dialogs.ModalTemplate` chooses its own default with the template's `DefaultBackdropDismissal` parameter. Dialogs of the original generation have no such policy, and a click outside them or Escape still closes them through their close button.

Removing a modal host while a dialog is pending completes the interaction with `ModalResult.Cancel`, including cancellation during rendering or JavaScript startup. It does not surface an unhandled task cancellation through the originating UI event. Unrelated errors and cancellations still propagate. This lifecycle cleanup is independent of whether the user is permitted to press Cancel.

`PagedTable<TItem>` displays an in-memory collection, `VirtualPagedTable<TItem>` uses an asynchronous items provider, and `RapidTable<TItem>` consumes an asynchronous stream. Choose the CRUD grid when you need persistence, validation and editing rather than display-only row templates.

## Existing components and migration

- `Components`, `ViewModels`, `Models`, `Services`: original MVVM components, tables and modal/wizard framework.
- `Plugins`: root-namespace `BlazorPlugin` definitions/builders.
- `Application`, `Grids`: application workspace contracts, builders and grid behavior in the root namespace.
- `Modal`, `Wizard`: dialog and wizard behavior in the corresponding domain namespaces.
- `UI`: application workspace, dialog, wizard, table and editable-grid rendering. Component-specific support types live beside their `.razor` files in the same namespace.

Both generations contain names such as `ModalHost` and `PagedTable`; import the generation locally or use fully qualified component tags. When hosting both dialog APIs in one layout, give their hosts distinct `Id` values (for example, `legacy-modal` and `modern-modal`) and initialize each corresponding service. The default ID remains `modal` for existing hosts. `AddSphere10BlazorPlugins<TPluginLocator>()` retains routed plugin/navigation registration through `IBlazorRoutedPluginLocator`. Reusable library layouts no longer contain sample profile, endpoint or placeholder menu entries.

`UI.Controls.BlazorGrid.BlazorGrid<TItem>` uses `IDataSource<TItem>` asynchronous CRUD. For a data source that updates a project draft, set SaveButtonText="Apply row" and EditNotice="Apply updates the draft." to distinguish row application from project persistence. Defaults preserve the existing Save button and notice. Component parameters are auto-properties synchronized through lifecycle methods; virtual paging rejects stale responses and streaming tables cancel/restart enumeration when the source changes. Wizards validate the final step and complete once.

The application types use `ApplicationBlock`, `ApplicationMenuBuilder`, `ApplicationMenuItemBuilder`, `BlazorPluginBuilder` and `ApplicationScreen` in this library's namespace. Platform application interfaces, sessions and hosts also omit the redundant `Blazor` prefix. Typed contracts and navigation coordination live in the shared Application library. Qualify shared lifecycle members as `SharedUI.IApplicationScreen` when using explicit interface implementations; `Sphere10.Framework.Web.AspNetCore.Blazor.IApplicationScreen` is the Blazor screen contract. The former routed `App`, `AppBlock` and `AppBlockPage` types use the distinct `BlazorRoutedApplication*` names above.

Replace old `Sphere10.Framework.DApp.Presentation*` namespaces with this library's namespace. Inject scoped modal/view services instead of using static services. `Application` is the scoped runtime projection of the screen host's live blocks and successfully loaded startup plugins. Resolve `IApplication` or the shared `IApplication` to obtain that same running instance. Read `Blocks` for runtime navigation and `LoadedPlugins` for startup metadata; the former runtime `Plugins` and `ActivePlugin` properties have been removed. Replace the former `Initialize(IServiceCollection)`/`AttachScreenHost` lifecycle with startup registration through `AddSphere10BlazorPlugin`; plugin configuration runs once at startup rather than once per circuit. The direct application constructor accepts an already-loaded plugin set and does not perform loading.

## Build, test and package

Run these commands from the repository root:

```shell
dotnet build src/Sphere10.Framework.Web.AspNetCore.Blazor/Sphere10.Framework.Web.AspNetCore.Blazor.csproj -c Release -p:BuildRevision=0
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0
```

The [test project README](../../tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/README.md) lists the grid fixtures and focused test commands. The [Blazor tester guide](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md) explains the full-window application demo. `/` and `/application` enter the same shell; component and legacy URLs select registered screens and resolve to the canonical `/application?block=...&screen=...` address. The old `/components/grid` and `/modern` URLs both open the CRUD grid tab. Theme, endpoint selection, commands and retained screen state belong to the same running host.

For isolated package-consumer verification, run `./scripts/validate-packages.ps1` from the Framework repository root. The [validation script](../../scripts/validate-packages.ps1) packs the public framework graph, builds external consumers and checks a published Blazor host and its Razor assets. It does not publish packages to a registry.

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
- [Sphere10.Framework.Windows.Forms](../Sphere10.Framework.Windows.Forms/README.md): desktop ApplicationBlock and VisualGrid counterparts.
- [Blazor tester](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md) and [regression tests](../../tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/README.md): runnable examples and verification.

## License

Distributed under the MIT License; see [LICENSE](../../LICENSE).

### Conditional wizard screens

Both wizard generations observe the shared wizard's `Changed` event, so a step can insert a temporary screen with `UpdateSteps` and navigate to it through Next. A step that creates its successor during validation should override the shared trait:

```csharp
public override WizardStepTraits Traits => WizardStepTraits.AssumeHasNext;
```

Import `Sphere10.Framework.Application.UI` for `WizardStepTraits`. Both Blazor step bases implement the shared `IWizardStep` contract. Hosts read traits from the actual rendered component through `StepTraitsResolver`; they do not construct extra components to inspect metadata. The flag makes `wizard.HasNext` true and presents Next before another step has been registered. Successful `OnNextAsync` validation must register the successor before navigation. If it does not, the wizard reports a navigation error and remains on the current step; the flag cannot advance beyond the registered steps or finish the wizard. An ordinary final step keeps `WizardStepTraits.None` and presents Finish. Traits can depend on the model and are read live. Replacing a wizard or disposing its host restores its preceding traits resolver.

The modern host invokes the current step's validation callback for both Next and Back. If that callback explicitly navigates the wizard (for example an Import step applying its preview and returning to the route screen with Previous), the host renders that destination without advancing a second time. Pending Back validation blocks competing navigation and cancellation; failed validation preserves the current screen.
