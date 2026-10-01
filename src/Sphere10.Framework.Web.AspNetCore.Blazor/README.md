# Sphere10.Framework.Web.AspNetCore.Blazor

.NET 10 Razor class library implementing the Sphere10 ApplicationBlock pattern for interactive Blazor Web Apps. It includes the restored tables, grids, dialogs, wizards and plugin APIs, with current component lifecycle and JavaScript isolation patterns.

## ApplicationBlock architecture

The application APIs extend the existing `Logic` namespace. They mirror the WinForms configuration and hosting model while using Razor components, asynchronous lifecycle methods and circuit-scoped state.

| WinForms concept | Blazor implementation |
| --- | --- |
| ApplicationBlock and fluent builders | `Logic.ApplicationBlockBuilder`, `MenuBuilder`, `MenuItemBuilder` |
| Screen host interface/base/concrete/decorator | `IApplicationScreenHost`, `ApplicationScreenHostBase`, `ApplicationScreenHost`, `ApplicationScreenHostDecorator<TConcrete>` |
| Registered blocks | `IApplicationBlockCatalog` with immutable structural snapshots, base class and decorators |
| ApplicationScreen | `Logic.ApplicationScreen : ComponentBase, IApplicationScreen` |
| Single-instance screens | One retained, keyed component per screen type in a circuit, including selection through another block |
| Multi-instance screens | Independent sessions and component instances with unique IDs |
| Screen hide/close cancellation | Awaited `CanDeactivateAsync` before changing state |
| Screen display/hide lifecycle | `OnActivatedAsync` / `OnDeactivatedAsync`; disposal stays with the Razor renderer |
| Desktop images and control menus | Browser icon URLs/CSS classes and Razor menu components |
| Desktop screen tabs | `UI.Application.ApplicationShell` with open-screen selection and close controls |
| Screen navigation | Registered block/screen IDs and optional session IDs in the URL, with Back/Forward and bookmark support |

The implementation directly reuses `IHelpableObject` from `Sphere10.Framework.Application`, `EventHandlerEx`, `Guard`, disposable scopes and the existing `IDataSource<T>` grid pipeline. WinForms controls, image ownership and desktop docking are platform-specific and are not dependencies of this package.

## Register a workspace

Reference the project, or install the matching NuGet package from your package feed. In the host's `Program.cs`:

```csharp
using Sphere10.Framework.Web.AspNetCore.Blazor;
using Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSphere10Blazor();
builder.Services.AddApplicationBlock(block => block
	.WithId("work")
	.WithName("Workspace")
	.WithDefaultScreen<OverviewScreen>()
	.AddMenu(menu => menu.WithText("Work")
		.AddScreenItem<OverviewScreen>("overview", "Overview")
		.AddScreenItem<EditorScreen>("editor", "Editor")
		.AddScreenItem<ScratchpadScreen>("scratchpad", "New scratchpad", ScreenActivationMode.MultiInstance)
		.AddActionItem("refresh", "Refresh", (services, token) =>
			services.GetRequiredService<MyScopedService>().RefreshAsync(token))));

// After builder.Build():
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
```

IDs must be unique within their scope; item IDs are unique across all menus in a block. Register a concrete Razor component implementing `IApplicationScreen`, normally by inheriting `ApplicationScreen`. Conflicting single/multiple instance policies for the same type are rejected before the host starts. A default screen need not also appear in a menu.

Block definitions are application-wide snapshots. Open sessions, active selections, event aggregation and modal services are scoped to the user circuit. Action delegates receive that circuit's `IServiceProvider`: do not capture a scoped service in a singleton registration. Registration-time menu event handlers are retained by the snapshots and must also avoid capturing per-user state. Parameter dictionaries are copied, but their values are shallow copies; use immutable parameter values and inject mutable per-user services into the component.

Host the shell in an interactive routable page:

```razor
@page "/application"
@using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application

<ApplicationShell Title="My application" NavigationPath="application"
	BlockId="@BlockId" ScreenId="@ScreenId" InstanceId="InstanceId" />

@code {
	[SupplyParameterFromQuery(Name = "block")] public string BlockId { get; set; }
	[SupplyParameterFromQuery(Name = "screen")] public string ScreenId { get; set; }
	[SupplyParameterFromQuery(Name = "instance")] public Guid? InstanceId { get; set; }
}
```

`NavigationPath` is relative to the host's base URI. The shell also accepts `Header`, `Sidebar`, `Footer` and `EmptyContent` fragments. For an existing layout, compose `ApplicationBlockMenu`, `ApplicationMenus` and `ApplicationScreenHostView` directly with `IApplicationScreenHost`. A menu selection awaits its action and exposes failures to the containing Blazor error boundary.

