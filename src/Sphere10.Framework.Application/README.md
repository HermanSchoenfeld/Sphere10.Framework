<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💫 Sphere10.Framework.Application

**Complete application framework** providing dependency injection, modular architecture, settings management, lifecycle hooks, and product information for building production-ready Sphere10 Framework-based applications.

Sphere10.Framework.Application enables **rapid application development** by providing a complete infrastructure for **service composition, settings persistence, initialization/finalization pipelines, and modular configuration**—all integrated with Microsoft.Extensions.DependencyInjection.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Application
```

## Shared UI framework

`Sphere10.Framework.Application.UI` contains the common application-block model used by the WinForms and Blazor adapters. The Application package targets `net10.0` and has no ASP.NET Core, Windows desktop or other UI framework dependency. The `UI` folder also contains the existing help, website launcher and user-interface services, which retain their `Sphere10.Framework.Application` namespace.

| Shared responsibility | Types |
|---|---|
| Plugin definitions and lifecycle | `IApplicationPlugin`, `ApplicationPluginBase`, `ApplicationPlugin`, `ApplicationPluginDecorator`, `ApplicationPluginBuilder` |
| Running application and commands | `IApplication`, `ApplicationBase`, `ApplicationDecorator`, `IApplicationCommandProvider` |
| Application interactions and command execution | `IUserInterfaceServices`, `ApplicationUserInterfaceServices`, `UserInterfaceServicesDecorator` |
| Application command composition | `ApplicationCommandLevel`, `ApplicationCommandLayer`, `Tools.UI.GetCommandLayers`, `Tools.UI.MergeApplicationMenus`, `Tools.UI.MergeApplicationToolBarItems` |
| General menu and toolbar merging | `Tools.UI.MergeMenus`, `Tools.UI.MergeMenuItems`, `IApplicationMenuSeparator` |
| Wizard orchestration and configuration | `IWizard<TStep>`, `IWizard<TModel, TStep>`, `IWizardBuilder`, `WizardBase`, `Wizard`, `WizardDecorator`, `WizardBuilderBase` |
| Block, menu and item contracts | `IApplicationBlock`, `IApplicationMenu`, `IApplicationMenuItem`, `IScreenMenuItem` |
| Mutable metadata and notification handlers | `ApplicationBlockBase`, `ApplicationMenuBase<TItem>`, `ApplicationMenuItemBase` |
| Fluent builder composition and validation | `IApplicationUIBuilder<TProduct>`, `ApplicationBuilderBase`, `ApplicationPluginBuilderBase`, `ApplicationBlockBuilderBase`, `ApplicationMenuBuilderBase`, `ApplicationMenuItemBuilderBase` |
| Snapshot traversal and ownership validation | `ApplicationBlockSnapshotBase<TBlock, TMenu, TItem>` |
| Screen metadata validation and lifecycle | `ScreenActivationMode`, `ScreenKind`, `ApplicationScreenDefinition`, `IApplicationScreen` |
| Block navigation and screen hosting | `IApplicationScreenHost`, `IApplicationScreenHost<TBlock, TScreen>`, `ApplicationScreenHostBase<TBlock, TScreen>`, `ApplicationScreenHost<TBlock, TScreen>`, `ApplicationScreenHost`, `ApplicationScreenHostDecorator` |
| Single-screen operations | `ISingleApplicationScreenHost`, `ISingleApplicationScreenHost<TBlock, TScreen>`, `SingleApplicationScreenHostDecorator` |
| Multiple-screen operations | `IMultiApplicationScreenHost`, `IMultiApplicationScreenHost<TBlock, TScreen>`, `MultiApplicationScreenHostDecorator` |
| Awaitable menu actions | `ApplicationAction` |

Platform-specific extensions use explicit names: `IBlazorApplicationBlock : IApplicationBlock` and `IWinFormsApplicationBlock : IApplicationBlock`, implemented by `BlazorApplicationBlock` and `WinFormsApplicationBlock`. Their menus, builders and screen hosts follow the same convention. Import the shared UI namespace normally alongside the platform namespace; the APIs do not require namespace aliases. Both platforms use the shared `ScreenActivationMode` directly.

The shared storage classes are abstract extension foundations. Platform adapters provide concrete blocks and menus. A block owns `ApplicationScreenDefinition` objects with immutable screen type, title, activation mode and kind. Its `DefaultScreenDefinition` references one of its `ScreenDefinitions`; screen commands reference those same objects. `Activators` returns the attached commands targeting that definition. Navigation is optional: a screen can be registered and activated without a menu entry.

```csharp
using Sphere10.Framework.Application.UI;

var block = new WorkspaceBlock { Name = "Administration", Position = 10 };
var users = new ApplicationScreenDefinition(typeof(UsersScreen), "Users", ScreenActivationMode.SingleInstance);
block.AddScreenDefinition(users);
block.DefaultScreenDefinition = users;
// await host.OpenScreenAsync(block, users);

public sealed class WorkspaceBlock : ApplicationBlockBase {
}

