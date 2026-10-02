# Sphere10.Framework.Utils.BlazorTester

A .NET 10 Blazor Web App that presents every component demo through one full-page ApplicationBlock shell. It uses prerendered Interactive Server rendering and local sample data. No WebAssembly workload, database, backend node or WebSocket server is required.

Open `/` for the Workspace block or `/components/grid` for the editable CRUD grid. Both use the same application shell; compatibility URLs select registered screens and become canonical `/application?block=...&screen=...&instance=...` URLs. See [the Blazor library guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) for the reusable APIs and host integration.

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

## Screen lifetime profiles

Screen menu entries open independent instances by default, including the component and legacy galleries. Overview and **Single-instance screen** explicitly retain their singleton lifetimes to demonstrate state retention; the empty and permanent profiles also declare their intended lifetimes.

Use File → Close all screens in the default tester to display the real **Empty workspace** screen without a tab. Enter a note, browse back to Workspace and open Overview, then close it. The default empty singleton retains both its note and instance ID.

Three named launch profiles make the policies visible:

| Launch profile | Configuration | What to try |
| --- | --- | --- |
| `Empty singleton` | `Demo:EmptyScreenLifetime=SingleInstance` | The empty screen's note survives opening and closing a normal screen. |
| `Empty instance` | `Demo:EmptyScreenLifetime=MultiInstance` | Each return to the empty workspace creates a new instance and clears its note. |
| `Permanent singleton` | `Demo:PermanentScreen=true` | A Permanent screen tab opens automatically, has no close button, and retains its note across Close all and View layout changes. |

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.BlazorTester -c Release -p:BuildRevision=0 --launch-profile "Empty instance"
```

With `--no-launch-profile`, the same settings can be supplied as `Demo__EmptyScreenLifetime` or `Demo__PermanentScreen` environment variables, or application arguments `--Demo:EmptyScreenLifetime=MultiInstance` / `--Demo:PermanentScreen=true`. The permanent profile intentionally never displays Empty workspace while its normal permanent screen is registered.

[ScreenPoliciesPlugin](Application/ScreenPoliciesPlugin.cs) registers these examples through the ordinary plugin and block builders. The Workspace default remains the initial screen because its plugin is registered first. The optional permanent block opens alongside it; no separate navigation or screen state is used by the demo.

## Routes and project layout

| Entry URL | Registered block / screen | What to try |
| --- | --- | --- |
| `/`, `/application` | Workspace / Overview | Retained counter, single/multiple screen instances, explicit switching guards and scoped actions. |
| `/components/grid`, `/modern` | Component gallery / CRUD grid | Buffered editing, validation, reference selection and async row actions. |
| `/components/tables` | Component gallery / Tables | Paged, virtual and streaming tables. |
| `/components/dialogs` | Component gallery / Dialogs | Awaited information, confirmation and custom dialogs. |
| `/components/wizards` | Component gallery / Wizards | Branching wallet wizard and step validation. |
| `/widget-gallery` | Legacy examples / Plugin gallery | Original plugin gallery. |
| `/widget-gallery/modals` | Legacy examples / Legacy dialogs | Original information and confirmation dialogs. |
| `/widget-gallery/tables` | Legacy examples / Legacy tables | Original paged, virtual and streaming tables. |
| `/widget-gallery/wizards` | Legacy examples / Legacy wizard | Widget validation, optional dimensions and summary. |
| `/servers` | Legacy examples / Endpoint sample | Add/select sample endpoints, synchronized with the sidebar selector. |
| `/legacy/dashboard` | Legacy examples / Legacy dashboard | Illustrative loader statistics. |

The single [Workspace.razor](Application/Workspace.razor) route component handles these URLs. Existing links remain usable, including the grid's **Related component demos** links. Navigation opens the target screen in the retained host and updates the URL; it does not replace the application shell.

| Location | Responsibility |
| --- | --- |
| [Program.cs](Program.cs) | Interactive Server registration, Sphere10 services, workspace registration, plugin samples and endpoint mapping. |
| [App.razor](App.razor), [Routes.razor](Routes.razor) | HTML document, styles/import map, render mode and page routing. |
| [Layouts/DemoLayout.razor](Layouts/DemoLayout.razor) | Theme provider and the two modal hosts; presentation is owned by the library ApplicationShell. |
| [Layouts/DemoNavigation.cs](Layouts/DemoNavigation.cs) | Compatibility URL mappings and header search over the actual registered application screens. |
| [Pages](Pages) | Component content reused by hosted screens. |
| [Modern/UI/Index.razor](Modern/UI/Index.razor) | Grid content and related-demo links, hosted by ComponentScreen. |
| [Demos/GridDemo.razor](Demos/GridDemo.razor) | Reusable grid bindings, reference picker and asynchronous Details action. |
| [Demos/TablesDemo.razor](Demos/TablesDemo.razor), [Demos/DialogsDemo.razor](Demos/DialogsDemo.razor), [Demos/WizardsDemo.razor](Demos/WizardsDemo.razor) | Reusable examples rendered inside registered application screens. |
| [Modern/UI/Controls/TestClass.cs](Modern/UI/Controls/TestClass.cs) | Grid entity with enum, date, decimal, boolean, note and optional related record. |
| [Modern/UI/Controls/TestClassDataSource.cs](Modern/UI/Controls/TestClassDataSource.cs) | Local CRUD source, search, typed sorting, paging and validation. |
| [Loader/Sphere10Plugin.cs](Loader/Sphere10Plugin.cs), [WidgetGallery/WidgetGalleryPlugin.cs](WidgetGallery/WidgetGalleryPlugin.cs) | Fluent plugin definitions: service registrations, application blocks, menus and screens. |
| [Application](Application) | Sole routable workspace, retained screen wrappers, command definitions and scoped state. |
| [Loader](Loader), [WidgetGallery](WidgetGallery) | Original content, validators and endpoint services; hosted by the Legacy examples block. |
| [wwwroot](wwwroot) | Local Bootstrap base, icons and host assets. |

`ApplicationShell` fills the viewport. Its application menu and toolbar span the full width above the sidebar and content. The left sidebar contains the endpoint selector, the selected block's menus, and bottom icons for **Workspace**, **Component gallery** and **Legacy examples**. Tabs sit above the scrollable screen body. Application, block and active-screen commands merge into the shared File/View/Help menu area and toolbar.

`DemoLayout` supplies theme and modal services around that shell. It does not add another header, sidebar or navigation list. There is one modal host per component generation, with distinct IDs. The not-found page uses the same providers without creating a nested application shell.

Reusable components, modal services, wizard frameworks, theme support and grid logic belong to [the Blazor library](../../src/Sphere10.Framework.Web.AspNetCore.Blazor).

## Toolbar and local demo identity

`Workspace.razor` places the framework `SearchInput`, `ThemeSelector` and [DemoIdentity](Application/DemoIdentity.razor) in `ApplicationShell.ToolBarContent`. They share the same compact toolbar as application commands; the shell's `Header` slot remains available for branding. The controls wrap at narrow widths without adding another application shell.

`DemoIdentity` supplies the reusable `IdentityControl` with a standard `ClaimsPrincipal`: **Demo user**, `demo@sphere10.local`, and the **Tester** role, labelled **Local demo account**. Open the identity menu and choose **Profile** to view those details in the existing framework information dialog. **Sign out of demo** replaces only the sample principal with an anonymous **Guest** identity; **Use demo account** restores it. Screens, tabs, endpoint choices, themes and buffered grid edits stay intact. These actions demonstrate identity presentation and callbacks; they do not contact an authentication backend or change a real signed-in session. A page refresh restores the initial demo account.

Escape or clicking outside dismisses the dropdown and returns focus to its trigger. The dropdown uses ordinary framework menu item builders and async command execution. Applications can bind the library control to their own authentication provider and use its rendering templates and selection hooks; see [the reusable control guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md).

## Classic blue, Blue, Light and Dark themes

The **Theme** selector in the upper-right application toolbar switches between **Classic blue**, **Blue**, **Light** and **Dark**. Classic blue restores the original loader's blue sidebar gradient, pale content background, white topbar, heading styling and `Roboto, Helvetica, Arial, sans-serif` font stack. Fonts use the first available installed face; the demo makes no external font request. The original brand and block SVG icons are reused. **Blue** restores the newer styling: a blue gradient on both the header and sidebar, the system font stack and normal heading weights. Use `ThemeMode.ClassicBlue` / `classic-blue` for the original appearance or `ThemeMode.Blue` / `blue` for the newer one. Classic blue remains the tester's default.

All themes share one presentation structure and the same services. Changing theme updates semantic CSS tokens, without recreating the current screen, discarding grid drafts or changing endpoint/block state. The single `ThemeProvider` also surrounds both modal hosts. The scoped theme service retains the choice during navigation in the current circuit; a full refresh starts a new circuit with Classic blue. The tester does not store a browser preference.

The restored shell provides:

- **Top-left Endpoint selector:** choose an existing sample endpoint, or open **Manage endpoints** to add one. The selector and `/servers` share the plugin-registered `IEndpointManager`, including changes in either direction.
- **Bottom-left application block icons:** select **Workspace**, **Component gallery** or **Legacy examples**. The library shell uses `ApplicationBlockMenu` and its scoped screen host, with metadata from the existing plugin/block builders. Choosing a block only changes the sidebar menus; the active screen, tabs, URL and history stay unchanged. Screen menu items and tabs still honor the active screen's switching guard.
- **Toolbar search:** the framework `SearchInput` searches registered screens in the current host, including original examples. Results use canonical application URLs. Escape dismisses the results.
- **Mobile navigation:** **Menu** opens the selected block sidebar and icon dock over the screen. Browsing a block leaves the overlay open so a screen can be chosen; selecting a screen closes it. Theme, search and the identity menu remain in the compact application toolbar.

The original dashboard, widget gallery, dialogs, tables and widget wizard remain accessible in **Legacy examples**. The identity menu now uses the framework control and real local demo actions; the old template's placeholder alerts remain omitted. The original standalone loader remains an inactive migration reference; its useful controls now run inside the current shell.

`App.razor` loads the neutral Bootstrap base, the library's `themes.css`, and the host/component styles. Individual pages do not load separate light/dark stylesheets. To reuse this arrangement, see [the library theme guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md).

Endpoint selection stores sample values for the current circuit and never contacts those endpoints. The wallet wizard demonstrates navigation and validation without creating or storing a wallet. Grid data belongs to its retained screen instance: browsing blocks, switching tabs or following compatibility links preserves it. Closing that screen or refreshing the browser creates a fresh sample.

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

The active host calls these definitions directly, followed by the screen-policy plugin. The `permanentScreen` and `emptyScreenLifetime` values come from the selected launch profile:

```csharp
services.BuildBlazorApplication()
	.WithTitle("Sphere10 Blazor demos")
	.WithFavicon("img/logo.svg", "image/svg+xml")
	.ConfigureApplication(WorkspaceCommands.Configure)
	.AddPlugin(Sphere10Plugin.Configure)
	.AddPlugin(WidgetGalleryPlugin.Configure)
	.AddPlugin(plugin => ScreenPoliciesPlugin.Configure(plugin, permanentScreen, emptyScreenLifetime))
	.Build();
