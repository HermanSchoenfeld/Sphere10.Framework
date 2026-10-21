# Sphere10.Framework.Utils.BlazorTester

A .NET 10 Blazor Web App that presents every component demo through one full-page ApplicationBlock shell. It uses prerendered Interactive Server rendering and local sample data. No WebAssembly workload, database, backend node or WebSocket server is required.

Open `/` for the organized tour or `/components/grid` for the editable CRUD grid. The **Application patterns** block adds complete card, menu, workflow and theme examples alongside the component reference and preserved legacy examples. Every entry uses the same application shell; compatibility URLs select registered screens and become canonical `/application?block=...&screen=...&instance=...` URLs. See [the Blazor library guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) for the reusable APIs and host integration.

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

## Plugin initialization and shutdown

All four demo plugins declare `WithName(...)` before registering their initializer and finalizer through `RegisterInitializer` and `RegisterFinalizer`: **Sphere10.Framework**, **Widget Gallery**, **Application patterns** and **Screen policies**. The two [lifecycle task types](Lifecycle) create eight task instances per application, reuse `AsyncApplicationInitializerBase` and `AsyncApplicationFinalizerBase`, log through `IApplication.Logger`, and simulate short startup and shutdown work. The tester supplies `ConsoleLogger` when no application logger is registered, so the terminal displays both phases. Each plugin registers its own instances under its name, even though they share task types. Both type and factory registration overloads throw immediately if `WithName(...)` has not been called; loading also rejects duplicate plugin names.

Opening an interactive browser circuit awaits application initialization. The footer then shows **Plugins initialized: 4**. Prerendering does not run the tasks, and reconnecting to the same circuit does not repeat them. File → Exit application awaits all four finalizers before showing terminal content; closing the circuit also finalizes its application. The completion names are recorded in the circuit's scoped `WorkspaceStatus`. Another browser circuit has its own tasks and status, while the plugin registrations and web server remain shared.

`DemoPluginLifecycleTests` exercises the running tester's `Program.ConfigureServices`, task keys, startup footer, independent circuits, reconnects and once-only exit/finalization.

## Screen lifetime profiles

Screen menu entries open independent instances by default, including the component and legacy galleries. Overview, **Single-instance screen**, and the four Application patterns screens explicitly retain their singleton lifetimes. Collection editors use an instance key so each collection has its own retained tab; the empty and permanent profiles also declare their intended lifetimes.

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
| `/patterns/cards` | Application patterns / Cards & collections | Artwork cards, shared searchable/paged data source, table selection, keyed retained editors and Save/Discard guards. |
| `/patterns/menus` | Application patterns / Menus & commands | Merged File/Tools, nested Export, enabled/hidden commands and preview dialogs. |
| `/patterns/wizards` | Application patterns / Guided workflows | The same validated conditional workflow in an embedded host or a dialog, with a preset detour and review. |
| `/patterns/themes` | Application patterns / Theme studio | All catalogue presets applied to a complete dashboard, selectable chart, editable form and dialog. |
| `/components/grid`, `/modern` | Component gallery / CRUD grid | Buffered editing, validation, reference selection and async row actions. |
| `/components/tables` | Component gallery / Tables | Paged, virtual and streaming tables. |
| `/components/dialogs` | Component gallery / Dialogs | Awaited information, confirmation and custom dialogs. |
| `/components/wizards` | Component gallery / Wizards | Guided workspace setup plus the retained branching wallet wizard and step validation. |
| `/components/themes` | Component gallery / Themes | Full semantic component palette, form states, navigation, tables and dialogs. |
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
| [Demos/GridDemo.razor](Demos/GridDemo.razor) | Editable CRUD/reference grid plus an independent live-snapshot workspace for paging, retained column layout, no-indicator refresh, row refresh/removal and filtered export. |
| [Modern/UI/Controls/GridDemoSnapshotDataSource.cs](Modern/UI/Controls/GridDemoSnapshotDataSource.cs) | Local immutable sample rows over core's refreshable snapshot source; replaces identities without native collection or external services. |
| [Demos/TablesDemo.razor](Demos/TablesDemo.razor), [Demos/DialogsDemo.razor](Demos/DialogsDemo.razor), [Demos/WizardsDemo.razor](Demos/WizardsDemo.razor) | Reusable examples rendered inside registered application screens. |
| [Modern/UI/Controls/TestClass.cs](Modern/UI/Controls/TestClass.cs) | Grid entity with enum, date, decimal, boolean, note and optional related record. |
| [Modern/UI/Controls/TestClassDataSource.cs](Modern/UI/Controls/TestClassDataSource.cs) | Local CRUD source, search, typed sorting, paging and validation. |
| [Loader/Sphere10Plugin.cs](Loader/Sphere10Plugin.cs), [WidgetGallery/WidgetGalleryPlugin.cs](WidgetGallery/WidgetGalleryPlugin.cs) | Fluent plugin definitions: service registrations, application blocks, menus and screens. |
| [Lifecycle](Lifecycle) | Plugin-owned startup and shutdown tasks, registered by all four plugin definitions and executed by the circuit's application. |
| [Application](Application) | Sole routable workspace, retained screen wrappers, command definitions and scoped state. |
| [Showcase](Showcase/README.md) | Composed application patterns, their plugin registration, isolated sample data and public-API recipes. |
| [Loader](Loader), [WidgetGallery](WidgetGallery) | Original content, validators and endpoint services; hosted by the Legacy examples block. |
| [wwwroot](wwwroot) | Local Bootstrap base, icons and host assets. |