public sealed class UsersScreen : IApplicationScreen {
	public IApplicationScreenHost Host { get; set; }
	public IApplicationBlock Block { get; set; }
}
```

`AddMenu` registers each screen command's definition on the block and assigns the menu's `Parent`. `AddItem` assigns the item's `Parent` and updates its definition's reverse activator membership. Removing membership clears that parent; transferring a menu or item requires removing it from its current owner first. Removing a menu leaves its block-owned screens available for direct activation. `IApplicationBlock.Parent` identifies the running application, while `Source` identifies its original startup plugin; standalone blocks have no source plugin. UI services activate a screen command through `item.Parent.Parent`.

Block builders collect definitions introduced by child menu builders, explicit `AddScreenDefinition` calls and `WithDefaultScreen` configuration. Snapshot factories clone definition metadata once per source reference in each copied graph. Its default, commands and activators use the same canonical cloned objects; separate snapshots have independent parents and reverse membership. `WithTitle` and `WithActivationMode` create metadata variants with no copied activators. Blazor owns named IDs, reserved routing IDs, parameter copies and lookup validation; the shared models require none of these.

`Menus`, `Items`, `ScreenDefinitions`, `Activators`, `Blocks` and `AvailableBlocks` return defensive arrays; modifying array slots cannot change stored membership. Copies preserve element references within their graph. Presentation command merging borrows the original command references and callbacks without changing their owning parents.

`ApplicationScreenDefinition.ValidateDefinitions` checks a complete collection for valid screen types, activation modes, hosting kinds and conflicting declarations without changing metadata or membership. It accepts an optional platform type validator. The host validates a whole batch before updating its internal type-to-definition index and preserves a retained screen's captured lifetime. `IApplicationScreen` supplies writable `Host` and `Block` ownership slots, UI-independent help metadata, cancellation-aware activation/deactivation hooks and a navigation guard. `ApplicationAction.ExecuteAsync` receives the executing service provider and cancellation token; callbacks should resolve scoped services there. Owner-aware synchronous callbacks use `ApplicationAction.UIAction` and `Execute(IUserInterfaceServices)`; `ExecuteAsync` resolves those UI services from the executing provider. `PrimaryUIController` supplies the platform UI owner. Registration-time callbacks must not capture scoped services or mutable per-user state.

The [WinForms adapter](../Sphere10.Framework.Windows.Forms/README.md) retains controls, images, UI-thread dispatch and desktop screen ownership. The [Blazor adapter](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md) retains component validation, rendering, circuit scopes and browser interaction. Each supplies factories and platform validation to the shared engines. An ApplicationBlock describes navigation metadata; `ModuleConfiguration` continues to handle service registration and framework startup.

The [Application test suite](../../tests/Sphere10.Framework.Application.Tests/README.md) exercises the shared behavior using ordinary .NET classes and checks the framework assembly dependency graph. [NuGet validation](../../scripts/package-consumers/README.md) also builds a consumer referencing only the Application package and rejects UI dependencies in its resolved packages and framework references.

## Application plugins

The runtime hierarchy is `IApplication` → `Blocks` → menus and screen definitions. A plugin is a named startup contribution that can supply several blocks. `LoadedPlugins` records the actual plugins whose startup registration completed successfully. Standalone blocks participate directly in the application hierarchy. The application's `Blocks` projects its `ScreenHost.Blocks`, so the host remains the single owner of live membership and navigation selection. Shell block icons select blocks; browsing another block does not require a screen switch.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;

var plugin = new ApplicationPluginBuilder()
	.WithName("Administration")
	.AddBlock(new WorkspaceBlock { Name = "Users" })
	.ConfigureServices(services => services.AddSingleton<AdministrationSettings>())
	.Build();

var services = new ServiceCollection();
plugin.Load(services); // Startup owner calls this before building its service provider.
// A startup owner can call plugin.Unload() during its own shutdown sequence.

public class AdministrationSettings {
	public string Title { get; set; } = "Administration";
}
```

`ApplicationPluginBase.Load(IServiceCollection)` applies service configuration, invokes `OnLoaded`, then raises `Loaded`. `Unload` invokes `OnUnloaded`, then raises `Unloaded`. Registration failures propagate without announcing a successful load. The parameterless `Load()` is a notification-only compatibility entry point. These methods do not construct a service provider or enforce a once-only load policy: the startup owner controls calls, and runtime application adapters never load or unload plugins merely because a form or circuit opens or closes. Resolve scoped services when commands execute, rather than capturing them in startup definitions.

Platform startup registrars publish an `ApplicationPluginRegistration<TPlugin>` only after loading the plugin, validating its finalized definitions and registering its blocks. They assign each registered block's `Source` to the original plugin. The immutable receipt retains that plugin and copies finalized block membership; constructing a receipt validates existing source ownership without changing it or invoking lifecycle methods. Application factories resolve this receipt inventory to supply `LoadedPlugins`; an unrelated plugin registered directly in DI does not become a loaded startup plugin. The framework does not currently connect plugin `Unload` to process shutdown, application disposal or Blazor circuit disposal.

`ApplicationPlugin` supports constructor configuration and init-only `Name`/`Blocks`; its arrays copy membership and retain block identity. Platform adapters can snapshot their own block metadata before forwarding it to shared storage. `ApplicationPluginBuilderBase<TBlock, TPlugin>` shares name validation, accumulated blocks, service callback composition and independent build inputs with typed platform builders. Empty applications and service-only plugins with no blocks are valid.

Typed plugin implementations derive from `ApplicationPluginBase<TBlock>` to share storage and lifecycle while remaining assignable to the existing `ApplicationPlugin`. The generic interface supplies the neutral `Blocks` projection for direct interface implementations. Because an inherited class implementation takes precedence over an interface default, the typed base retains one compatibility hook that sends neutral class reads through the typed virtual getter. Platform plugins use this shared hook instead of repeating explicit interface adapters.

`Tools.UI.ValidatePlugins` validates plugin names and block ownership for startup configuration: plugin names must be nonblank and unique with ordinal comparison, and each block object belongs to exactly one plugin. Blocks require nonblank names but are identified by reference. It returns an owned membership array preserving the supplied plugin instances. `ValidatePluginBlocks` provides the corresponding single-plugin block validation. `ValidatePluginRegistrations` applies the same name and ownership checks to captured registration receipts without rereading mutable plugin block membership. Platform interfaces extend `IApplicationPlugin` with typed block arrays; existing plugins remain the same startup definitions when accessed through shared or platform contracts.

## Running application and command scopes