```

`BlazorApplicationOptions` supplies the configured title and favicon to `App.razor` and the workspace. The host resolves `img/logo.svg` through its fingerprinted static assets; there is one SVG favicon link in the document head.

`BlazorApplicationBuilder` registers immutable singleton branding options and uses the existing plugin and application registration APIs. [App.razor](App.razor) resolves those options for the document title and SVG favicon, using `Assets` to map the icon path. [Workspace](Application/Workspace.razor) uses the same configured title for `PageTitle` and `ApplicationShell`. Change `WithTitle`/`WithFavicon` in the builder to brand the host; no scoped application is resolved to render the head.

Each plugin owns the dependencies its registered screens and their content need. The active tester no longer constructs legacy `BlazorRoutedApplication`, `BlazorRoutedApplicationBlock` or `BlazorRoutedApplicationPage` graphs, uses a plugin locator, or manually calls `new BlazorRoutedPlugin().ConfigureServices(...)`. The routed navigation library remains covered by its dedicated compatibility tests. Hosted application screens derive from `BlazorApplicationScreen`; both platforms use the shared `Application.UI.ScreenActivationMode` enum.

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
		.AddMenu(menu => menu.WithId("work").WithText("Screen hosting").WithIcon("fas fa-desktop")
			.ConfigureItem(item => item.WithId("overview").WithText("Overview")
				.WithIcon("fas fa-home").WithScreen<OverviewScreen>()))));
```