`ApplicationShell` fills the viewport. Its application menu and toolbar span the full width above the sidebar and content. The left sidebar contains the endpoint selector, the selected block's menus, and bottom icons for **Workspace**, **Component gallery**, **Application patterns** and **Legacy examples**, plus the configured screen-policy blocks. Tabs sit above the scrollable screen body. Application, block and active-screen commands merge into the shared File/View/Help menu area and toolbar.

The runtime hierarchy is **Application → Blocks → Menus/screens**. `IApplication.Blocks` follows the live host, and `LoadedPlugins` records the four successfully loaded startup contributors. **Widget Gallery** contributes both **Component gallery** and **Legacy examples**, with an independent navigation icon and menu for each block. The default profile has five block icons; enabling the optional permanent-screen block adds a sixth.

Runtime blocks expose their circuit application through `Parent` and their original loaded plugin through `Source`. A menu's `Parent` is its block, a command's `Parent` is its menu, and attached screens expose `Host` and `Block`. `Block.ScreenDefinitions` owns screen policy and each definition's `Activators` identifies its commands. Screen menu graphs stay with their component instance; command predicates continue to reflect current state, while diagnostics visibility rebuilds its affected menu graph. Closing or detaching a component clears its runtime ownership without changing renderer disposal.

Ordinary commands execute through their canonical command objects. [WorkspaceCommands](Application/WorkspaceCommands.cs) retains the runtime scratchpad activator and its owning block; [CardsScreen](Showcase/Cards/CardsScreen.razor) opens a canonical definition through its own `Host` and `Block`. Block/screen IDs are resolved at the incoming route boundary and used to generate links, rather than passed around for command execution. Application-owned resources belong in `IApplication.Disposables`, and an attached screen can log through `Block.Parent.Logger`.

File → Exit application awaits the scoped application's `ExitAsync` confirmation and screen/block permission checks. Declining keeps the workspace available. Accepting awaits application-local finalization and shows terminal content while the web server and shared plugin registrations continue running. Refreshing the page starts a new circuit; exit does not redirect or stop the server.

`DemoLayout` supplies theme and modal services around that shell. It does not add another header, sidebar or navigation list. There is one modal host per component generation, with distinct IDs. The not-found page uses the same providers without creating a nested application shell.

Reusable components, modal services, wizard frameworks, theme support and grid logic belong to [the Blazor library](../../src/Sphere10.Framework.Web.AspNetCore.Blazor).