`IApplication` represents the running application rather than service-container startup. It exposes array-valued `Blocks` and `LoadedPlugins`, `ScreenHost`, `HasUnsavedChanges` and a `Changed` event, alongside application menus and toolbar commands. `Blocks` reflects the host's current registered membership; `LoadedPlugins` preserves startup provenance, including service-only plugins with no blocks. Navigate through `ScreenHost`, inspect its `ActiveBlock` and `ActiveScreen`, and obtain interactions through `ScreenHost.UserInterfaceServices`. The application keeps no duplicate navigation state. `IBlazorApplication` and `IWinFormsApplication` specialize that contract. Resolving the shared interface returns the same platform application instance; disposing that adapter releases subscriptions without taking ownership of the host, its interaction services, screens or plugin registrations.

`IApplication<TPlugin, THost>`, `IApplicationPlugin<TBlock>`, `IApplicationBlock<TMenu, TItem>`, `IApplicationMenu<TItem>` and `IApplicationCommandProvider<TMenu, TItem>` provide typed platform collections and a typed host while centrally projecting the same instances through their neutral contracts. Platform interfaces inherit these generic contracts instead of redeclaring common members. Consumers should inject the neutral contract for shared behavior and use a typed platform contract when they need platform presentation details.

`ApplicationBase` shares command storage, notifications and live projections of the host's blocks and dirty state. Derived applications call `ObserveScreenHost()` after assigning their host; it maintains each registered block's `Parent`, clears it on unregistration or application disposal, and preserves startup `Source`. `IApplicationCommandProvider` supplies array-valued `Menus` and `ToolBarItems`. `Tools.UI.GetCommandLayers(application)` identifies the application, the active screen's owning block and the active screen. With no active screen, it uses the browsed navigation block. Application commands also work when no block is selected.

Use `MergeApplicationMenus` and `MergeApplicationToolBarItems` to compose those layers. Application contributions survive matching contextual captions or platform keys; within contextual contributions, a screen replaces a matching block command in its original position. Reusing the exact application command object in a screen replaces matching block context without duplicating that global command. Matching menus combine items and take the narrower menu's display metadata. Separators are normalized and Help remains last. The results retain original command references and callbacks. UI adapters distinguish application and contextual rendering keys when both use the same local command ID.

```csharp
using System;
using Sphere10.Framework.Application.UI;

public static class ApplicationCommands {
	public static TMenu[] ComposeMenus<TMenu, TItem>(IApplication application, Func<TMenu, TItem[], TMenu> createMenu)
		where TMenu : class, IApplicationMenu
		where TItem : class, IApplicationMenuItem =>
		Tools.UI.MergeApplicationMenus<TMenu, TItem>(createMenu, Tools.UI.GetCommandLayers(application));
}
```

The convenience overloads use display captions; adapters can supply semantic key selectors. General `MergeMenus` and `MergeMenuItems` remain available for arbitrary ordered layers where every narrower contribution may replace a broader one. Application shells use the level-aware helpers to preserve application commands.

`ScreenMode.SingleView` and `ScreenMode.MultiView` live in this shared namespace alongside `ScreenActivationMode`. Their screen retention rules belong to the shared screen host; platform adapters own presentation and view disposal.

## Shared application screen host

Use `IApplicationScreenHost` for block navigation, current-screen hosting, view mode and unsaved-state checks. Use `ISingleApplicationScreenHost` to close the current screen, and `IMultiApplicationScreenHost` when working with retained collections, ordering or other tabs. Use `IUserInterfaceServices` for application interactions and menu-command execution. The Blazor and WinForms registrations expose their platform implementations through these neutral interfaces, preserving circuit or native application lifetimes. `IApplication.ScreenHost.UserInterfaceServices` exposes the host's interaction service. Resolve a platform interface when code needs component metadata, native controls or docking.

The generic screen-host interfaces provide neutral property and method projections, including argument validation and asynchronous return adaptation, for direct implementations. Reusable host bases and typed decorators retain their neutral class mappings so virtual overrides remain authoritative even when a subclass explicitly remaps the typed interface.

`IApplicationScreenHost` owns available and registered blocks, the selected navigation block, the current screen and lifecycle transitions. `IApplicationScreenHost<TBlock, TScreen>` supplies typed projections constrained to application blocks and screens. The single and multiple host contracts have corresponding typed variants. The dynamic shared implementation supports both capability interfaces; consumers depend on the operations they use. Hosting uses screen reference identity; there is no abstract session descriptor, runtime GUID or renderer-attachment API.

`SelectBlockAsync(block)` browses its navigation without leaving the current screen. `ActivateBlockAsync(block)` opens its configured default; `ActivateScreenAsync(block, type)` and the screen-menu overload resolve type policy before creating or selecting a screen. `OpenScreenAsync(block, definition)` accepts a canonical definition owned by that block. `AvailableBlocks` retains startup definitions while `Blocks` reflects registration. Removing a block preflights its owned screens, including permanent screens, before updating membership. Registering a known block restores its definitions and permanent screens after the current placeholder's leave guard permits the transition.

On `IMultiApplicationScreenHost`, `Screens` includes hidden singletons and `OpenScreens` contains normal screens in presentation order. An empty placeholder has no tab. `ActiveBlock` may differ from the active screen's owner after browsing another block; `GetCommandLayers` resolves the owner when composing that screen's commands. Explicit screen selection restores its owner's navigation. Dirty-state notifications preserve a separately browsed block.

The host assigns a screen's `Host` and `Block` before activation and lifecycle hooks. They remain attached while a single instance is hidden, and clear after actual removal cleanup or host disposal. A rejected deactivation guard preserves ownership. Shared host cleanup detaches those slots; platform adapters retain responsibility for disposing their views and screen instances.

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using Sphere10.Framework.Application.UI;