Import `Microsoft.Extensions.DependencyInjection`, `Sphere10.Framework.Web.AspNetCore.Blazor` and `Sphere10.Framework.Utils.BlazorTester.Application` for this standalone example. In the running tester, use the three plugin definitions above rather than registering this additional workspace block; block IDs must remain unique. Original URLs such as `/widget-gallery/wizards` and `/servers` select the corresponding Legacy examples screens through Workspace. Their content uses services registered by those same plugins.

Open `/application` and try these flows:

- Increment the Overview counter, switch screens and return: the component instance is retained.
- Choose **Screen hosting → Single-instance screen**. Change its **Tab title** and increment its instance counter, leave and choose the same menu again: its visible instance ID and state remain unchanged. Typing in **Session note** prevents switching or closing until **Save in session** or **Discard changes**.
- Choose **Screen hosting → New screen instance** more than once, or **New scratchpad** in File/the toolbar. Each instance has a different visible ID, its own notes, counter, tab title and history entry. Switch between them to compare retained state, then reorder a tab by dragging or with Ctrl+Shift+Home/End.
- Check **Prevent tab switching** on either screen type. Browse other blocks using the bottom icons: only the left menus change. Selecting a different screen or tab, or closing this instance, is vetoed until the checkbox is cleared. The checkbox belongs to that instance; Save/Discard does not clear it, and scratchpad notes alone do not block switching.
- Select tabs with the arrow keys or Home/End. Close with its button, middle-click or Delete; right-click or Shift+F10 exposes Close, Close other screens and Close all screens. A dirty editor vetoes a batch before any tab closes.
- Use **View → Tabbed screens** or **View → Single screen** to change screen layout. Overview retains its counter when hidden in Single screen; leaving a scratchpad releases that multi-instance screen.
- In the top **File** menu and toolbar, **Application command** changes to **Save in session** while Single-instance screen is active. **Workspace action** remains a block contribution. Save there, switch back to Overview and verify the application command returns without duplicates; Help stays last.
- Run **Run scoped async action**. Its service belongs to the current circuit.
- Switch to **Component gallery**, then select **CRUD grid**, **Tables**, **Dialogs** or **Wizards**. Each screen hosts its reusable demo content and retains its own state. The **Legacy examples** block exposes the original gallery, dialogs, tables, wizard, endpoints and dashboard in the same host.