## Toolbar and local demo identity

`Workspace.razor` places the framework `SearchInput`, `ThemeSelector` and [DemoIdentity](Application/DemoIdentity.razor) in `ApplicationShell.ToolBarContent`. They share the same compact toolbar as application commands; the shell's `Header` slot remains available for branding. The controls wrap at narrow widths without adding another application shell.

`DemoIdentity` supplies the reusable `IdentityControl` with a standard `ClaimsPrincipal`: **Demo user**, `demo@sphere10.local`, and the **Tester** role, labelled **Local demo account**. Open the identity menu and choose **Profile** to view those details in the existing framework information dialog. **Sign out of demo** replaces only the sample principal with an anonymous **Guest** identity; **Use demo account** restores it. Screens, tabs, endpoint choices, themes and buffered grid edits stay intact. These actions demonstrate identity presentation and callbacks; they do not contact an authentication backend or change a real signed-in session. A page refresh restores the initial demo account.

Escape or clicking outside dismisses the dropdown and returns focus to its trigger. The dropdown uses ordinary framework menu item builders and async command execution. Applications can bind the library control to their own authentication provider and use its rendering templates and selection hooks; see [the reusable control guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md).

## Template themes

The **Theme** selector in the upper-right application toolbar exposes all 20 framework presets: Classic blue, Blue, Light, Dark, Artificial Reason (blue, green, orange and violet), Boost, HUD (dark and light), Landkit, MacAdmin, MacBeath, MacKart, Raleway (cyan, purple and green), and AppStack (light and dark). Classic blue remains the default and retains the original white topbar and blue sidebar. Blue retains the newer blue header/sidebar appearance.

Each template supplies its own colors, typography preferences, corner treatments, shadows and chrome through shared framework tokens. [The source audit](../../docs/theming-template-sources.md) identifies the owned template distributions and font fallbacks. Fonts use locally available faces; the demo makes no external font request.

All themes share one presentation structure and the same services. Changing theme updates semantic CSS tokens, without recreating the current screen, discarding grid drafts or changing endpoint/block state. The single `ThemeProvider` also surrounds both modal hosts. The scoped theme service retains the choice during navigation in the current circuit; a full refresh starts a new circuit with Classic blue. The tester does not store a browser preference.

The restored shell provides:

- **Top-left Endpoint selector:** choose an existing sample endpoint, or open **Manage endpoints** to add one. The selector and `/servers` share the plugin-registered `IEndpointManager`, including changes in either direction.
- **Bottom-left application block icons:** select **Workspace**, **Component gallery**, **Application patterns** or **Legacy examples**. The library shell uses `ApplicationBlockMenu` and its scoped screen host, with metadata from the existing plugin/block builders. By default, choosing a block changes its sidebar menus while retaining the active screen. Overview's **Activate the block's default screen** checkbox demonstrates the alternate activation mode. Screen changes and tabs honor the active screen's switching guard.
- **Toolbar search:** the framework `SearchInput` searches registered screens in the current host, including original examples. Results use canonical application URLs. Escape dismisses the results.
- **Mobile navigation:** **Menu** opens the selected block sidebar and icon dock over the screen. Browsing a block leaves the overlay open so a screen can be chosen; selecting a screen closes it. Theme, search and the identity menu remain in the compact application toolbar.

The original dashboard, widget gallery, dialogs, tables and widget wizard remain accessible in **Legacy examples**. The identity menu now uses the framework control and real local demo actions; the old template's placeholder alerts remain omitted. The original standalone loader remains an inactive migration reference; its useful controls now run inside the current shell.

`App.razor` loads the neutral Bootstrap base and host/component styles. `ThemeProvider` loads the selected template's single CSS file, which imports the common `themes.css` integration. Switching families replaces that link; color variants share their family file. Individual pages do not add theme bundles. To reuse this arrangement, see [the library theme guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md).