public static class WorkspaceCommands {
	public static Task<IApplicationScreen> OpenSettingsAsync(IApplicationScreenHost host) {
		var settings = host.Blocks.Single(block => block.Name == "Settings");
		return host.ActivateBlockAsync(settings);
	}

	public static Task<bool> CloseOtherTabsAsync(IMultiApplicationScreenHost host) {
		var current = host.ActiveScreen;
		var closing = host.OpenScreens.Where(screen => !ReferenceEquals(screen, current)).ToArray();
		return host.CloseScreensAsync(closing);
	}
}
```

`ISingleApplicationScreenHost.CloseActiveScreenAsync` and `CanCloseActiveScreenAsync` work on the current screen. The multiple host contract adds reference-based Show, Close, CanClose, Move and batch Close operations. `ApplicationScreenHostDecorator`, `SingleApplicationScreenHostDecorator` and `MultiApplicationScreenHostDecorator` forward their respective contracts; choose the matching decorator when overriding a capability.

The writable `ScreenMode` property supports safe configuration: the same value, promotion to multiple view, or switching to single view when no other ordinary open screen requires closure. It rejects a switch requiring lifecycle work before changing any state. Runtime controls must await `TrySetScreenModeAsync(mode)`, which preflights all affected guards before removing anything; the synchronous setter never blocks on an asynchronous guard. A change during an in-progress transition is also rejected.

Permanent screens reject ordinary closure. `CanNavigateAsync` checks all retained screens. `NotifyScreenChanged(screen)` reports changes to a retained screen's title, commands or dirty state. The host's asynchronous engine serializes lifecycle work, checks cancellation before committing selection or removal, and propagates post-commit activation failures. Lifecycle callbacks must not await another transition on the same host.

`ApplicationScreenHost<TBlock, TScreen>` shares registration, type policies, singleton retention, selection, ordering and guards. Its overridable `CreateScreen`, reuse and removal hooks let adapters own platform presentation. The neutral factory resolves or creates the actual screen through dependency injection. Native adapters create and own controls; Blazor keeps its renderer proxy, session GUIDs, runtime reuse keys, parameters and attachment bookkeeping in the Blazor library. Those presentation details do not enter the abstract host contract. Named routing IDs and lookup APIs live in Blazor. Shared menu merging uses display captions by default and accepts adapter-supplied key selectors when a platform needs another semantic identity.

`IUserInterfaceServices` provides application dialogs, status, reporting, exit, UI dispatch and menu/action execution. `ApplicationUserInterfaceServices` borrows the host for screen commands without retaining another block or screen collection. The neutral host uses a registered interaction service when available; otherwise it lazily owns one shared command-capable fallback. Host disposal releases that fallback and leaves registered services to their independent owner. Platform interaction implementations supply dialogs and native/browser dispatch.

Actions execute outside the host's lifecycle gate and may navigate through `ScreenHost`. Owner-aware callbacks receive the interaction service executing the command, while asynchronous callbacks receive the executing scoped provider. The interaction service's `Changed` event reports interaction/status changes; host membership and screen changes are reported by the host. Disposing UI services does not dispose the borrowed host or platform screens.

The [host tests](../../tests/Sphere10.Framework.Application.Tests/UI/ApplicationScreenHostTests.cs) protect lifecycle and retention, and the [navigation tests](../../tests/Sphere10.Framework.Application.Tests/UI/ApplicationScreenHostNavigationTests.cs) cover block selection and guarded membership changes. The [capability tests](../../tests/Sphere10.Framework.Application.Tests/UI/ApplicationScreenHostCapabilityTests.cs) cover the interface split and safe synchronous and asynchronous mode changes. The [identity tests](../../tests/Sphere10.Framework.Application.Tests/UI/ApplicationScreenIdentityTests.cs) exercise actual screen references, including distinct screens with equal values. The [UI-services tests](../../tests/Sphere10.Framework.Application.Tests/UI/ApplicationUserInterfaceServicesTests.cs) cover command execution, interactions and the responsibility boundary.

## Shared fluent builders

The fluent methods live in this library. WinForms and Blazor inherit the same configuration, validation and child-builder composition; their adapters supply native products, supported screen/step checks, and platform presentation options.

| Shared base | Responsibility |
|---|---|
| `ApplicationBuilderBase<TSelf, TApplication, TPlugin, TPluginBuilder, TResult>` | Application title, existing plugins, `ConfigurePlugin(builder => ...)`, deferred application callbacks and a terminal `Build`. The platform registers or creates its host without the base resolving services. |
| `ApplicationPluginBuilderBase<TSelf, TBlock, TBlockBuilder, TPlugin>` | Plugin name, service callbacks, existing blocks and `ConfigureBlock(builder => ...)`. |
| `ApplicationBlockBuilderBase<TSelf, TMenu, TMenuBuilder, TBlock>` | Block metadata, default screen policy, existing menus and `ConfigureMenu(builder => ...)`. |
| `ApplicationMenuBuilderBase<TSelf, TItem, TItemBuilder, TMenu>` | Menu metadata, existing items and `ConfigureItem(builder => ...)`. |
| `ApplicationMenuItemBuilderBase<TSelf>` | Screen/action selection, screen captions and instance policies. Platform factories reject unsupported item kinds. |
| `WizardBuilderBase<TSelf, TModel, TStep, TWizard>` | Fluent wizard setup, reset, ordered steps, cancellation, callbacks and build validation. |

`ConfigurePlugin`, `ConfigureBlock`, `ConfigureMenu` and `ConfigureItem` follow `ProtocolBuilder`: create a child builder, invoke its callback, call `Build()` and register the product through the corresponding `Add` method. `AddPlugin`, `AddBlock`, `AddMenu` and `AddItem` accept existing definitions. Convenience `Add` methods construct definitions directly from their arguments without invoking a child builder.

`IApplicationUIBuilder<out TProduct>` lets a parent accept child builders through their product interfaces. The self type preserves fluent platform return types; the smaller state bases remain available for custom builders that need a different facade. New platform adapters should override factories and validation hooks or add genuine platform options, rather than repeat `WithName`, `ConfigureBlock`, `ConfigureMenu` or wizard lifecycle methods. Web icon URLs, native `Image` objects, Razor step constraints and DI registration stay in their respective libraries.

Native builders retain their existing live-product ownership; Blazor factories produce registration snapshots. Application host registration is terminal so repeating `Build` cannot repeat service-registration side effects. Plugin service callbacks and application configuration callbacks remain deferred to the existing startup and scoped application paths.

Keep each builder invocation as one continuous indented block without blank separator lines. Put each configuration call on its own line, indent nested calls one tab below their owner and align multiline closing parentheses with the owning call. See [the code guide](../../docs/guidelines/code-styling.md#fluent-configuration-hierarchy), [WinForms plugin configuration](../Sphere10.Framework.Windows.Forms/README.md#grouping-blocks-into-application-plugins), and [Blazor examples](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md). Both testers and the package-consumer fixtures use these inherited APIs.

## Default, permanent and empty screens

Ordinary Blazor and WinForms screens default to `ScreenActivationMode.MultiInstance`: each activation creates a new screen. Choose `SingleInstance` explicitly for a retained screen, such as Settings; permanent and empty-screen test scenarios keep their declared lifetimes. Enum numeric values are unchanged.

Both UI adapters use the same screen metadata and selection rules. Set a block's canonical `DefaultScreenDefinition`, or use the platform builders' `AsDefault()` and `WithDefaultScreen<TScreen>(...)` convenience APIs. Menu default hints are promoted to the block's selected definition. Startup chooses the first marked screen in plugin registration order, then each plugin's block order, then menu declaration order. `Position` remains a navigation display setting and does not override plugin ownership order. Explicit navigation to a screen takes precedence over startup selection.

The shared block builder resolves type-based defaults against matching owned definitions before creating the block. Menu registrations take precedence over definitions supplied through `AddScreenDefinition`. Omitted captions and policies inherit the matching definition's metadata; matching metadata reuses its exact reference. An explicit caption or policy override creates a separate definition owned by the block. `WithDefaultScreen(definition)` always retains the supplied definition unchanged. Platform builders can extend the inference candidates through `GetDefaultScreenDefinitions`, including canonical definitions retained by a cached block after its navigation changes.

| Configuration | Behavior |
|---|---|
| `ScreenActivationMode.MultiInstance` (default) | Each activation opens an independent screen instance. |
| `ScreenActivationMode.SingleInstance` | Reopening selects the retained instance. Ordinary tabs can be closed. |
| `ScreenActivationMode.PermanentSingleton` | Automatically opens one instance, remains open for the block's lifetime and cannot be closed through tab commands. Other screens can still be selected. |
| `ScreenKind.Empty` + `SingleInstance` | A tabless placeholder appears only when no normal screen is open; its instance is retained while hidden. |
| `ScreenKind.Empty` + `MultiInstance` | The placeholder is destroyed when a normal screen opens and recreated when the workspace becomes empty again. |

`Empty` describes the screen's role, separately from its activation mode. It never has a tab and cannot be combined with `PermanentSingleton`. A permanently open normal screen therefore keeps an empty placeholder hidden. Administrative block removal ends its screens' lifetime, including permanent screens, after applicable navigation guards permit removal; normal application shutdown also disposes them. A permanent screen does not make application shutdown impossible.

`Tools.UI.OrderApplicationBlocks` resolves plugin membership against registered blocks and appends standalone blocks in their supplied order. Its receipt overload uses captured startup membership, preserving registration order even when a plugin later changes its live `Blocks`. `GetScreenDefinitions` returns the immutable definitions already owned by each block, validating command references and lifetime/role consistency. `GetDefaultScreen(blocks)` and `GetEmptyScreen(blocks)` inspect the blocks' selected default references. Hosts can specialize startup order through `GetStartupScreen` without changing the definition model. The first explicitly marked empty candidate is preferred; otherwise the first empty candidate is used. Platform convenience APIs may reuse a matching owned definition; explicitly supplied definitions retain their reference identity. Conflicting explicit policies fail before registration.

For example, a UI-independent placeholder declaration is:

```csharp
var block = new WorkspaceBlock {
	Name = "Welcome",
	DefaultScreenDefinition = new ApplicationScreenDefinition(typeof(WelcomeScreen), "Welcome",
		ScreenActivationMode.SingleInstance, ScreenKind.Empty)
};
```

`WelcomeScreen` supplies the platform screen implementation. See the [Blazor guide](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md) and [WinForms guide](../Sphere10.Framework.Windows.Forms/README.md) for executable builder configuration and demo profiles. Rendering, retained component/control ownership and UI-thread dispatch stay in those adapters.

## Shared wizard workflow

`Wizard<TModel, TStep>` owns ordered steps, the current position, dynamic branch updates, cancellation policy and completion. A step can be a component `Type`, a native screen, or an ordinary domain object. The shared assembly has no knowledge of Razor components or Windows controls. Hosts validate and present the current step before asking the workflow to navigate or finish.

```csharp
using System.Threading.Tasks;
using Sphere10.Framework;
using Sphere10.Framework.Application.UI;

