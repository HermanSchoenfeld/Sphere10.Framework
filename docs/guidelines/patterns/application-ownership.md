# Application Ownership

## Application and screen lifetimes

Treat application lifetime and screen lifetime as separate ownership boundaries.
Choose the owner before creating a timer, subscription, cancellation source,
background task or native resource. A screen borrows application services; it
does not become their shutdown owner merely because it displays their data.

| Owner | Start or acquire | Stop or release |
|---|---|---|
| Application-wide process inventory, telemetry, caches or shared workers | Application initializer, registered through the existing application/plugin lifecycle | Application finalizer cancels and awaits outstanding work before dependent services are disposed |
| One screen's initial snapshot and presentation | Screen load or first-display override, after required host services are available | Screen destruction/removal cleanup; retain an idempotent disposal fallback |
| A visible screen's refresh timer or view subscription | Display/activation override; resume with the latest published snapshot | Actual presentation change/deactivation, then destruction cleanup |
| An open screen's explicit capture session or shared-service lease | First display or the user's capture command, according to that screen's policy | Explicit stop or actual screen destruction; hiding a retained screen need not end the session |

Initialize controls and designer state in constructors. Put screen work in the
screen's overridable lifecycle methods rather than wiring the screen to its own
display/load events or starting collection in its constructor. Events remain
appropriate for outside observers and user commands. Initial control loading can
precede host attachment, so defer work requiring `Host`, `Block` or application
services until the screen is actually displayed. Make repeated display and
cleanup calls safe without duplicating workers or subscriptions.

### WinForms screen hooks

Use the methods of
[WinFormsApplicationScreen](../../../src/Sphere10.Framework.Windows.Forms/Application/Forms/WinFormsApplicationScreen.cs):

- `OnLoad(EventArgs)` prepares native controls; call the base implementation.
- `OnShowFirstTime()` starts first-display initialization once per retained
  instance. `OnShow()` handles each selection/presentation and must call its
  base implementation so first-display initialization is raised.
- `OnVisibleChanged(EventArgs)` is the existing native override for actual
  effective visibility. Use it for presentation-bound pause/resume, including
  ancestor visibility and docking changes; call the base implementation and
  tolerate construction before fields are initialized. Visibility is not proof
  of removal or permission to dispose shared resources.
- `OnHide(ref bool cancelHide)` and the legacy `ScreenHidden` event are
  **preflight guards**. They can run without a later hide, and a later guard can
  veto a batch operation. Only decide permission there: do not stop highlighting,
  cancel capture, unsubscribe, dispose resources or commit state in the guard.
- `OnDestroyScreen()` ends the screen-owned lifetime once. Cancel screen-owned
  work, detach observers and release owned sessions/leases there. Keep cleanup
  idempotent and retain the normal `Dispose(bool)` fallback for native controls
  and incomplete/unhosted construction. Do not dispose borrowed services.

These native hooks are synchronous. Do not turn a guard or lifecycle override
into `async void` to perform unobserved cleanup. Keep asynchronous work in
observed tasks, initiate cancellation from the appropriate hook, and arrange for
its actual owner to drain it before releasing resources it still uses. Do not
block the UI waiting for workers. The native host has synchronous presentation
paths; do not assume that a shared asynchronous lifecycle bridge adds a native
post-hide notification that the host does not issue.

### Blazor screen hooks

[BlazorApplicationScreen](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/Application/BlazorApplicationScreen.cs)
attaches through `OnInitializedAsync`; retain its base call when overriding it.
Use `OnActivatedAsync` for active-view work, `CanDeactivateAsync` only for the
leave decision, and `OnDeactivatedAsync` for accepted deactivation. Await the
operations these asynchronous hooks own. A retained hidden component remains
alive; use `DisposeAsyncCore` for final screen cleanup and leave renderer-owned
disposal to the renderer. Do not use rendering callbacks as a second sampling
lifecycle. Host lifecycle callbacks must not await a new transition on the same
host, which already owns the transition gate.

### Shared services, delivery and shutdown

Register application-owned workers with the existing initializer/finalizer
mechanism described below. Showing another process inspector, hiding a chart or
closing one tab must not create, stop or dispose the shared process/telemetry
provider. A screen subscribes to its published data and may acquire a counted
capture lease. For an open-view lease, retain it while that screen is hidden and
release it when the instance is removed. Stop the shared capture only when its
own policy permits it, such as after the last lease ends.