The URL carries the active screen's `block`, `screen` and `instance` query values. Browsing a different block does not change these values or create history entries. Back/Forward selects retained screens and restores their owning block menus; merged commands also continue to follow the active screen while another block is being browsed. A full refresh starts a fresh circuit and recreates the requested screen; sample edits are not persisted. See [the library guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) for screen lifecycle contracts and shell customization.

## Wizard walkthrough

Open `/widget-gallery/wizards` for the original widget wizard or `/components/wizards` for the wallet example. **Allow cancellation** is checked before opening either wizard. Clearing it creates a wizard with `WithCancellation(false)`: Cancel and the header close button are hidden, and Escape keeps the wizard open. Complete the wizard to leave this mode. A step may also opt out through `IsCancellable`; an application `OnCancelled` callback can still reject cancellation and display a specific validation message.

In **New Widget**, try Next with empty fields to see validation. Enter Name, Description and a positive Price. Select **Supply dimensions**, continue to Height and Length, then use Back to change the selection. The following steps update without duplicating the dimensions page. The summary offers Finish; only Finish adds a row to the example's table. Cancel or closing a cancellable wizard discards its unfinished model.

In **New Wallet**, enter a name and choose Standard for the password branch or Restored for the seed branch. Back lets you change branches. The final Create action reports the result in the page; no real wallet is stored. Both examples share responsive wizard footers and the active theme.