var wizard = new Wizard<string, string>(
	"Review request", "draft", new[] { "Details", "Review" },
	finish: model => Task.FromResult<Result<bool>>(true));

await wizard.MoveNextAsync();
var result = await wizard.FinishAsync();
// wizard.State == WizardState.Finished; another FinishAsync does not repeat the callback.
```

`MoveNextAsync` and `MovePreviousAsync` provide awaited navigation. `UpdateStepsAsync` and `RemoveStepAsync` let platform adapters update their displayed controls before returning. `WizardStepUpdateType` supports injecting steps, replacing the remaining branch, replacing all steps and removing subsequent steps. The workflow returns defensive `Steps` arrays while retaining step and model identities. Finish and Cancel share a [Stateless](https://www.nuget.org/packages/Stateless/5.20.1) lifecycle: concurrent terminal operations are rejected while busy; a declined or failed callback leaves a retryable workflow; successful completion runs once. Callbacks must not recursively finish or cancel the same wizard.

`WizardBuilderBase<TSelf, TModel, TStep, TWizard>` centralizes the fluent setup methods, reset/start requirements, callbacks and build validation over its shared state base. The shared `IWizardBuilder<TSelf, TModel, TStep, TWizard>` also carries the fluent contract, so interface-returning chains retain the same capabilities. Platform builders supply their step validation and factories. Use `WinFormsWizardBuilder<TModel>` for native dialogs, or `BlazorWizardBuilder<TModel>` from the chosen Blazor wizard generation. Their adapters keep rendering, screen initialization and component validation in the UI libraries while consuming the same workflow. `WizardDecorator` forwards the common contract for behavior extensions.

## Awaitable UI services

`IUserInterfaceServices.ShowNagScreen`, `ShowSendCommentDialog`, `ShowSubmitBugReportDialog`, `ShowRequestFeatureDialog`, and `ShowAboutBox` return `Task`. Await them to preserve sequencing and observe errors. `IProductLicenseEnforcer.EnforceLicense(bool)` also returns `Task`, and completes after any licensing dialog closes. Implementations with no UI work return `Task.CompletedTask`.

## ⚡ Quick Start

This minimal Windows Forms host uses the current startup builder. Reference `Sphere10.Framework.Windows.Forms` from a Windows desktop project with `<UseWindowsForms>true</UseWindowsForms>`; the shared Application package itself remains UI-independent. See [AutoMouse](https://github.com/HermanSchoenfeld/AutoMouse) for a complete application.

```csharp
using System;
using System.Windows.Forms;
using Sphere10.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;
using FormsApplication = System.Windows.Forms.Application;

