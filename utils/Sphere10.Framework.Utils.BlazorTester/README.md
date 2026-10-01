# Sphere10.Framework.Utils.BlazorTester

A .NET 10 Blazor Web App for trying Sphere10's components and ApplicationBlock workspace. It uses prerendered Interactive Server rendering and local sample data. No WebAssembly workload, database, backend node or WebSocket server is required.

Start with `/components/grid` for the editable CRUD grid, or `/application` for the application shell. See [the Blazor library guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) for the reusable APIs and host integration.

## Run the tester

Install the .NET 10 SDK and run these commands **from the repository root**. The first run restores the project references and NuGet dependencies.

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.BlazorTester/Sphere10.Framework.Utils.BlazorTester.csproj -c Release -p:BuildRevision=0
```

Wait for `Now listening on: http://localhost:5187`, then open [the grid demo](http://localhost:5187/components/grid) or [the ApplicationBlock workspace](http://localhost:5187/application). The command keeps the server running; stop it with **Ctrl+C**. `BuildRevision=0` supplies an explicit local build revision.

`dotnet run` does not promise to open a browser. In Visual Studio, set `Sphere10.Framework.Utils.BlazorTester` as the startup project and select its named launch profile. [launchSettings.json](Properties/launchSettings.json) configures the IDE to open `/components/grid` on port `5187` with the Development environment. Use **F5** or **Ctrl+F5**.

For an explicit HTTP address independent of the launch profile:

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.BlazorTester/Sphere10.Framework.Utils.BlazorTester.csproj -c Release -p:BuildRevision=0 --no-launch-profile -- --urls http://127.0.0.1:5188
```

Open `http://127.0.0.1:5188/components/grid`. To let the server choose an available port, replace `5188` with `0` and use the actual address printed in the terminal. Use `127.0.0.1` for dynamic port binding.

## Routes and project layout

| Route | What to try |
| --- | --- |
| `/` | Overview and links to the demonstrations. |
| `/components/grid` | Typed CRUD grid with buffered editing, validation, reference selection and an async action. |
| `/components/tables` | Paged, virtual and streaming tables. |
| `/components/dialogs` | Awaited information, confirmation and custom dialogs. |
| `/components/wizards` | Branching wallet wizard and step validation. |
| `/application` | Retained screens, guarded editor, independent scratchpads, scoped actions and hosted component demos. |
| `/modern` | Compatibility alias for `/components/grid`. |
| `/widget-gallery` | Clearly labelled original plugin gallery. |
| `/widget-gallery/modals` | Original information and confirmation dialogs. |
| `/widget-gallery/tables` | Original paged, virtual and streaming tables. |
| `/widget-gallery/wizards` | Original widget wizard, validation, dimensions and summary. |
| `/servers` | Add/select sample endpoints, synchronized with the top-left selector. |
| `/legacy/dashboard` | Original loader dashboard with illustrative statistics. |

| Location | Responsibility |
| --- | --- |
| [Program.cs](Program.cs) | Interactive Server registration, Sphere10 services, workspace registration, plugin samples and endpoint mapping. |
| [App.razor](App.razor), [Routes.razor](Routes.razor) | HTML document, styles/import map, render mode and page routing. |
| [Layouts/DemoLayout.razor](Layouts/DemoLayout.razor) | Shared sidebar with endpoint selector and bottom block icons, header search, right-aligned theme selector and both modal hosts. |
| [Layouts/DemoNavigation.cs](Layouts/DemoNavigation.cs) | Overview, Components, Workspace and Legacy examples links with icons, also supplying header search results. |
| [Pages](Pages) | Overview and dedicated component routes. |
| [Modern/UI/Index.razor](Modern/UI/Index.razor) | Grid route and `/modern` compatibility alias. |
| [Demos/GridDemo.razor](Demos/GridDemo.razor) | Reusable grid bindings, reference picker and asynchronous Details action. |
| [Demos/TablesDemo.razor](Demos/TablesDemo.razor), [Demos/DialogsDemo.razor](Demos/DialogsDemo.razor), [Demos/WizardsDemo.razor](Demos/WizardsDemo.razor) | Reusable examples shared by standalone pages and workspace screens. |
| [Modern/UI/Controls/TestClass.cs](Modern/UI/Controls/TestClass.cs) | Grid entity with enum, date, decimal, boolean, note and optional related record. |
| [Modern/UI/Controls/TestClassDataSource.cs](Modern/UI/Controls/TestClassDataSource.cs) | Local CRUD source, search, typed sorting, paging and validation. |
| [Loader/Sphere10Plugin.cs](Loader/Sphere10Plugin.cs), [WidgetGallery/WidgetGalleryPlugin.cs](WidgetGallery/WidgetGalleryPlugin.cs) | Fluent plugin definitions: service registrations, application blocks, menus and screens. |
| [Application](Application) | Retained workspace screens, scoped state and the thin nested workspace layout. |
| [Loader](Loader), [WidgetGallery](WidgetGallery) | Original routed pages, validators and endpoint services, configured by the same builders as the workspace. |
| [wwwroot](wwwroot) | Local Bootstrap base, icons and host assets. |

