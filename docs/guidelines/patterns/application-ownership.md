# Application Ownership

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