Endpoint selection stores sample values for the current circuit and never contacts those endpoints. The wallet wizard demonstrates navigation and validation without creating or storing a wallet. Grid data belongs to its retained screen instance: browsing blocks, switching tabs or following compatibility links preserves it. Closing that screen or refreshing the browser creates a fresh sample.

## Walk through the editable grid

Open `/components/grid` and find **Editable grid**. Complete or cancel the current edit before searching, paging, sorting or running a row action.

1. **Browse and search.** Enter `Bitcoin` and press **Search** or Enter. The source matches names, details and notes without case sensitivity. Press **Clear** to restore all records. Click **Name** or **Age** to change sort direction; use the **First**, **Previous**, **Next**, **Last** arrow icons (with tooltips), the page field or **Rows per page** dropdown to navigate. The sample configures choices through `PageSizeOptions`; custom active sizes remain selectable. The source returns filtered counts and uses IDs to break sort ties.
2. **Select and edit.** Click a row, then **Edit**. A second single click deselects a selected row. Double-click a row, or focus the row and press **F2**, to start editing. The Active checkbox becomes editable only within an edit session. ID and the Summary template remain read-only.
3. **Compare Save and Cancel.** Change Name, Color, Created, Age, Active or Note. **Cancel**, or Escape while focus is in the grid row, discards the draft. Repeat the edit and choose **Save** to update the local source. The custom Note editor sends buffered values through `BlazorGridCellEditContext.ValueChanged`.
4. **Exercise validation.** Save an empty Name or Age `151`. The error remains visible, the record retains its prior values, and the draft stays available for correction. Enter a nonblank Name and an Age from `0` to `150`, then Save again.
5. **Create a record.** Choose **New**. The draft receives a sample ID but is not added to the source until Save succeeds. Cancel discards it. Clear any search before looking for a saved record; the existing query and sort still apply.
6. **Choose a related record.** While editing, open the related-record dropdown. Search or page through the eligible source, select a row, then choose **Use selected**. **Cancel** closes the picker without changing the buffered reference. **Clear related record** sets the optional reference to null. The outer grid's **Save** commits the change; its Cancel discards it. The picker permits reading, searching, sorting and paging, and assigns the selected object itself.
7. **Delete with validation.** Select a record and choose **Delete**, then **Delete item** in the confirmation area. Cancel the confirmation to keep it. A record named `Polkadot` is protected by source validation and remains visible with an error message.
8. **Run an asynchronous action.** Choose **Details** for a row and dismiss its dialog. The grid awaits the action before refreshing. The selection summary beneath the grid identifies the selected record.

The separate **Live snapshots and layout** section exercises the reusable grid behavior without changing the editable records above:

1. **Compare paging modes.** Its footer starts in **Auto Paged**, measuring complete rows in the 360 CSS-pixel viewport. Resize the window or change browser zoom, then navigate. **Paged** restores the manual Rows per page size. **Single Paged** shows all matching samples with scrolling and hides the navigator; search/sort still apply.
2. **Check preferred stretching and retained widths.** Name and Details share spare width. Turn **Stretch Name and Details** off to compare. Drag a header edge, move to a later page containing longer descriptions, search and publish a new snapshot: the chosen width remains. Sorting reserves room for its indicator and cycles ascending, descending and natural order.
3. **Compare loading behavior.** Choose **Publish snapshot** or enable **Live captures**. The simulated source read waits 350 ms during the actual grid refresh. **No-indicator updates** starts enabled; existing rows remain usable while that read is pending. Turn it off to compare ordinary loading. Both paths retain the same initial loading surface. Live captures repeat every two seconds and stop when the screen is disposed.
4. **Refresh or remove one row.** Select a sample, then **Refresh selected row** to increment only its revision through the source's row-refresh API. **Remove selected row** updates this local source and removes its visible row without refreshing every other item. Full captures replace row objects while retaining their IDs and selection.
5. **Preview the current filter across pages.** Search for `Network`, navigate to another page, then choose **Preview filtered snapshot**. The inline preview contains every matching row in current sort order, keeping the grid's current page and selection. It demonstrates the data capture contract; the application chooses its file format and download UI.
6. **Inspect presentation and persistence.** Change **Title alignment**, **Image alignment** and **Image layout**. Image behavior consumes the shared Framework resize enums through browser layout. Both grids opt into independent browser settings keys; their paging/filter/sort state and manually chosen widths survive reloading this local origin. Clear site storage to restore untouched defaults. Live captures, preview output and the sample rows themselves are not persisted.