Both platform adapters now share the navigation and completion workflow in `Sphere10.Framework.Application.UI`. Blazor owns the rendered step validation and dialog presentation; its `BlazorWizardBuilder<TModel>` reuses the common builder state.

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

`WizardBrowserTests` executes both demos through required-field validation, non-cancellable Escape handling, completion, cancellable reopening and focus restoration. `ApplicationTabsBrowserTests` exercises singleton reuse, independent instance IDs/state, per-instance switching locks, tabs, command merging and screen modes.

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

- Open the grid, tables, dialogs, wizards, workspace and an original alias URL. Confirm one full-page shell: File/View/Help and the toolbar span the top, active block menus and icons sit left, and tabs sit above the screen body.
- Select Classic blue, Blue, Light and Dark from the top-right Theme control, then navigate to another example. Confirm the shell, form controls, tables and dialogs retain the selected theme.
- Open **Demo user**, dismiss with Escape and an outside click, then use **Profile** and close the information dialog. Sign out of the local demo and use **Guest → Use demo account** while editing a grid row; confirm the same draft, tab and URL remain.
- Start editing a grid row, change a value and change the theme. Browse another block (the grid should stay active), then open one of its screens and return via the grid tab, or follow a Related component demos link. The same draft and selection should survive; Cancel should restore the original record.
- Open a modern dialog and an original dialog, one at a time. Verify focus, dismissal and colors, and confirm their content appears in the appropriate shared modal host.
- Add an endpoint on `/servers`, select it from the top-left dropdown and verify the page selection changes too. Navigate to another example and confirm the endpoint is retained.
- Browse all three blocks using the bottom icons; verify that only the left menus change and the active screen URL stays fixed. Open screens from those menus to check retained state, history and merged screen commands.
- Compare the same single-instance screen reopened twice with two newly opened multi-instance screens. Lock one instance with **Prevent tab switching**, verify menu/tab selection is blocked while block browsing works, then unlock and retry.
- Verify the browser title is **Sphere10 Blazor demos** and the tab icon uses the configured Sphere10 SVG logo.
- Search for `grid` or `dashboard` in the toolbar and follow a result. At a narrow width, use Menu to expose the active block menus and dock.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Nothing opened after running the project | Keep the terminal running, wait for `Now listening on`, and open its address plus `/components/grid` or `/application` yourself. Verify the IDE startup project/profile if using F5. |
| Address already in use | Stop the earlier tester instance with Ctrl+C, select another port, or use `--no-launch-profile -- --urls http://127.0.0.1:0` and read the assigned port. |
| Page appears but buttons do nothing | Inspect the terminal and browser console. Interactive Server needs the `_blazor` circuit; confirm `_framework/blazor.web.js` loads and the connection completes. |
| Grid is unstyled or resizing fails | Check the host `.styles.css` bundle and the grid `.razor.js` request in browser network tools. Preserve the stylesheet/import-map setup in `App.razor` when adapting the host. |
| Grid commands are disabled during an edit | Save or Cancel first. The buffered edit blocks grid paging/search/sort and other row operations; shell tab state is retained separately. |
| A screen will not switch or close | Save or discard its unsaved note and clear **Prevent tab switching** on the active instance. Browsing another block only changes its sidebar menus. |
| A saved record seems missing | Clear the search and check the current sort/page. New records are not forced into the currently visible page. |
| SDK/restore errors | Confirm `dotnet --info` lists a .NET 10 SDK and inspect the first build or NuGet restore error. Run commands from the repository root. |
| Build cannot replace the tester executable | Stop the running tester before rebuilding that configuration. |

## Retained migration scaffolding

The original `Loader`, `ModernLoader` and `WidgetGallery` project files, WebAssembly entry points and original static-asset copies remain as inactive migration references pending approval to remove superseded files. They are excluded from the active tester and are not solution projects. Use the root tester project named in the commands above.