static class Program {
	[STAThread]
	static void Main(string[] args) {
		using var singleInstance = new SingleApplicationInstanceScope();
		FormsApplication.SetHighDpiMode(HighDpiMode.PerMonitorV2);
		FormsApplication.EnableVisualStyles();
		FormsApplication.SetCompatibleTextRenderingDefault(false);

		Sphere10Framework.Instance
			.BuildWinFormsApplication()
			.UseMainForm<BlockMainForm>()
			.UseModule<Sphere10.Framework.Application.ModuleConfiguration>()
			.UseModule<Sphere10.Framework.Windows.Forms.ModuleConfiguration>()
			.StartWinFormsApplication();
	}
}
```

## 🏗️ Core Architecture

### The Framework Singleton

`Sphere10Framework.Instance` is the central orchestrator that:
- Discovers and registers all `ModuleConfiguration` classes in your assemblies
- Builds the DI container with all registered services
- Executes initialization and finalization pipelines
- Provides access to the `IServiceProvider`

```csharp
// Access services anywhere in your application
var settings = Sphere10Framework.Instance.ServiceProvider.GetService<IProductInformationProvider>();
var controller = Sphere10Framework.Instance.ServiceProvider.GetService<IAutoMouseController>();
```

### Framework Lifecycle

```
Sphere10Framework.Instance.StartFramework()
    ↓
    1. Discover ModuleConfiguration classes (via reflection)
    2. Call RegisterComponents() on each module (priority order)
    3. Build ServiceProvider
    4. Call OnInitialize() on each module
    5. Execute IApplicationInitializer instances
    ↓
Application runs...
    ↓
Sphere10Framework.Instance.EndFramework()
    ↓
    1. Execute IApplicationFinalizer instances  
    2. Call OnFinalize() on each module
    3. Dispose ServiceProvider (if owned)
```

## 🧩 ModuleConfiguration Pattern

The `ModuleConfiguration` pattern is the **core architectural pattern** for organizing your application into cohesive, self-contained modules. Each module registers its services, initializers, and configuration.

### Creating a Module

```csharp
using Sphere10.Framework;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace MyApp;

public class ModuleConfiguration : ModuleConfigurationBase {
    
    public override void RegisterComponents(IServiceCollection services) {
        // Register application services
        services.AddTransient<ISoundMaker, DefaultSoundMaker>();
        services.AddTransient<IMouseHook, WindowsMouseHook>();
        services.AddTransient<IKeyboardHook, WindowsKeyboardHook>();
        services.AddTransient<IAutoMouseController, WindowsAutoMouseController>();
        
        // Register help and UI services
        services.AddTransient<IHelpServices, CHMHelpProvider>();
        services.AddTransient<IAutoRunServices, StartupFolderAutoRunServicesProvider>();
        
        // Register initializers (run at startup)
        services.AddInitializer<FirstTimeSetWindowsStartupTask>();
        services.AddInitializer<DatabaseMigrationInitializer>();
        
        // Register control state providers for UI binding
        services.AddControlStateEventProvider<ClickRadiusSelector, ClickRadiusSelector.StateEventProvider>();
    }
    
    public override void OnInitialize(IServiceProvider serviceProvider) {
        base.OnInitialize(serviceProvider);
        // Custom initialization logic
    }
    
    public override void OnFinalize(IServiceProvider serviceProvider) {
        base.OnFinalize(serviceProvider);
        // Custom cleanup logic
    }
}
```

### Module Priority

Modules are executed in priority order. Set `Priority` to control execution order:

```csharp
public class ModuleConfiguration : ModuleConfigurationBase {
    public override int Priority => int.MinValue;  // Execute last (lowest priority)
    // or
    public override int Priority => int.MaxValue;  // Execute first (highest priority)
}
```

## ⚙️ Settings Management

### Defining Settings Classes

Settings inherit from `SettingsObject` and use `[DefaultValue]` attributes:

```csharp
using System.ComponentModel;
using Sphere10.Framework.Application;