Keep native reads and data preparation on workers. A subscription should check
its lifecycle-controlled presentation state **before** posting to the UI, mark
hidden data dirty, and render the latest snapshot when the view resumes. Recheck
the lifetime/generation inside the callback so queued or late work cannot update
a removed screen. Cancellation during teardown is an owned cancellation outcome,
not a successful empty inventory or an availability failure; keep the previous
published snapshot intact.

Finalization and exit are different operations: use `ExitAsync` for the guarded
application exit and finalizers for draining shared resources. For a restart,
the task observed after the last native window closes must be able to complete
without posting back to its stopped message loop. Dispatch only work that still
requires a live UI; do not synchronously wait for a continuation that needs that
same dispatcher.

Verify first display, repeated activation, hidden retained instances, actual
removal, direct disposal before hosting, detached presentation, and a later
guard veto. Check that subscriptions do not queue hidden refreshes, leases remain
correct when several screens are open, and application shutdown drains shared
work independently of which screen is active. See the
[testing practice](testing.md#winforms-ui-test-contexts) for real native dispatcher
tests.

## Ownership and platform extensions

Application task registration belongs on
[ApplicationPluginBuilderBase](../../../src/Sphere10.Framework.Application/UI/ApplicationPluginBuilderBase.cs).
Call `WithName`
before `RegisterInitializer` or `RegisterFinalizer`, including factory
overloads; an unnamed registration must throw immediately before changing
builder state. Register task services under the built plugin's unique `Name`,
and reject duplicate names when loading. Name comparison is ordinal and
case-sensitive; custom plugin names must remain stable. No additional plugin ID is required.
Resolve tasks through the executing application's provider so scoped dependencies
and task instances belong to that application. Keep the inventory protected;
ordinary `ConfigureServices` callbacks continue to register other dependencies.
Reserve unkeyed tasks for the existing process lifecycle and `typeof(IApplication)`
keys for application-owned host tasks. Demonstrate these helpers in real plugins.

[ApplicationBase](../../../src/Sphere10.Framework.Application/UI/ApplicationBase.cs)
resolves host-owned keys first, then each successfully loaded plugin's name
through that application's provider. The builder captures each product's
actual name and configured service callbacks at `Build()`, so later builder
edits do not redirect an earlier plugin's tasks. The
[lifecycle registration tests](../../../tests/Sphere10.Framework.Application.Tests/UI/ApplicationPluginLifecycleRegistrationTests.cs)
cover declaration order, distinct transient task instances and scoped factories;
the [WinForms demo plugin](../../../utils/Sphere10.Framework.Utils.WinFormsTester/WinFormsComponentDemoPlugin.cs)
and [Blazor showcase plugin](../../../utils/Sphere10.Framework.Utils.BlazorTester/Showcase/ShowcasePlugin.cs)
use the helpers on their actual startup paths.

Keep common policies and contracts in the UI-independent application layer.
Platform adapters own presentation and platform-specific runtime details.
Use shared contracts for common behavior; inject a platform subtype only when
its extra members are required. Extend a shared type with an explicit platform
name rather than redeclaring the same name in another namespace.

Choose identity according to the abstraction. A host operating on actual
screen objects uses reference identity. A platform route or renderer may need
its own named identifiers; those identifiers should not become requirements
of an otherwise reference-based shared contract.

The running application exposes blocks, application commands, its screen host
and loaded startup plugins. Its runtime hierarchy is application → blocks →
menus and screen definitions; a plugin can contribute several blocks.
`IApplication.Blocks` projects the host's live registered membership, keeping
one membership owner. `LoadedPlugins` records actual successful startup
registrations rather than creating plugins for standalone blocks. Navigation
selection, screen state and view mode remain on the host. UI services provide
interactions and command execution. A single-screen contract and a
multiple-screen contract should expose the operations their consumers need
without requiring every host to provide tab collections and ordering.

[Blazor registration](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/IServiceCollectionExtensions.cs)
and [WinForms registration](../../../src/Sphere10.Framework.Windows.Forms/Extensions/IoCExtensions.cs)
validate contributions before and after `Load(IServiceCollection)` and publish
[ApplicationPluginRegistration](../../../src/Sphere10.Framework.Application/UI/ApplicationPluginRegistration.cs)
only after successful registration. A DI plugin registration alone is not a
loaded plugin. Service-only plugins still receive a receipt; directly registered
blocks need no synthetic plugin. Duplicate successful names and recursive
same-name loads fail. Failed registration removes newly added lifecycle service
descriptors under the attempted name through
[ApplicationPluginRegistrationScope](../../../src/Sphere10.Framework.Application/UI/ApplicationPluginRegistrationScope.cs);
ordinary service callbacks and other side effects are not transactional.

Make runtime ownership explicit: a block's `Parent` is its running application,
a menu's `Parent` is its block, and an item's `Parent` is its menu. Startup
registration assigns the block's `Source` to the original plugin after the
complete capture is validated. Removing live membership clears its parent but
preserves startup provenance. Screen command execution follows its parent
chain instead of searching every block for a matching definition.

[ApplicationUserInterfaceServices.ExecuteMenuItemAsync](../../../src/Sphere10.Framework.Application/UI/ApplicationUserInterfaceServices.cs)
reads the screen command's parent chain directly. The
[ownership tests](../../../tests/Sphere10.Framework.Application.Tests/UI/ApplicationParentOwnershipTests.cs)
cover live membership, provenance, rejected ownership transfers and commands
whose parent chain is missing.

Screen definitions have immutable metadata and reverse `Activators` membership.
Owning menu/item mutations update parents and reverse membership together;
transfer an object by removing it from its existing owner first. A copied graph
clones each source definition once and uses that clone for its default, commands
and activators. Independent snapshots must not share mutable ownership links.
Metadata variants have no copied commands still targeting the source definition.
Specifically,
[ApplicationScreenDefinition.WithTitle and WithActivationMode](../../../src/Sphere10.Framework.Application/UI/ApplicationScreenDefinition.cs)
create a new definition with an empty activator collection; they do not retarget
commands attached to the original definition.
Validate complete definition collections before changing host state. The screen
host captures each retained instance's lifetime and owns its type-to-definition
index directly.

[ApplicationBlockSnapshotBase](../../../src/Sphere10.Framework.Application/UI/ApplicationBlockSnapshotBase.cs)
maintains one reference-keyed definition map per copied graph. The
[Blazor snapshot adapter](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/Application/BlazorApplicationBlockSnapshot.cs)
uses that map for menus, toolbar commands and the selected default. Its
[graph tests](../../../tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/ApplicationGraphOwnershipTests.cs)
verify independent ownership and canonical definitions across builds and circuits.

Commands retain their contribution level when composed. Application commands
remain available when a block or screen contributes a command with the same
caption or platform key. Within contextual contributions, screen commands
may replace matching block commands. Preserve the original command objects
and callbacks; composition must not rewrite reusable registrations with
per-render ownership state.

[Tools.UI.GetCommandLayers and MergeApplicationMenus/ToolBarItems](../../../src/Sphere10.Framework.Application/UI/UITool.cs)
select the active screen's owning block before the browsed block and keep
application and contextual replacement keys in separate scopes. Platform
adapters supply their own semantic keys; the original command objects remain
the execution targets. The lower-level `MergeMenus` and `MergeMenuItems`
helpers do not introduce application-level protection; use the application-aware
variants when composing those tiers. See
[ApplicationCommandMergeTests](../../../tests/Sphere10.Framework.Application.Tests/UI/ApplicationCommandMergeTests.cs).

Assign a screen's `Host` and `Block` before lifecycle hooks, keep them while a
single instance is hidden, and clear them after actual removal cleanup or host
disposal. A rejected navigation guard leaves those links intact. Shared hosts
detach ownership; platform adapters own view and screen disposal.

[ApplicationScreenHost](../../../src/Sphere10.Framework.Application/UI/ApplicationScreenHost.cs)
wires and detaches the retained instance's ownership. Blazor additionally
[attaches renderer components](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/Application/BlazorApplicationScreenHostBase.cs)
to their session ownership and leaves component disposal to the renderer.

A synchronous configuration property must not start an unobserved transition
or block on asynchronous lifecycle work. Runtime changes that can deactivate
screens must await the host's guarded transition API. Make any restriction on
the synchronous setter explicit and leave the current state intact when it
cannot perform the requested change safely.

Expose array snapshots for collection-valued data properties. A defensive
membership copy retains the element references; it does not imply deep
cloning or ownership of those elements. Make resource ownership explicit,
and use the framework's disposable scopes for cleanup and state transitions.