Every active route uses `DemoLayout`, including the original plugin pages and not-found page. The workspace composes this same layout and presents its screen menus inside the content area. The bottom sidebar icons select its registered application blocks from any demo route. Its screens host reusable demo components instead of embedding routable pages. The common shell owns one modal host per component generation with distinct IDs.

Reusable components, modal services, wizard frameworks, theme support and grid logic belong to [the Blazor library](../../src/Sphere10.Framework.Web.AspNetCore.Blazor).

## Classic blue, Blue, Light and Dark themes

The **Theme** selector in the upper-right header switches between **Classic blue**, **Blue**, **Light** and **Dark**. Classic blue restores the original loader's blue sidebar gradient, pale content background, white topbar, heading styling and `Roboto, Helvetica, Arial, sans-serif` font stack. Fonts use the first available installed face; the demo makes no external font request. The original brand and block SVG icons are reused. **Blue** restores the newer styling: a blue gradient on both the header and sidebar, the system font stack and normal heading weights. Use `ThemeMode.ClassicBlue` / `classic-blue` for the original appearance or `ThemeMode.Blue` / `blue` for the newer one. Classic blue remains the tester's default.

All themes share one presentation structure and the same services. Changing theme updates semantic CSS tokens, without recreating the current screen, discarding grid drafts or changing endpoint/block state. The single `ThemeProvider` also surrounds both modal hosts. The scoped theme service retains the choice during navigation in the current circuit; a full refresh starts a new circuit with Classic blue. The tester does not store a browser preference.

The restored shell provides:

- **Top-left Endpoint selector:** choose an existing sample endpoint, or open **Manage endpoints** to add one. The selector and `/servers` share the plugin-registered `IEndpointManager`, including changes in either direction.
- **Bottom-left application block icons:** the original Sphere10 and boxes images select **Workspace** and **Component gallery**. The dock consumes the framework's `ApplicationBlockMenu` and scoped screen host, with metadata supplied through the existing plugin/block builders. Workspace guards still block navigation while an editor has unsaved changes.
- **Header search:** the existing framework `SearchInput` searches the same route catalog used by the sidebar, including original examples. Results are real demo links. Escape dismisses the results.
- **Mobile navigation:** the **Menu** button expands or collapses page links; endpoints, block icons and the theme selector remain available.

The original dashboard, widget gallery, dialogs, tables and widget wizard remain accessible in **Legacy examples**. The old template's placeholder alerts/profile/logout links did not implement application services and are not presented as working features. The original standalone loader remains an inactive migration reference; its useful controls now run inside the current shell.

`App.razor` loads the neutral Bootstrap base, the library's `themes.css`, and the host/component styles. Individual pages do not load separate light/dark stylesheets. To reuse this arrangement, see [the library theme guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md).

Endpoint selection stores sample values for the current circuit and never contacts those endpoints. The wallet wizard demonstrates navigation and validation without creating or storing a wallet. Grid data belongs to the page instance and resets when it is recreated, including after a full browser refresh.

## Walk through the editable grid

Open `/components/grid` and find **Editable grid**. Complete or cancel the current edit before searching, paging, sorting or running a row action.