Drag a column's header edge in either grid to check resizing in a browser. The editable grid's Name and Note share spare width while retaining their different starting widths. Narrow layouts scroll horizontally and keep editors readable. Its footer also switches between the three paging modes; the independent live grid starts with automatic viewport sizing so no source edits are required to exercise it.

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

[Sphere10Plugin.cs](Loader/Sphere10Plugin.cs) defines the Workspace block and its scoped state/endpoint services. [WidgetGalleryPlugin.cs](WidgetGallery/WidgetGalleryPlugin.cs) contributes the Component gallery and Legacy examples blocks and registers the original gallery's view models, random-number service and validator. [ShowcasePlugin.cs](Showcase/ShowcasePlugin.cs) contributes Application patterns, and [ScreenPoliciesPlugin.cs](Application/ScreenPoliciesPlugin.cs) contributes the empty/permanent screen examples. All use `BlazorPluginBuilder` → `BlazorApplicationBlockBuilder` → `BlazorApplicationMenuBuilder` → `BlazorApplicationMenuItemBuilder`, backed by the shared `Sphere10.Framework.Application.UI` models, validation and UI coordination. Each also registers its own startup and shutdown task. The WinForms tester consumes the same neutral foundation through native adapters.

The active host calls all four definitions directly. The `permanentScreen` and `emptyScreenLifetime` values come from the selected launch profile:

```csharp
services
	.BuildBlazorApplication()
	.WithTitle("Sphere10 Blazor demos")
	.WithFavicon("img/logo.svg", "image/svg+xml")
	.ConfigureApplication(WorkspaceCommands.Configure)
	.ConfigurePlugin(Sphere10Plugin.Configure)
	.ConfigurePlugin(WidgetGalleryPlugin.Configure)
	.ConfigurePlugin(ShowcasePlugin.Configure)
	.ConfigurePlugin(plugin => ScreenPoliciesPlugin.Configure(plugin, permanentScreen, emptyScreenLifetime))
	.Build();
```

`BlazorApplicationOptions` supplies the configured title and favicon to `App.razor` and the workspace. The host resolves `img/logo.svg` through its fingerprinted static assets; there is one SVG favicon link in the document head.

`BlazorApplicationBuilder` registers immutable singleton branding options and uses the existing plugin and application registration APIs. [App.razor](App.razor) resolves those options for the document title and SVG favicon, using `Assets` to map the icon path. [Workspace](Application/Workspace.razor) uses the same configured title for `PageTitle` and `ApplicationShell`. Change `WithTitle`/`WithFavicon` in the builder to brand the host; no scoped application is resolved to render the head.

Each plugin owns the dependencies its registered screens and their content need. The active tester no longer constructs legacy `BlazorRoutedApplication`, `BlazorRoutedApplicationBlock` or `BlazorRoutedApplicationPage` graphs, uses a plugin locator, or manually calls `new BlazorRoutedPlugin().ConfigureServices(...)`. The routed navigation library remains covered by its dedicated compatibility tests. Hosted application screens derive from `BlazorApplicationScreen`; both platforms use the shared `Application.UI.ScreenActivationMode` enum.

Block navigation and guards use `IApplicationScreenHost` from `Sphere10.Framework.Application.UI`. File's Close screen command uses `ISingleApplicationScreenHost`; retained screen collections and batch operations use `IMultiApplicationScreenHost`. All these aliases resolve one host per circuit. The application exposes loaded plugin metadata and global commands and owns its local lifecycle/resources, while its `ScreenHost` owns selection and `UserInterfaceServices`. Global commands remain visible alongside same-ID screen commands and execute when no block is selected. The toolbar's Use single screen / Use tabs button awaits the guarded layout transition and keeps retained screen state. The footer shows global command executions and completed plugin initializers. The shell and collection editor use Blazor extensions for tab IDs, renderer attachment, activation parameters and reuse keys. Razor creates and disposes components; logical screen proxies delegate lifecycle to their retained components.