Bookmarks identify registered screens, never arbitrary component type names. A session ID restores an existing screen in the current workspace; a fresh circuit recreates the screen from its registered definition. Retained component state lasts while the workspace remains mounted. Refreshing or leaving the workspace disposes its components; use a scoped state service or persistence when longer retention is required.

## Screen lifecycle and unsaved changes

Override `CanDeactivateAsync(CancellationToken)` to veto screen switching, closure or workspace navigation. Override `HasUnsavedChanges` to enable the browser's confirmation when leaving the page. Call the protected `NotifyScreenChanged()` after changing dirty state so the shell updates its navigation lock.

Override `OnActivatedAsync`, `OnDeactivatedAsync` and `DisposeAsyncCore` for lifecycle work. If overriding `OnInitializedAsync`, await the base implementation so the component attaches to its session. The renderer creates and disposes screen components; never instantiate or dispose them through the host.

Host transitions are serialized. Lifecycle callbacks must not await another transition on the same host; menu actions run outside the transition gate and may navigate. Cancellation and guard vetoes leave the selection unchanged before commit. A post-commit activation failure is propagated with the selected screen still active. Unregistering a block preflights all its open screens before removing any session. `CanNavigateAsync` checks hidden screens too.

## Current Blazor hosting and assets

The supported host is a .NET 10 Blazor Web App with Interactive Server rendering, including prerendering. This package depends on the shared ASP.NET Core layer and `Microsoft.AspNetCore.App`; it is not a standalone WebAssembly package.

Use `MapStaticAssets`, `ImportMap`, the generated host styles bundle and `@Assets` to reference fingerprinted resources in `App.razor`:

```razor
<link rel="stylesheet" href="@Assets["MyHost.styles.css"]" />
<link rel="stylesheet" href="@Assets["_content/Sphere10.Framework.Web.AspNetCore.Blazor/css/BlazorGrid.css"]" />
<ImportMap />
<!-- Place the grid script before blazor.web.js. -->
<script src="@Assets["_content/Sphere10.Framework.Web.AspNetCore.Blazor/js/BlazorGrid.js"]"></script>
<script src="@Assets["_framework/blazor.web.js"]"></script>
```

Modal hosts import their own `js/modal.js` module on demand, target element references, restore focus, and cancel pending operations on disposal. No global modal script or jQuery is required. The optional legacy `initDataTableById` helper still requires an application-provided DataTables plugin. The archived `css/app.css` Bootstrap-era theme is optional; the new workspace uses isolated CSS.

Use `Router.NotFoundPage` with a routable not-found page and status-code re-execution; the old `<NotFound>` fragment is unsupported in .NET 10. The tester enables `BlazorDisableThrowNavigationException` and adopts these hosting patterns. See Microsoft's [navigation guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/navigation?view=aspnetcore-10.0) and [static asset guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/static-files?view=aspnetcore-10.0).

## Existing components and migration

- `Components`, `ViewModels`, `Models`, `Services`, `Plugins`: original MVVM components, tables, modal/wizard framework and plugin navigation.
- `UI`, `Logic`: application workspace, dialogs, wizards, tables and editable `BlazorGrid`.

Both generations contain names such as `ModalHost` and `PagedTable`; import the generation locally or use fully qualified component tags. `AddSphere10BlazorPlugins<TPluginLocator>()` retains original plugin/navigation registration. Reusable library layouts no longer contain sample profile, endpoint or placeholder menu entries.

`UI.Controls.BlazorGrid.BlazorGrid<TItem>` uses `IDataSource<TItem>` asynchronous CRUD. Component parameters are auto-properties synchronized through lifecycle methods; virtual paging rejects stale responses and streaming tables cancel/restart enumeration when the source changes. Wizards validate the final step and complete once.

Replace old `Sphere10.Framework.DApp.Presentation*` namespaces with this library's namespace. Inject scoped modal/view services instead of using static services. Legacy `Logic.Application.Initialize(IServiceCollection)` remains a startup/plugin configuration API; `AttachScreenHost` projects its active state from a scoped host. Do not share that attached application instance across circuits.

## Samples and verification

The tester at `utils/Sphere10.Framework.Utils.BlazorTester` demonstrates the workspace at `/application`, component galleries, guarded editors, multiple-instance scratchpads and scoped actions. See its README for commands. NUnit coverage includes builder validation, catalog snapshots, host lifecycle/cancellation, component retention/disposal, overlapping routes, paging, streaming, modal completion and wizard validation.

Run `./validate-packages.ps1` from the repository root to pack the public framework graph and verify external NuGet consumers, including a published Blazor ApplicationBlock host and its Razor assets. This does not publish packages to a registry.