1. **Browse and search.** Enter `Bitcoin` and press **Search** or Enter. The source matches names, details and notes without case sensitivity. Press **Clear** to restore all records. Click **Name** or **Age** to change sort direction; use **First**, **Previous**, **Next**, **Last**, the page field or **Rows per page** to navigate. The source returns filtered counts and uses IDs to break sort ties.
2. **Select and edit.** Click a row, then **Edit**. A second single click deselects a selected row. Double-click a row, or focus the row and press **F2**, to start editing. The Active checkbox becomes editable only within an edit session. ID and the Summary template remain read-only.
3. **Compare Save and Cancel.** Change Name, Color, Created, Age, Active or Note. **Cancel**, or Escape while focus is in the grid row, discards the draft. Repeat the edit and choose **Save** to update the local source. The custom Note editor sends buffered values through `BlazorGridCellEditContext.ValueChanged`.
4. **Exercise validation.** Save an empty Name or Age `151`. The error remains visible, the record retains its prior values, and the draft stays available for correction. Enter a nonblank Name and an Age from `0` to `150`, then Save again.
5. **Create a record.** Choose **New**. The draft receives a sample ID but is not added to the source until Save succeeds. Cancel discards it. Clear any search before looking for a saved record; the existing query and sort still apply.
6. **Choose a related record.** While editing, open the related-record dropdown. Search or page through the eligible source, select a row, then choose **Use selected**. **Cancel** closes the picker without changing the buffered reference. **Clear related record** sets the optional reference to null. The outer grid's **Save** commits the change; its Cancel discards it. The picker permits reading, searching, sorting and paging, and assigns the selected object itself.
7. **Delete with validation.** Select a record and choose **Delete**, then **Delete item** in the confirmation area. Cancel the confirmation to keep it. A record named `Polkadot` is protected by source validation and remains visible with an error message.
8. **Run an asynchronous action.** Choose **Details** for a row and dismiss its dialog. The grid awaits the action before refreshing. The selection summary beneath the grid identifies the selected record.

Drag a column's header edge to check resizing in a browser. Name and Note share spare width while retaining their different starting widths. Narrow layouts scroll the grid horizontally; editors remain readable. The current page uses manual page sizing; to exercise automatic sizing, add `AutoPageSize="true" Height="420"` to its grid binding and check how complete rows fit when the viewport changes.

## Adapt the sample

Use the existing page and source as a starting point:

- Add properties to `TestClass` and typed definitions to `_columns` in `Demos/GridDemo.razor`. `BlazorGridColumn<TestClass>.For(item => item.Name)` creates direct property accessors. Use `CanEditCell = false` for a read-only column, `Template` for display, or `EditorTemplate` for a custom buffered editor.
- Keep reference display shallow. The Related record example passes the full eligible source and separate picker columns, then forwards its selected value through the edit context callback.
- Extend `TestClassDataSource.ReadRange` when adding searchable or sortable fields. Filter and sort before paging, return the actual zero-based `Page`, and set `TotalCount` to the filtered count. Plain `ListDataSource<T>` provides list CRUD and paging; application search and sorting are implemented by this sample override.
- Enforce create/update/delete rules in the source's validation and persistence methods. The grid combines source capabilities with `AllowedCapabilities`; commands and editors follow that intersection. An asynchronous external source can derive from `AsyncBatchDataSourceBase<T>` and implement its batch methods rather than duplicating sync and async plumbing.

For a read-only view, replace the grid binding in `Demos/GridDemo.razor` with the following, using its existing `_dataSource` and `_columns` fields:

```razor
<BlazorGrid TItem="TestClass" DataSource="_dataSource" Columns="_columns"
	AllowedCapabilities="@(DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage)"
	AllowCellEditing="false" PageSize="10" />
```

This is a source configuration example. It hides create/edit/delete commands while preserving the allowed browse operations. Source methods remain responsible for enforcing application permissions.

## ApplicationBlock workspace