public class AutoMouseSettings : SettingsObject {
    
    [DefaultValue(true)]
    public bool AutoStartProgram { get; set; }
    
    [DefaultValue(Key.LControlKey)]
    public Key ScreenMouseActivationKey { get; set; }
    
    [DefaultValue(1000)]
    public int ScreenMouseTimeoutMS { get; set; }
    
    // Computed property wrapping the stored value
    public TimeSpan ScreenMouseTimeout {
        get => TimeSpan.FromMilliseconds(ScreenMouseTimeoutMS);
        set => ScreenMouseTimeoutMS = (int)value.TotalMilliseconds;
    }
    
    [DefaultValue(true)]
    public bool MakeClickSound { get; set; }
    
    [DefaultValue(50)]
    public int ClickFreeZoneRadius { get; set; }
}
```

### Using Settings

```csharp
// Get settings (creates with defaults if not exists)
var settings = UserSettings.Get<AutoMouseSettings>();

// Modify and save
settings.AutoStartProgram = false;
settings.ScreenMouseTimeoutMS = 2000;
settings.Save();

// Reset to defaults
settings.RestoreDefaultValues();
settings.Save();

// Check if settings exist
bool hasSettings = UserSettings.Has<AutoMouseSettings>();
```

### Settings Scopes

- **`UserSettings`**: Per-user settings stored in `{UserDataDir}/{ProductName}/`
- **`GlobalSettings`**: System-wide settings stored in `{SystemDataDir}/{ProductName}/`

```csharp
// User-specific settings
var userPrefs = UserSettings.Get<UserPreferences>();

// System-wide settings (shared by all users)
var globalConfig = GlobalSettings.Get<SystemConfiguration>();
```

### Settings in UI Controls

Use the `[UseSettings]` attribute on controls to automatically bind settings:

```csharp
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

[UseSettings(typeof(AutoMouseSettings))]
public partial class AutoMouseSettingsControl : ApplicationControl {
    
    public AutoMouseSettings Settings => UserSettings.Get<AutoMouseSettings>();
    
    protected override void CopyModelToUI() {
        // Copy settings to UI controls
        _autoStartCheckBox.Checked = Settings.AutoStartProgram;
        _timeoutNumeric.Value = Settings.ScreenMouseTimeoutMS;
        _soundCheckBox.Checked = Settings.MakeClickSound;
    }
    
    protected override void CopyUIToModel() {
        // Copy UI values back to settings
        Settings.AutoStartProgram = _autoStartCheckBox.Checked;
        Settings.ScreenMouseTimeoutMS = (int)_timeoutNumeric.Value;
        Settings.MakeClickSound = _soundCheckBox.Checked;
        Settings.Save();
    }
}
```

## 🚀 Application Initializers

Initializers run automatically at framework startup. They're perfect for one-time setup tasks.

### Creating an Initializer

```csharp
using Sphere10.Framework.Application;

public class FirstTimeSetWindowsStartupTask : ApplicationInitializerBase {
    
    public FirstTimeSetWindowsStartupTask(
        IProductInformationProvider productInformationProvider,
        IProductUsageServices productUsageServices,
        IAutoRunServices autoRunServices) {
        ProductInformationProvider = productInformationProvider;
        ProductUsageServices = productUsageServices;
        AutoRunServices = autoRunServices;
    }
    
    public IProductInformationProvider ProductInformationProvider { get; }
    public IProductUsageServices ProductUsageServices { get; }
    public IAutoRunServices AutoRunServices { get; }
    
    public override void Initialize() {
        // Only on first launch
        if (ProductUsageServices.ProductUsageInformation.NumberOfUsesByUser == 1) {
            // Set the app to autorun on Windows startup
            AutoRunServices.SetAutoRun(
                AutoRunType.CurrentUser,
                ProductInformationProvider.ProductInformation.ProductName,
                Application.ExecutablePath,
                null);
        }
    }
}
```

### Registering Initializers

```csharp
public override void RegisterComponents(IServiceCollection services) {
    services.AddInitializer<FirstTimeSetWindowsStartupTask>();
    services.AddInitializer<DatabaseMigrationInitializer>();
    services.AddInitializer<CacheWarmupInitializer>();
}
```

### Initializer Options

```csharp
public class ParallelInitializer : ApplicationInitializerBase {
    public override int Priority => 50;  // Lower = runs earlier
    public override bool Parallelizable => true;  // Can run in parallel with others
    
    public override void Initialize() {
        // Initialization logic
    }
}
```

## 📋 Product Information

Product information is extracted from assembly attributes:

### Assembly Attributes

```csharp
// Properties/AssemblyInfo.cs
using Sphere10.Framework.Application;

[assembly: AssemblyCopyright("Copyright © Herman Schoenfeld 2008 - {CurrentYear}")]
[assembly: AssemblyProductDistribution(ProductDistribution.ReleaseCandidate)]
[assembly: AssemblyCompanyNumber("herman@sphere10.com")]
[assembly: AssemblyCompanyLink("https://sphere10.com")]
[assembly: AssemblyProductCode("2fbd6040-dece-45df-9f7a-7d2b562141ad")]
[assembly: AssemblyProductLink("https://sphere10.com/products/automouse")]
[assembly: AssemblyProductPurchaseLink("https://sphere10.com/products/automouse")]
[assembly: AssemblyProductHelpCHM("{StartPath}/AutoMouse.CHM")]
```

### Using Product Information

```csharp
var productInfo = Sphere10Framework.Instance.ServiceProvider
    .GetService<IProductInformationProvider>();