The configuration has this shape, using the tester's existing screen and state types:

```csharp
services
	.AddSphere10BlazorPlugin(plugin => plugin
		.WithName("Blazor tester")
		.ConfigureServices(registry => registry.AddScoped<WorkspaceStatus>())
		.ConfigureBlock(block => block
			.WithId("workspace")
			.WithName("Workspace")
			.WithIconUrl("img/heading-solid.svg")
			.WithDefaultScreen<OverviewScreen>()
			.ConfigureMenu(menu => menu
				.WithId("work")
				.WithText("Screen hosting")
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

Import `Microsoft.Extensions.DependencyInjection`, `Sphere10.Framework.Web.AspNetCore.Blazor` and `Sphere10.Framework.Utils.BlazorTester.Application` for this standalone example. In the running tester, use the four plugin definitions configured in `Program` rather than registering this additional workspace block; block IDs must remain unique. Standalone registrations appear directly in `Application.Blocks` and do not create a loaded plugin. Original URLs such as `/widget-gallery/wizards` and `/servers` select the corresponding Legacy examples screens through Workspace. Their content uses services registered by those same plugins.

Open `/application` and try these flows. If a saved startup choice opens another screen, choose **Workspace → Overview** for the counter example:

- Increment the Overview counter, switch screens and return: the component instance is retained.
- Choose **Screen hosting → Single-instance screen**. Change its **Tab title** and increment its instance counter, leave and choose the same menu again: its visible instance ID and state remain unchanged. Typing in **Session note** prevents switching or closing until **Save in session** or **Discard changes**.
- Choose **Screen hosting → New screen instance** more than once, or **New scratchpad** in File/the toolbar. Each instance has a different visible ID, its own notes, counter, tab title and history entry. Switch between them to compare retained state, then reorder a tab by dragging or with Ctrl+Shift+Home/End.
- Check **Prevent tab switching** on either screen type. Browse other blocks using the bottom icons: only the left menus change. Selecting a different screen or tab, or closing this instance, is vetoed until the checkbox is cleared. The checkbox belongs to that instance; Save/Discard does not clear it, and scratchpad notes alone do not block switching.
- Select tabs with the arrow keys or Home/End. Close with its button, middle-click or Delete; right-click or Shift+F10 exposes Close, Close other screens and Close all screens. A dirty editor vetoes a batch before any tab closes.
- Use **Open Screens** beside the tab strip to select a particular open instance. Identical titles gain occurrence numbers such as `(2)`; the selector switches the existing instance and retains its notes and counter. Registered icons, title ellipsis/full tooltips and dirty markers remain independent. Permanent tabs also appear; hidden retained and empty-workspace sessions do not.
- Right-click a restorable tab and choose **Make Startup Screen**. Reopen its context commands to see the checked choice, then visit the root `/` or blank `/application` URL: the next circuit opens that registered screen without briefly opening an unrelated default. A direct bookmark such as `/components/grid`, or explicit block/screen query, still selects its requested target. The tester uses the application-qualified key `Sphere10.BlazorTester.Workspace` in this browser's local storage. Clear that site's storage to restore the registered default. This preference remembers the screen definition, not current notes, dynamic tab titles or other unsaved fields; screens requiring runtime arguments cannot be chosen.
- Use **View → Tabbed screens** or **View → Single screen** to change screen layout. Overview retains its counter when hidden in Single screen; leaving a scratchpad releases that multi-instance screen.
- In the top **File** menu and toolbar, **Application command** changes to **Save in session** while Single-instance screen is active. **Workspace action** remains a block contribution. Save there, switch back to Overview and verify the application command returns without duplicates; Help stays last.
- Run **Run scoped async action**. Its service belongs to the current circuit.
- Confirm the footer shows **Plugins initialized: 4**, then use **File → Exit application**. Decline once to retain the workspace; accept to see all four plugin finalizers logged before terminal content replaces the screens. Refresh to create and initialize a new circuit.
- Switch to **Component gallery**, then select **CRUD grid**, **Tables**, **Dialogs** or **Wizards**. Each screen hosts its reusable demo content and retains its own state. The **Legacy examples** block exposes the original gallery, dialogs, tables, wizard, endpoints and dashboard in the same host.

The URL carries the active screen's `block`, `screen` and `instance` query values. Browsing a different block does not change these values or create history entries. Back/Forward selects retained screens and restores their owning block menus; merged commands also continue to follow the active screen while another block is being browsed. A full refresh starts a fresh circuit and recreates the requested screen; sample edits are not persisted. See [the library guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) for screen lifecycle contracts and shell customization.

## Wizard walkthrough

Open `/widget-gallery/wizards` for the original widget wizard or `/components/wizards` for the wallet example. **Allow cancellation** is checked before opening either wizard. Clearing it creates a wizard with `WithCancellation(false)`: Cancel and the header close button are hidden, and Escape keeps the wizard open. Complete the wizard to leave this mode. A step may also opt out through `IsCancellable`; an application `OnCancelled` callback can still reject cancellation and display a specific validation message.

In **New Widget**, try Next with empty fields to see validation. Enter Name, Description and a positive Price. Select **Supply dimensions**, continue to Height and Length, then use Back to change the selection. The following steps update without duplicating the dimensions page. The summary offers Finish; only Finish adds a row to the example's table. Cancel or closing a cancellable wizard discards its unfinished model.

In **New Wallet**, enter a name and choose Standard for the password branch or Restored for the seed branch. Back lets you change branches. The final Create action reports the result in the page; no real wallet is stored. Both examples share responsive wizard footers and the active theme.

Both platform adapters now share the navigation and completion workflow in `Sphere10.Framework.Application.UI`. Blazor owns the rendered step validation and dialog presentation; its `BlazorWizardBuilder<TModel>` reuses the common builder state.

The workspace Details/Settings, wallet Type and widget Details steps declare `WizardStepTraits.AssumeHasNext` because their validation composes the remaining route. They continue to show Next if that route has not been registered yet. Only ordinary review/summary steps show Finish. A successful validation that fails to register its promised successor reports an error and keeps the wizard active. See the [library's conditional wizard guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#conditional-wizard-screens) for the shared metadata and rendered-host behavior.

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
- Browse all configured blocks using the bottom icons; verify that only the left menus change and the active screen URL stays fixed. Check that Component gallery and Legacy examples have separate icons and menus despite sharing the Widget Gallery plugin. Open screens from those menus to check retained state, history and merged screen commands.
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

The original `Loader`, `ModernLoader` and `WidgetGallery` project files, WebAssembly entry points and original static-asset copies remain as inactive migration references pending approval to remove superseded files. The obsolete entry points/assets are excluded from the active tester, and the old projects are not solution projects; retained Loader and WidgetGallery component/service code still supplies the current hosted examples. Use the root tester project named in the commands above.

## Theme component coverage

Open **Components → Themes** or `/components/themes` to compare the full palette. The gallery exercises solid/outline buttons, badges, semantic utilities, alerts, cards, form states and validation, tables, menus, tabs, pagination, lists, accordions and progress. Its dialog button opens the framework modal host. Drafts, selections and counters remain intact when changing the toolbar's Theme selector; the nested provider receives the same scoped theme.

The template styles now include source button/input padding, borders, shadows, casing and separate input corners, alongside full semantic palettes. For example, Boost uses raised uppercase buttons with unraised fields, Landkit uses bold actions, and Raleway keeps square inputs. Template families still have one CSS file each; the common `theme-bootstrap.css` integration supplies the shared component rules. Use semantic classes such as `bg-body` and `text-body-secondary` in demos instead of fixed template colors.