[Sphere10Plugin.cs](Loader/Sphere10Plugin.cs) defines the Workspace block and its scoped state/endpoint services. [WidgetGalleryPlugin.cs](WidgetGallery/WidgetGalleryPlugin.cs) defines the Component gallery block and registers the original gallery's view models, random-number service and validator. Both use `BlazorPluginBuilder` → `BlazorApplicationBlockBuilder` → `BlazorApplicationMenuBuilder` → `BlazorApplicationMenuItemBuilder`, backed by the shared `Sphere10.Framework.Application.UI` models, validation and catalog. The WinForms tester consumes the same neutral foundation through native adapters.

The active host calls these definitions directly:

```csharp
services.AddSphere10Blazor()
	.AddSphere10BlazorPlugin(Sphere10Plugin.Configure)
	.AddSphere10BlazorPlugin(WidgetGalleryPlugin.Configure);
```

Each plugin owns the dependencies its screens and routed pages need. The active tester no longer constructs legacy `BlazorRoutedApplication`, `BlazorRoutedApplicationBlock` or `BlazorRoutedApplicationPage` graphs, uses a plugin locator, or manually calls `new BlazorRoutedPlugin().ConfigureServices(...)`. The routed navigation library remains covered by its dedicated compatibility tests. Hosted application screens derive from `BlazorApplicationScreen`; both platforms use the shared `Application.UI.ScreenActivationMode` enum.

The configuration has this shape, using the tester's existing screen and state types:

```csharp
services.AddSphere10BlazorPlugin(plugin => plugin
	.WithName("Blazor tester")
	.ConfigureServices(registry => registry.AddScoped<WorkspaceStatus>())
	.AddBlock(block => block
		.WithId("workspace")
		.WithName("Workspace")
		.WithIconUrl("img/heading-solid.svg")
		.WithDefaultScreen<OverviewScreen>()
		.AddMenu(menu => menu.WithId("work").WithText("Work").WithIcon("fas fa-desktop")
			.ConfigureItem(item => item.WithId("overview").WithText("Overview")
				.WithIcon("fas fa-home").WithScreen<OverviewScreen>()))));
```

Import `Microsoft.Extensions.DependencyInjection`, `Sphere10.Framework.Web.AspNetCore.Blazor` and `Sphere10.Framework.Utils.BlazorTester.Application` for this standalone example. In the running tester, use the two named definitions above rather than registering this additional workspace block; block IDs must remain unique. Original page URLs such as `/widget-gallery/wizards` and `/servers` are still handled by the Blazor router and use the services registered by those same plugins.

Open `/application` and try these flows:

- Increment the Overview counter, switch screens and return: the component instance is retained.
- Type in Guarded editor's Session note. Switching, closing and internal navigation are blocked until **Save in session** or **Discard changes**.
- Open multiple **New scratchpad** screens. Each has its own component, text and history entry.
- Run **Run scoped async action**. Its service belongs to the current circuit.
- Switch to **Component gallery**, then select **CRUD grid**, **Tables**, **Dialogs** or **Wizards**. Each screen hosts the same reusable example as its dedicated page and retains its own state.

The URL carries `block`, `screen` and `instance` query values. Back/Forward selects retained screens. A full refresh starts a fresh circuit and recreates the requested screen; sample edits are not persisted. See [the library guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) for screen lifecycle contracts and shell customization.

## Wizard walkthrough

Open `/widget-gallery/wizards` for the original widget wizard or `/components/wizards` for the wallet example. **Allow cancellation** is checked before opening either wizard. Clearing it creates a wizard with `WithCancellation(false)`: Cancel and the header close button are hidden, and Escape keeps the wizard open. Complete the wizard to leave this mode. A step may also opt out through `IsCancellable`; an application `OnCancelled` callback can still reject cancellation and display a specific validation message.

In **New Widget**, try Next with empty fields to see validation. Enter Name, Description and a positive Price. Select **Supply dimensions**, continue to Height and Length, then use Back to change the selection. The following steps update without duplicating the dimensions page. The summary offers Finish; only Finish adds a row to the example's table. Cancel or closing a cancellable wizard discards its unfinished model.

In **New Wallet**, enter a name and choose Standard for the password branch or Restored for the seed branch. Back lets you change branches. The final Create action reports the result in the page; no real wallet is stored. Both examples share responsive wizard footers and the active theme.