Console.WriteLine($"Product: {productInfo.ProductInformation.ProductName}");
Console.WriteLine($"Version: {productInfo.ProductInformation.ProductVersion}");
Console.WriteLine($"Company: {productInfo.ProductInformation.CompanyName}");
```

### Product Usage Tracking

```csharp
var usageServices = Sphere10Framework.Instance.ServiceProvider
    .GetService<IProductUsageServices>();

var usage = usageServices.ProductUsageInformation;
Console.WriteLine($"Times launched: {usage.NumberOfUsesByUser}");
Console.WriteLine($"First used: {usage.DateOfFirstUse}");
Console.WriteLine($"Last used: {usage.DateOfLastUse}");
```

## 📝 Command-Line Parsing

Built-in command-line parser with attribute-based configuration:

### Defining Options

```csharp
using Sphere10.Framework.Application;

public class Options {
    [Option('v', "verbose", HelpText = "Enable verbose output")]
    public bool Verbose { get; set; }
    
    [Option('i', "input", Required = true, HelpText = "Input file path")]
    public string InputFile { get; set; }
    
    [Option('o', "output", Default = "output.txt", HelpText = "Output file path")]
    public string OutputFile { get; set; }
    
    [Option('n', "count", Default = 10, HelpText = "Number of items to process")]
    public int Count { get; set; }
}
```

### Parsing Arguments

```csharp
var result = Parser.Default.ParseArguments<Options>(args);

result.WithParsed(options => {
    Console.WriteLine($"Input: {options.InputFile}");
    Console.WriteLine($"Output: {options.OutputFile}");
    Console.WriteLine($"Verbose: {options.Verbose}");
});

result.WithNotParsed(errors => {
    foreach (var error in errors) {
        Console.WriteLine($"Error: {error}");
    }
});
```

### Verb Commands

```csharp
[Verb("add", HelpText = "Add items to the list")]
public class AddOptions {
    [Value(0, Required = true, HelpText = "Item to add")]
    public string Item { get; set; }
}

[Verb("remove", HelpText = "Remove items from the list")]
public class RemoveOptions {
    [Value(0, Required = true, HelpText = "Item to remove")]
    public string Item { get; set; }
}

// Parse with verbs
Parser.Default.ParseArguments<AddOptions, RemoveOptions>(args)
    .WithParsed<AddOptions>(opts => AddItem(opts.Item))
    .WithParsed<RemoveOptions>(opts => RemoveItem(opts.Item));
```

## 🌐 Token Resolution

String tokens like `{ProductName}` and `{UserDataDir}` are automatically resolved:

```csharp
// Tokens are resolved in paths and strings
string logPath = Tools.Text.FormatEx("{UserDataDir}/{ProductName}/logs");
// Result: "C:\Users\John\AppData\Local\AutoMouse\logs"

string configPath = Tools.Text.FormatEx("{SystemDataDir}/{ProductName}/config.json");
// Result: "C:\ProgramData\AutoMouse\config.json"
```

Available tokens include:
- `{ProductName}` - Application name
- `{ProductVersion}` - Application version
- `{UserDataDir}` - User's local app data folder
- `{SystemDataDir}` - System-wide program data folder
- `{StartPath}` - Application startup directory
- `{CurrentYear}` - Current year

## 🔧 Built-in Services

The framework registers these services by default:

| Service | Description |
|---------|-------------|
| `ISettingsServices` | Settings save/load operations |
| `IProductInformationProvider` | Product metadata from assembly |
| `IProductUsageServices` | Usage tracking and statistics |
| `IHelpServices` | Help file/URL launching |
| `IWebsiteLauncher` | Open URLs in default browser |
| `IDuplicateProcessDetector` | Detect multiple instances |
| `IProductInstancesCounter` | Count running instances |
| `IAutoRunServices` | Windows startup registration |

## 🎯 Framework Options

Configure framework behavior with options:

```csharp
Sphere10Framework.Instance.StartFramework(
    Sphere10FrameworkOptions.EnableDrm | 
    Sphere10FrameworkOptions.BackgroundLicenseVerify |
    Sphere10FrameworkOptions.EnsureSystemDataDirGloballyAccessible
);
```

| Option | Description |
|--------|-------------|
| `EnableDrm` | Enable DRM/licensing support |
| `BackgroundLicenseVerify` | Verify license in background |
| `EnsureSystemDataDirGloballyAccessible` | Make system data dir accessible to all users |

## 📖 Related Projects

- [Sphere10.Framework](../Sphere10.Framework) - Core framework
- [Sphere10.Framework.Windows.Forms](../Sphere10.Framework.Windows.Forms) - Windows Forms integration
- [Sphere10.Framework.Web.AspNetCore](../Sphere10.Framework.Web.AspNetCore) - ASP.NET Core integration
- [Sphere10.Framework.Communications](../Sphere10.Framework.Communications) - RPC services with DI

## 🌍 Real-World Example

See [AutoMouse](https://github.com/HermanSchoenfeld/AutoMouse) for a complete production application using this framework, demonstrating:
- ModuleConfiguration for service registration
- SettingsObject with complex settings
- ApplicationInitializer for first-run tasks
- Product attributes for app metadata
- Windows Forms integration with LiteMainForm

## ✅ Status & Maturity

- **Core Framework**: Production-tested, stable
- **DI Integration**: Full support for Microsoft.Extensions.DependencyInjection
- **.NET Target**: .NET 10 (`net10.0`)
- **Thread Safety**: Application-wide; services should handle their own thread safety

## ⚖️ License

Distributed under the **MIT License**.

See the LICENSE file for full details. More information: [MIT License](https://opensource.org/license/mit)

## 👤 Author

**Herman Schoenfeld** - Software Engineer

Subclasses of `Sphere10Framework` can raise the existing `VersionChangeDetected` event through `OnVersionChangeDetected(previousVersion, currentVersion)`.