The launcher code lives in [WizardsViewModel.cs](WidgetGallery/Widgets/ViewModels/WizardsViewModel.cs) and [WizardsDemo.razor](Demos/WizardsDemo.razor). Their step components are in `WidgetGallery/Widgets/Components` and `Modern/UI/Wizard/Examples/NewWallet`. Both builder generations accept this configuration:

```csharp
builder.NewWizard("New record")
	.WithModel(model)
	.WithCancellation(true)
	.AddStep<DetailsStep>()
	.AddStep<SummaryStep>()
	.OnFinished(SaveAsync)
	.Build();
```

`WizardCancellationTests` verifies default/disabled cancellation, step policies, guarded Cancel/close requests and branching. Browser verification should additionally check keyboard dismissal, field and checkbox sizing, footer wrapping and navigation while a dialog is open.

## Verify changes

From the repository root:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0
```

For the grid and its sample source only:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~BlazorGrid|FullyQualifiedName~GridDemoDataSource"
```

The [test guide](../../tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/README.md) describes controller, conversion, rendering, source and interop coverage. `HtmlRenderer` tests inspect markup and managed callbacks. Complete the grid walkthrough in a real browser when changing interactions, keyboard behavior, layout or JavaScript; fake interop tests do not execute DOM code.

The active host uses import maps and fingerprinted static assets. Grid styles come through the host styles bundle; the grid imports its collocated JavaScript module on demand. Modal hosts import an isolated module. The active host loads bundled Bootstrap base styles from `wwwroot/vendor/bootstrap` and the shared theme system. Archived admin/modern stylesheets remain inactive migration references.

### Browser checks for the common shell

- Open the grid, tables, dialogs, wizards, workspace and an original plugin route. Confirm there is one common header, one theme selector and consistent navigation icons.
- Select Classic blue, Blue, Light and Dark from the top-right Theme control, then navigate to another example. Confirm the shell, form controls, tables and dialogs retain the selected theme.
- Start editing a grid row, change a value and change the theme. The draft and selection should survive; Cancel should still restore the original record.
- Open a modern dialog and an original dialog, one at a time. Verify focus, dismissal and colors, and confirm their content appears in the appropriate shared modal host.
- Add an endpoint on `/servers`, select it from the top-left dropdown and verify the page selection changes too. Navigate to another example and confirm the endpoint is retained.
- Use the bottom block icons to switch between Workspace and Component gallery. Check retained screens, browser history and the guarded editor alongside the shared navigation.
- Search for `grid` or `dashboard` in the header and follow a result. At a narrow width, use Menu to expose the same page links.
## Troubleshooting

| Symptom | Check |
| --- | --- |
| Nothing opened after running the project | Keep the terminal running, wait for `Now listening on`, and open its address plus `/components/grid` or `/application` yourself. Verify the IDE startup project/profile if using F5. |
| Address already in use | Stop the earlier tester instance with Ctrl+C, select another port, or use `--no-launch-profile -- --urls http://127.0.0.1:0` and read the assigned port. |
| Page appears but buttons do nothing | Inspect the terminal and browser console. Interactive Server needs the `_blazor` circuit; confirm `_framework/blazor.web.js` loads and the connection completes. |
| Grid is unstyled or resizing fails | Check the host `.styles.css` bundle and the grid `.razor.js` request in browser network tools. Preserve the stylesheet/import-map setup in `App.razor` when adapting the host. |
| Commands are disabled during an edit | Save or Cancel first. The draft intentionally blocks navigation and other row operations. |
| A saved record seems missing | Clear the search and check the current sort/page. New records are not forced into the currently visible page. |
| SDK/restore errors | Confirm `dotnet --info` lists a .NET 10 SDK and inspect the first build or NuGet restore error. Run commands from the repository root. |
| Build cannot replace the tester executable | Stop the running tester before rebuilding that configuration. |

## Retained migration scaffolding

The original `Loader`, `ModernLoader` and `WidgetGallery` project files, WebAssembly entry points and original static-asset copies remain as inactive migration references pending approval to remove superseded files. They are excluded from the active tester and are not solution projects. Use the root tester project named in the commands above.