<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💫 Sphere10.Framework.Application

**Complete application framework** providing dependency injection, modular architecture, settings management, lifecycle hooks, and product information for building production-ready Sphere10 Framework-based applications.

Sphere10.Framework.Application enables **rapid application development** by providing a complete infrastructure for **service composition, settings persistence, initialization/finalization pipelines, and modular configuration**—all integrated with Microsoft.Extensions.DependencyInjection.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Application` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [Attributes](Attributes/README.md) | Assembly attributes that declare a product's identity, links, distribution, help resources and settings secret, and class attributes that declare screen settings and user-editable values. | [source](Attributes/) |
| [CommandLine](CommandLine/README.md) | A bundled copy of the CommandLineParser library, which maps command-line arguments onto attributed option classes, reports parse errors and builds help screens. | [source](CommandLine/) |
| [Core](Core/README.md) | Running-instance detection, the automatic-start scope enumeration and the default product-services aggregate. | [source](Core/) |
| [Extensions](Extensions/README.md) | Extension methods for service collections and service providers, JSON HTTP responses and configuration connection strings. | [source](Extensions/) |
| [Framework](Framework/README.md) | The module contract that lets a library register its services before the framework builds the service provider. | [source](Framework/) |
| [IoC](IoC/README.md) | Registration and lookup of several implementations of one service type by name, on top of Microsoft.Extensions.DependencyInjection. | [source](IoC/) |
| [Lifecycle](Lifecycle/README.md) | Startup and shutdown task contracts with synchronous and asynchronous bases, the `Tools.Application` task runners, progress reporting, automatic-start helpers and the application version type. | [source](Lifecycle/) |
| [Logging](Logging/README.md) | A rolling-file logger configured from a `LoggingConfiguration` section of an `app.config` or `web.config` file. | [source](Logging/) |
| [Product](Product/README.md) | Product identity read from assembly attributes or supplied in code, usage counters, running-instance counts, string-token resolvers and the records of support requests. | [source](Product/) |
| [Rest](Rest/README.md) | A JSON-over-HTTP client with typed GET, POST, PATCH and DELETE calls relative to a base URL. | [source](Rest/) |
| [Settings](Settings/README.md) | Typed per-user and machine-wide settings objects with their storage providers, caching and encryption, plus helpers that read .NET configuration files. | [source](Settings/) |
| [Support](Support/README.md) | The optional transport contract for feature requests, comments, bug reports and crash reports, and the feature-request record. | [source](Support/) |
| [UI](UI/README.md) | UI-independent contracts, bases, fluent builders, decorators and the screen-host engine for applications made of plugins, blocks, menus, screens and wizards, which the WinForms and Blazor adapters implement. | [source](UI/) |

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`ModuleConfiguration`](ModuleConfiguration.cs) | class | Application module with priority `int.MinValue` that registers default settings, product, help, website and duplicate-process services, token resolvers and the usage initializer, then installs the settings providers and the settings secret |
| [`Sphere10Framework`](Sphere10Framework.cs) | class | Process framework singleton `Instance` that starts and ends the modules and process tasks, owns its `Disposables`, raises lifecycle events and creates rolling-file application loggers |
| [`Sphere10FrameworkBuilder`](Sphere10FrameworkBuilder.cs) | class | Fluent framework startup: `AddModule`, `AddServices`, `UseAssemblyInfo`, `WithProductInformation`, `WithOptions`, `RegisterModules` and `Start` with an owned or external service provider |
| [`Sphere10FrameworkOptions`](Sphere10FrameworkOptions.cs) | enum | Framework option flags: `Default` or `EnsureSystemDataDirGloballyAccessible` |
<!-- domains:end -->

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Application
```

## Shared UI framework

`Sphere10.Framework.Application.UI` contains the common application-block model used by the WinForms and Blazor adapters. The Application package targets `net10.0` and has no ASP.NET Core, Windows desktop or other UI framework dependency. The `UI` folder also contains the existing help, website launcher and user-interface services, which retain their `Sphere10.Framework.Application` namespace.

| Shared responsibility | Types |
|---|---|
| Plugin definitions and startup registration | `IApplicationPlugin`, `ApplicationPluginBase`, `ApplicationPlugin`, `ApplicationPluginDecorator`, `ApplicationPluginBuilder`, `ApplicationPluginRegistration<TPlugin>` |
| Running application and lifecycle | `IApplication`, `ApplicationBase`, `SyncApplicationBase`, `AsyncApplicationBase`, `ApplicationDecorator` |
| Application interactions and command execution | `IUserInterfaceServices`, `ApplicationUserInterfaceServices`, `UserInterfaceServicesDecorator` |
| Presentation command contracts | `IApplicationCommandProvider`, `ApplicationCommandLevel`, `ApplicationCommandLayer` |
| Menu and toolbar definitions | `IApplicationMenu<TItem>`, `ApplicationMenuBase<TItem>`, `ApplicationMenuItemBase`, `IApplicationMenuSeparator` |
| Wizard orchestration and configuration | `IWizard<TStep>`, `IWizard<TModel, TStep>`, `IWizardStep`, `WizardStepTraits`, `IWizardBuilder`, `WizardBase`, `Wizard`, `WizardDecorator`, `WizardBuilderBase` |
| Block, menu and item contracts | `IApplicationBlock`, `IApplicationMenu`, `IApplicationMenuItem`, `IScreenMenuItem` |
| Mutable metadata and notification handlers | `ApplicationBlockBase`, `ApplicationMenuBase<TItem>`, `ApplicationMenuItemBase` |
| Fluent builder composition and validation | `IApplicationUIBuilder<TProduct>`, `ApplicationBuilderBase`, `ApplicationPluginBuilderBase`, `ApplicationBlockBuilderBase`, `ApplicationMenuBuilderBase`, `ApplicationMenuItemBuilderBase` |
| Screen metadata validation and lifecycle | `ScreenActivationMode`, `ScreenKind`, `ApplicationScreenDefinition`, `IApplicationScreen` |
| Block navigation and screen hosting | `IApplicationScreenHost`, `IApplicationScreenHost<TBlock, TScreen>`, `ApplicationScreenHostBase<TBlock, TScreen>`, `ApplicationScreenHost<TBlock, TScreen>`, `ApplicationScreenHost`, `ApplicationScreenHostDecorator` |
| Single-screen operations | `ISingleApplicationScreenHost`, `ISingleApplicationScreenHost<TBlock, TScreen>`, `SingleApplicationScreenHostDecorator` |
| Multiple-screen operations | `IMultiApplicationScreenHost`, `IMultiApplicationScreenHost<TBlock, TScreen>`, `MultiApplicationScreenHostDecorator` |
| Awaitable menu actions | `ApplicationAction` |

Platform implementations use names such as `ApplicationBlock` and `IApplicationScreen` in `Sphere10.Framework.Windows.Forms` or `Sphere10.Framework.Web.AspNetCore.Blazor`; the namespace identifies the platform. Shared contracts remain in `Sphere10.Framework.Application.UI`. Qualify shared references or use a namespace alias where a platform type has the same name, preserving the shared contract at that boundary. Both platforms use the shared `ScreenActivationMode` directly.

The shared block base stores screen-registration metadata. `IApplication`, `IApplicationBlock`, `IApplicationScreen` and `IApplicationPlugin` inherit `IApplicationCommandProvider`, exposing portable `Menus` and `ToolBarItems` descriptors with empty defaults. Platform adapters own the native controls, rendering and merging. A block owns `ApplicationScreenDefinition` objects with immutable screen type, title, activation mode and kind. Its `DefaultScreenDefinition` references one of its `ScreenDefinitions`; screen commands reference those same objects. `Activators` returns the attached commands targeting that definition. Navigation is optional: a screen can be registered and activated without a menu entry.

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

Register screens independently through the owning block builder's `AddScreen` methods. `AddMenu` assigns the menu's `Parent`; it never registers screens. `AddItem` assigns the item's `Parent` and updates a resolved definition's reverse activator membership. Removing membership clears that parent; transferring a menu or item requires removing it from its current owner first. Removing a menu leaves its block-owned screens available for direct activation. `IApplicationBlock.Parent` identifies the running application, while `Source` identifies its original startup plugin; standalone blocks have no source plugin. UI services activate a screen command through `item.Parent.Parent`.

| Runtime relationship | Property |
|---|---|
| Item → owning menu → owning block → application | `item.Parent.Parent.Parent` |
| Block → contributing startup plugin | `block.Source` |
| Screen instance → host and owning block | `screen.Host`, `screen.Block` |
| Screen definition → attached activating commands | `definition.Activators` |

Use these object relationships when executing commands or accessing application services. Pass the registered command object to `ExecuteMenuItemAsync(item, cancellationToken)`; retained screens use their own `Host` and `Block`. A screen definition's `Activators` is reverse command membership, while a screen instance's ownership is maintained by its host.

Block builders collect explicit `AddScreen` registrations; `WithDefaultScreen` references an existing registration. Screens own their title, activation policy, platform activator and persistent key. Menu and toolbar items own command presentation and refer to those registrations. `AddShowScreenItem<TScreen>()` selects the first registration of that type in the owning block's declaration order. The exact-registration overload selects its supplied moniker. Programmatic type lookups and type-based default selection require one unambiguous registration.

The primary callback form returns the parent builder. Its optional reference overload takes the configuration callback first and `out registration` second; callers without a use for the reference use the ordinary overload. `ApplicationScreenRegistration<TScreen>` is an immutable identity token, never a screen instance or a reference retargeted to the latest built graph. Snapshot factories call `ApplicationScreenDefinition.Clone()` once per source definition in each copied graph. Its default, commands and activators use the same canonical cloned objects; separate snapshots have independent parents and reverse membership. Platform definitions override `Clone()` to retain their activation metadata. `WithTitle` and `WithActivationMode` copy through that virtual method and start with no copied activators.

`host.OpenScreenAsync(block, registration)` resolves a source moniker within the supplied block's current graph and follows the normal host lifecycle. Singletons are retained per owning block and registration, so two registrations of the same screen type can use different policies and activators. A sibling's explicit policy never supplies another registration's omitted policy.

Persistent identity is separate from the transient moniker token. `WithStableKey` supplies an explicit key; an unambiguous screen type gets an assembly-name/type-name key automatically. Duplicate screen types that need startup or settings persistence require explicit keys. Keys are independent of captions and construction order, and explicit keys cannot collide with another explicit or generated key in that block. The framework validates this at the persistence boundary. Ordinary registrations need no hand-written key. `AddPlugin`, `AddBlock`, `AddMenu` and `AddItem` also offer callback-first, `out`-second overloads that capture their configured definition products; these configuration references are distinct from runtime instances and graph-local screen monikers.

`Menus`, `Items`, `ScreenDefinitions`, `Activators`, `Blocks` and `AvailableBlocks` return defensive arrays; modifying array slots cannot change stored membership. Copies preserve element references within their graph. Presentation command merging borrows the original command references and callbacks without changing their owning parents.

`ApplicationScreenDefinition.ValidateDefinitions` checks a complete collection for valid screen types, activation modes and hosting kinds without changing metadata or membership. It accepts an optional platform type validator. Block registration additionally checks distinct registration tokens and persistent keys. The host validates a whole batch before changing host state and preserves each retained registration's captured lifetime. `IApplicationScreen` supplies writable `Host` and `Block` ownership slots, UI-independent help metadata, cancellation-aware activation/deactivation hooks and a navigation guard. `ApplicationAction.ExecuteAsync` receives the executing service provider and cancellation token; callbacks should resolve scoped services there. Owner-aware synchronous callbacks use `ApplicationAction.UIAction` and `Execute(IUserInterfaceServices)`; `ExecuteAsync` resolves those UI services from the executing provider. `PrimaryUIController` supplies the platform UI owner. Registration-time callbacks must not capture scoped services or mutable per-user state.

The [WinForms adapter](../Sphere10.Framework.Windows.Forms/README.md) retains controls, images, UI-thread dispatch and desktop screen ownership. The [Blazor adapter](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md) retains component validation, rendering, circuit scopes and browser interaction. Each supplies factories and platform validation to the shared engines. An ApplicationBlock describes navigation metadata; `ModuleConfiguration` continues to handle service registration and framework startup.

The [Application test suite](../../tests/Sphere10.Framework.Application.Tests/README.md) exercises the shared behavior using ordinary .NET classes and checks the framework assembly dependency graph. [NuGet validation](../../scripts/package-consumers/README.md) also builds a consumer referencing only the Application package and rejects UI dependencies in its resolved packages and framework references.

## Screen, block and plugin preferences

Screens declare their nested `SettingsObject` subclass with
`[Settings(typeof(...))]`; an optional typed `ScreenSettings` override provides
convenient access. Existing `[UseSettings(typeof(...))]` declarations remain
supported. Registration normally needs only `AddScreen<TScreen>("Title")`.
Explicit settings instances may supply independent initial values for registrations
of one screen type. Attribute, property and builder declarations must agree or
registration throws. Discovery reads type metadata without constructing controls
or invoking their getters; there is no settings-value factory.

Blocks and plugins declare `BlockSettings` / `PluginSettings` through their own
builder's `UseSettings<T>()` or `UseSettings(instance)`. Standard Options includes
those owners, service-only plugins and unopened screens with settings; screens
without settings are omitted. A class marked
`[UserEditable]` enables the automatic editor; `[Transient]` excludes runtime
members from editing and storage. All settings roots must derive from
`SettingsObject`; nested value DTOs need no settings base. Validation combines
`SettingsObject.Validate()`, property annotations and `IValidatableObject`. A
registration editor overrides `[Editor]` on the settings class, which in turn
precedes the `[UserEditable]` automatic editor.

Native screens use `ApplicationControl`'s existing initialization and model/UI
hooks. Typed `ScreenSettings` overrides must explicitly be non-browsable and
hidden from designer serialization. `GetBlockSettings<T>()` and
`GetPluginSettings<T>()` read accepted runtime owner values, including when the
screen has no own settings. Nonvisual services read the actual plugin/block
settings objects, observe `OnChanged` and save through those objects' existing
`Save()` / `SaveAsync()` methods. `application.Settings` exposes their provider.

Persistence uses the existing native `SettingsServices.UserSettings` /
`UserSettings.Provider` through `ISettingsProvider` asynchronous methods, preserving
cache/encryption behavior. Blazor uses the same `UserSettings` facade with its
specialized `BlazorSettingsProvider` bound during Framework/circuit initialization
and circuit activities. The browser provider does not replace the process default
shared by other users. The persisted value is the
actual settings class, without an envelope, token record or parallel store.
Standard dialogs edit actual owner values, keep local Cancel backups and handle
Reset, validation, accepted writes and failure/retry. No application-root settings
definition, profile, capture or settings-factory layer is required. Preserve
independent registration keys, original provider identity, encryption, cached
values and window-placement ownership. Legacy values are imported only when the
typed record is absent; Reset does not reimport them after restart.

See the [native screen settings pattern](../../docs/guidelines/patterns/winforms-screen-settings.md),
[Screen Options](../../docs/usage/screen-options.md) and
[per-user settings](../../docs/guidelines/patterns/user-ui-settings.md) for complete
examples, provider boundaries and verification.

## Application plugins

The runtime hierarchy is `IApplication` → `Blocks` → menus and screen definitions. A plugin is a named startup contribution that can supply several blocks. `LoadedPlugins` records the actual plugins whose startup registration completed successfully. Standalone blocks participate directly in the application hierarchy. The application's `Blocks` projects its `ScreenHost.Blocks`, so the host remains the single owner of live membership and navigation selection. Shell block icons select blocks; browsing another block does not require a screen switch.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

var plugin = new ApplicationPluginBuilder()
	.WithName("Administration")
	.AddBlock(new WorkspaceBlock { Name = "Users" })
	.AddServices(services => services.AddSingleton<AdministrationSettings>())
	.Build();

var services = new ServiceCollection();
plugin.Load(services); // Startup owner calls this before building its service provider.
// A startup owner can call plugin.Unload() during its own shutdown sequence.

public class AdministrationSettings : SettingsObject {
	public string Title { get; set; } = "Administration";
}
```

`ApplicationPluginBase.Load(IServiceCollection)` applies service configuration, invokes `OnLoaded`, then raises `Loaded`. `Unload` invokes `OnUnloaded`, then raises `Unloaded`. Registration failures propagate without announcing a successful load. The parameterless `Load()` is a notification-only compatibility entry point. These methods do not construct a service provider or enforce a once-only load policy: the startup owner controls calls, and runtime application adapters never load or unload plugins merely because a form or circuit opens or closes. Resolve scoped services when commands execute, rather than capturing them in startup definitions.

Platform startup registrars publish an `ApplicationPluginRegistration<TPlugin>` only after loading the plugin, validating its finalized definitions and registering its blocks. They assign each registered block's `Source` to the original plugin. The immutable receipt retains that plugin and copies finalized block membership; constructing a receipt validates existing source ownership without changing it or invoking lifecycle methods. Application factories resolve this receipt inventory to supply `LoadedPlugins`; an unrelated plugin registered directly in DI does not become a loaded startup plugin. The framework does not currently connect plugin `Unload` to process shutdown, application disposal or Blazor circuit disposal.

Startup registration rejects both duplicate successful plugin names and recursive attempts to load the same name. If registration fails, it discards newly added lifecycle task descriptors under the attempted name so a later successful plugin with that name cannot inherit abandoned tasks. Ordinary `AddServices` side effects retain their existing behavior.

`ApplicationPlugin` supports constructor configuration and init-only `Name`/`Blocks`; its arrays copy membership and retain block identity. Platform adapters can snapshot their own block metadata before forwarding it to shared storage. `ApplicationPluginBuilderBase<TBlock, TPlugin>` shares name validation, accumulated blocks, service callback composition and independent build inputs with typed platform builders. Empty applications and service-only plugins with no blocks are valid.

Typed plugin implementations derive from `ApplicationPluginBase<TBlock>` to share storage and lifecycle while remaining assignable to the existing `ApplicationPlugin`. The generic interface supplies the neutral `Blocks` projection for direct interface implementations. Because an inherited class implementation takes precedence over an interface default, the typed base retains one compatibility hook that sends neutral class reads through the typed virtual getter. Platform plugins use this shared hook instead of repeating explicit interface adapters.

Plugin startup registration validates names and block ownership: plugin names must be nonblank and unique with ordinal comparison, and each block object belongs to exactly one plugin. Blocks require nonblank names but are identified by reference. It returns an owned membership array preserving the supplied plugin instances. The same checks apply to individual plugins and captured registration receipts without rereading mutable plugin block membership. Platform interfaces extend `IApplicationPlugin` with typed block arrays; existing plugins remain the same startup definitions when accessed through shared or platform contracts.

## Running application and command scopes

`IApplication` represents the running application rather than service-container startup. It exposes array-valued `Blocks` and `LoadedPlugins`, `ScreenHost`, `HasUnsavedChanges` and a `Changed` event. `Blocks` reflects the host's current registered membership; `LoadedPlugins` preserves startup provenance, including service-only plugins with no blocks. Navigate through `ScreenHost`, inspect its `ActiveBlock` and `ActiveScreen`, and obtain interactions through `ScreenHost.UserInterfaceServices`. The application keeps no duplicate navigation state. `IApplication` and `IApplication` specialize that contract. Resolving the shared interface returns the same platform application instance. Disposal finalizes application tasks and owned resources, then releases subscriptions without taking ownership of the host, its interaction services, screens or plugin registrations.

`IApplication<TPlugin, THost>` and `IApplicationPlugin<TBlock>` provide typed plugin collections and a typed host. Applications, blocks, screens and plugins are shared command providers. Platforms specialize their descriptors through `IApplicationMenu<TItem>` and `IApplicationCommandProvider<TMenu, TItem>`; the inline bridges expose those same arrays through the shared owner interfaces. Consumers can read portable commands through the neutral contracts and use platform contracts for native presentation details. `ApplicationDecorator` forwards the running application's current command arrays rather than substituting empty defaults.

Platform ownership properties specialize the shared object relationships and bridge them inline at interface level. Keep these projections together under `#region Base Interface Bridges`; execution-direction adapters belong in the directional base classes described below.

`ApplicationBase` shares lifecycle tasks, owned resource cleanup, notifications and live projections of the host's blocks and dirty state. Derived applications call `ObserveScreenHost()` after assigning their host; it maintains each registered block's `Parent`, clears it on unregistration or application disposal, and preserves startup `Source`. Applications, blocks, screens and plugins expose array-valued `Menus` and `ToolBarItems` through their inherited command-provider contract.

The platform presentation shell composes the application, the active screen's source plugin, its owning block and the screen, in that order. Without an active screen, only application commands appear; browsing another navigation block does not alter the visible screen's command path. Applications supply commands through their owners rather than call internal validation or composition helpers.

Application contributions survive matching contextual captions or platform keys; within contextual contributions, a screen replaces a matching block command in its original position. Matching menus combine items and take the narrower menu's display metadata. Separators are normalized and Help remains last. Composition retains original command references and callbacks. The platform adapters own rendering, native merging and the internal shared algorithms.

`ScreenMode.SingleView` and `ScreenMode.MultiView` live in this shared namespace alongside `ScreenActivationMode`. Their screen retention rules belong to the shared screen host; platform adapters own presentation and view disposal.

## Application initialization and shutdown

Call `WithName(...)` before `AddInitializer` or `AddFinalizer`, including their factory overloads. An unnamed plugin throws `InvalidOperationException` immediately without adding a deferred registration.

`IApplication` provides synchronous `Initialize()`, `Finalize()` and `Exit()` operations alongside awaited `InitializeAsync()`, `FinalizeAsync()` and `ExitAsync()` counterparts. Following the repository hierarchy, the interface and neutral `ApplicationBase` require both directions. Choose `SyncApplicationBase` for synchronous lifecycle work or `AsyncApplicationBase` for awaited lifecycle work. The directional base supplies the opposite API and disposal direction; shared state, notifications, coalescing and ownership remain in `ApplicationBase`.

Application task inventories are protected implementation details of `ApplicationBase`. `IApplicationInitializer` / `IApplicationFinalizer` and their neutral bases declare both execution methods without interface implementations. Derive synchronous tasks from `SyncApplicationInitializerBase` / `SyncApplicationFinalizerBase` and override only `Initialize()` / `Finalize()`. Derive asynchronous tasks from `AsyncApplicationInitializerBase` / `AsyncApplicationFinalizerBase` and override only `InitializeAsync()` / `FinalizeAsync()`. Sync-first async adapters run the synchronous primitive on a worker using `Task.Run`. Async-first sync adapters start the asynchronous primitive outside the caller's synchronization context and block until completion, propagating errors. Plugins declare their application tasks through `AddInitializer<T>()` and `AddFinalizer<T>()`; platform plugin builders inherit these helpers. Ordinary service registrations continue to use `AddServices`. Unkeyed registrations keep their existing process-wide `Sphere10Framework` lifetime and are not replayed by the application:

```csharp
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

var plugin = new ApplicationPluginBuilder()
	.WithName("Workspace")
	.AddInitializer<WorkspaceInitializer>()
	.AddFinalizer<WorkspaceFinalizer>()
	.Build();

public sealed class WorkspaceInitializer : SyncApplicationInitializerBase {
	private readonly IApplication _application;

	public WorkspaceInitializer(IApplication application) {
		Guard.ArgumentNotNull(application, nameof(application));
		_application = application;
	}

	public override void Initialize() => _application.Logger.Info("Preparing workspace...");
}

public sealed class WorkspaceFinalizer : AsyncApplicationFinalizerBase {
	private readonly IApplication _application;

	public WorkspaceFinalizer(IApplication application) {
		Guard.ArgumentNotNull(application, nameof(application));
		_application = application;
	}

	public override Task FinalizeAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		_application.Logger.Info("Closing workspace...");
		return Task.CompletedTask;
	}
}
```

The helpers register transient tasks keyed by the built plugin's `Name`. Loading rejects duplicate plugin names; custom plugins must keep their names stable throughout their lifetime. The same task type can be registered by different plugins under their respective names. `ApplicationBase` resolves tasks for successful `LoadedPlugins` through the application's own service provider and caches them after application construction, allowing tasks to inject their application. Host registrations using `services.AddInitializer<T>(typeof(IApplication))` and `services.AddFinalizer<T>(typeof(IApplication))` remain supported for application-owned tasks. The combined inventory starts with these host-owned registrations in registration order, followed by plugins in `LoadedPlugins` order and each plugin's tasks in declaration order. It captures shutdown tasks during startup so fallback disposal can execute them without querying an already disposed provider. Protected task properties return defensive membership arrays; protected `SetInitializers` and `SetFinalizers` configure derived applications before startup. Both initialization forms share one attempt, raise `Initializing` / `Initialized`, and set `IsInitialized` on success. Pending async calls coalesce. Failed initialization is not retried; shutdown can still release partial startup resources. The protected `OnInitialize()` readiness hook and its async counterpart may return false before task execution to permit a later readiness attempt. Initializer priorities apply across the combined inventory within the existing sequential and parallelizable phases: sequential initializers run by ascending priority before parallelizable initializers. Finalizers execute in combined inventory order. `Tools.Application` supplies synchronous and awaited runners with logging and `ProgressHandler` reporting. `Sphere10Framework` retains its existing process task execution.

After startup has begun, both finalization forms raise `Finalizing` / `Finalized` and attempt every shutdown task. They release every resource in the application's existing `Disposables` bag even when a task or cleanup action fails. `SyncApplicationBase` uses synchronous task and disposal contracts; `AsyncApplicationBase` awaits task and asynchronous resource disposal. Each base adapts the opposite public API to that chosen direction. They share once-only cleanup and coalesce pending shutdown. Finalization waits for pending startup to finish. Cancelling an async-first finalization caller stops its wait while committed cleanup continues. A sync-first adapter can cancel before its worker begins; synchronous work already running completes normally. `DisposeAsync()` cancels pending startup and exit, completes the chosen shutdown direction and detaches application ownership. Synchronous disposal remains available; a pending async lifecycle operation on its synchronization context must be awaited before synchronous disposal. Errors are reported after cleanup. Repeated shutdown and disposal do not repeat tasks or owned cleanup. A lifecycle callback cannot dispose its own pending startup or finalization. Disposing an application that never began startup releases its bag and subscriptions without running shutdown tasks; this includes Blazor prerender scopes. Hosts await `FinalizeAsync()` before disposing their DI provider so shutdown tasks can still use scoped dependencies. Disposal is a fallback, not a guarantee that arbitrary DI-owned dependencies remain alive.

`Logger` exposes the registered Sphere10 `ILogger`, with `SystemLog.Logger` as a fallback. Screens can use `Block.Parent.Logger`; plugin tasks can inject `IApplication` and use the same logger. This is the framework logger, separate from ASP.NET Core's `Microsoft.Extensions.Logging.ILogger<T>`. Derived applications can configure the logger; borrowed loggers and DI resources remain under their existing ownership. Add only resources owned by the application to `Disposables`.

WinForms initializes the application after framework startup and block registration, while its enabled splash screen displays application task progress alongside existing framework logging and fades. Approved exit shows the shutdown splash and completes application and framework tasks before closing the main window. Service disposal follows the message loop. Blazor initializes on interactive circuit open and finalizes on circuit close. Temporary disconnects retain the application; prerendering does not execute circuit tasks. Plugins retain their existing startup registration and unload ownership.

The [WinForms tester](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md) loads two plugins with lifecycle tasks while retaining its process startup tasks. The [Blazor tester](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md) loads four plugins with circuit-scoped startup and shutdown tasks. Both demonstrate the same plugin builder helpers through their actual startup paths.

Use `await application.ExitAsync()` for coordinated shutdown, or the boolean `Exit()` counterpart at a synchronous platform boundary. The neutral application base leaves both forms and their paired work hooks abstract. `SyncApplicationBase` executes `ConfirmExit`, `CanExit` and `OnExit` synchronously, uses UI `AskYN`, and calls `Finalize()` for committed cleanup; its async public form adapts that same primitive. `AsyncApplicationBase` awaits `ConfirmExitAsync`, `CanExitAsync` and `OnExitAsync`, uses UI `AskYNAsync`, and awaits `FinalizeAsync()`; its synchronous form waits for that chosen primitive. Override the hooks in your application's chosen direction to customize the question, permissions or platform completion.

Both directions reuse `ScreenHost.CanNavigateAsync()` to check every retained screen (including hidden and permanent screens), then ask the active screen's owning block and the selected navigation block for permission. These collaborators currently expose only async guards. The sync-first permission hook explicitly starts and waits for each collaborator outside the caller's synchronization context, with cancellation of its wait, while the application confirmation and completion stay synchronous. This boundary does not call the application's async exit primitive. A block implements `CanExitAsync(IApplication application, CancellationToken cancellationToken)`; both blocks receive the running application, and a shared block is checked only once. The application argument supplies circuit-local context when a platform copies startup definitions.

Exit returns `false` on refusal and remains retryable. Concurrent requests share the pending exit; `ExitAsync(force: true)` bypasses confirmation and guards and supersedes a pending awaited question or collaborator guard wait. Forced exit still performs normal platform shutdown and finalization. `IsExiting` describes the pending operation and `HasExited` records successful completion; changes raise `Changed`. The initiating caller's cancellation token controls preparation, while cancelling a joining caller only cancels its wait. A genuinely synchronous confirmation or task already running follows its platform's blocking and cancellation behavior; `Task.Run` cannot interrupt that work. Once default logical shutdown begins, cleanup continues to terminal exit even if the initiating caller stops waiting; completed cleanup failures are logged. A refusal to begin or join cleanup, preparation failures and overridden platform completion failures remain retryable. A finalizer cannot start its own exit before cleanup completes. Exit hooks must not recursively await the same pending exit. WinForms finalizes the application before closing its main window; its runner also completes framework shutdown tasks under the splash before closure and disposes services after the message loop. Blazor completes logical application finalization and displays an exited workspace without terminating the server or unloading startup plugins.

## Shared application screen host

Application and screen lifetimes have separate owners. Start and drain shared
workers through application initializers/finalizers; use each screen's lifecycle
overrides for its own refreshes, subscriptions and capture leases. Hiding a
retained screen is not removal, and a leave guard is not cleanup. See
[application and screen lifetimes](../../docs/guidelines/patterns/application-ownership.md#application-and-screen-lifetimes)
for both platform hook mappings, worker delivery and shutdown rules.

Use `IApplicationScreenHost` for block navigation, current-screen hosting, view mode and unsaved-state checks. Use `ISingleApplicationScreenHost` to close the current screen, and `IMultiApplicationScreenHost` when working with retained collections, ordering or other tabs. Use `IUserInterfaceServices` for application interactions and menu-command execution. The Blazor and WinForms registrations expose their platform implementations through these neutral interfaces, preserving circuit or native application lifetimes. `IApplication.ScreenHost.UserInterfaceServices` exposes the host's interaction service. Resolve a platform interface when code needs component metadata, native controls or docking.

`await application.ScreenHost.ShowScreenUndockedAsync(screen)` requests direct modeless detached presentation of a configured instance without changing the main-frame selection. This optional presentation capability belongs to the shared contract; the logical host and Blazor throw `NotSupportedException`. WinForms supports it in MultiView for normal nonpermanent screens, retains the same instance, and permits subsequent guarded docking. A rejected request must not transfer an unretained supplied instance to the host. Ordinary `IMultiApplicationScreenHost.ShowScreenAsync(screen)` continues to show a supplied screen through the platform's normal presentation.

The generic screen-host interfaces provide neutral property and method projections, including argument validation and asynchronous return adaptation, for direct implementations. Reusable host bases and typed decorators retain their neutral class mappings so virtual overrides remain authoritative even when a subclass explicitly remaps the typed interface.

`IApplicationScreenHost` owns available and registered blocks, the selected navigation block, the current screen and lifecycle transitions. `IApplicationScreenHost<TBlock, TScreen>` supplies typed projections constrained to application blocks and screens. The single and multiple host contracts have corresponding typed variants. The dynamic shared implementation supports both capability interfaces; consumers depend on the operations they use. Hosting uses screen reference identity; there is no abstract session descriptor, runtime GUID or renderer-attachment API.

`SelectBlockAsync(block)` browses its navigation without leaving the current screen. `ActivateBlockAsync(block)` opens its configured default; `ActivateScreenAsync(block, type)` and the screen-menu overload resolve type policy before creating or selecting a screen. `OpenScreenAsync(block, definition)` accepts a canonical definition owned by that block. `AvailableBlocks` retains startup definitions while `Blocks` reflects registration. Removing a block preflights its owned screens, including permanent screens, before updating membership. Registering a known block restores its definitions and permanent screens after the current placeholder's leave guard permits the transition.

On `IMultiApplicationScreenHost`, `Screens` includes hidden singletons and `OpenScreens` contains normal screens in presentation order. An empty placeholder has no tab. `SelectedBlock` identifies the browsed navigation block; `ActiveBlock` identifies the active screen's owner, falling back to that selected block when no screen is active. Explicit screen selection restores its owner's navigation. Dirty-state notifications preserve a separately browsed block.

Command owners activate `Menus` / `ToolBarItems` once through protected template hooks and return membership snapshots. Configure command membership before activation. Native shells rebuild views from the host form and active plugin/block/screen, using their existing merge controls; mutable captions and checked/enabled state remain on the original commands. Detached native windows have a local active-screen adapter that delegates navigation and lifetime to the root host.

The host assigns a screen's `Host` and `Block` before activation and lifecycle hooks. They remain attached while a single instance is hidden, and clear after actual removal cleanup or host disposal. A rejected deactivation guard preserves ownership. Shared host cleanup detaches those slots; platform adapters retain responsibility for disposing their views and screen instances.

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using Sphere10.Framework;
using Sphere10.Framework.Application.UI;

public static class WorkspaceCommands {
	public static Task<IApplicationScreen> OpenSettingsAsync(IApplicationBlock settingsBlock) {
		Guard.ArgumentNotNull(settingsBlock, nameof(settingsBlock));
		Guard.Argument(settingsBlock.Parent != null, nameof(settingsBlock), "The settings block must be registered with an application.");
		return settingsBlock.Parent.ScreenHost.ActivateBlockAsync(settingsBlock);
	}

	public static Task<bool> CloseOtherTabsAsync(IMultiApplicationScreenHost host) {
		var current = host.ActiveScreen;
		var closing = host.OpenScreens.Where(screen => !ReferenceEquals(screen, current)).ToArray();
		return host.CloseScreensAsync(closing);
	}
}
```

`ISingleApplicationScreenHost.CloseActiveScreenAsync` and `CanCloseActiveScreenAsync` work on the current screen. The multiple host contract adds reference-based Show, Close, CanClose, Move and batch Close operations. `ApplicationScreenHostDecorator`, `SingleApplicationScreenHostDecorator` and `MultiApplicationScreenHostDecorator` forward their respective contracts; choose the matching decorator when overriding a capability.

Closing the active tab selects the first surviving tab after it in presentation order, then the nearest surviving tab before it when none follows. Batch closure uses the order before any removals, including after tabs have been reordered. Closing inactive tabs preserves the current selection. If no tab survives, selection becomes null; closure does not activate an empty-screen definition. Startup and explicit empty-screen navigation retain their existing placeholder behavior.

The writable `ScreenMode` property supports safe configuration: the same value, promotion to multiple view, or switching to single view when no other ordinary open screen requires closure. It rejects a switch requiring lifecycle work before changing any state. Runtime controls must await `TrySetScreenModeAsync(mode)`, which preflights all affected guards before removing anything; the synchronous setter never blocks on an asynchronous guard. A change during an in-progress transition is also rejected.

Permanent screens reject ordinary closure. `CanNavigateAsync` checks all retained screens. `NotifyScreenChanged(screen)` reports changes to a retained screen's title, commands or dirty state. The host's asynchronous engine serializes lifecycle work, checks cancellation before committing selection or removal, and propagates post-commit activation failures. Lifecycle callbacks must not await another transition on the same host.

`ApplicationScreenHost<TBlock, TScreen>` shares registration-specific policies, singleton retention, selection, ordering and guards. Its overridable `CreateScreen`, reuse and removal hooks let adapters own platform presentation. The neutral factory resolves or creates the actual screen through dependency injection. Native adapters create and own controls; Blazor keeps its renderer proxy, session GUIDs, runtime reuse keys, parameters and attachment bookkeeping in the Blazor library. Those presentation details do not enter the abstract host contract. Named routing IDs and lookup APIs live in Blazor. Shared menu merging uses display captions by default and accepts adapter-supplied key selectors when a platform needs another semantic identity.

`IUserInterfaceServices` provides application dialogs, status, reporting, exit, UI dispatch and menu/action execution. `ApplicationUserInterfaceServices` borrows the host for screen commands without retaining another block or screen collection. The neutral host uses a registered interaction service when available; otherwise it lazily owns one shared command-capable fallback. Host disposal releases that fallback and leaves registered services to their independent owner. Platform interaction implementations supply dialogs and native/browser dispatch.

Actions execute outside the host's lifecycle gate and may navigate through `ScreenHost`. Owner-aware callbacks receive the interaction service executing the command, while asynchronous callbacks receive the executing scoped provider. The interaction service's `Changed` event reports interaction/status changes; host membership and screen changes are reported by the host. Disposing UI services does not dispose the borrowed host or platform screens.

The [host tests](../../tests/Sphere10.Framework.Application.Tests/UI/ApplicationScreenHostTests.cs) protect lifecycle and retention, and the [navigation tests](../../tests/Sphere10.Framework.Application.Tests/UI/ApplicationScreenHostNavigationTests.cs) cover block selection and guarded membership changes. The [capability tests](../../tests/Sphere10.Framework.Application.Tests/UI/ApplicationScreenHostCapabilityTests.cs) cover the interface split and safe synchronous and asynchronous mode changes. The [identity tests](../../tests/Sphere10.Framework.Application.Tests/UI/ApplicationScreenIdentityTests.cs) exercise actual screen references, including distinct screens with equal values. The [UI-services tests](../../tests/Sphere10.Framework.Application.Tests/UI/ApplicationUserInterfaceServicesTests.cs) cover command execution, interactions and the responsibility boundary.

## Shared fluent builders

The fluent methods live in this library. WinForms and Blazor inherit the same configuration, validation and child-builder composition; their adapters supply native products, supported screen/step checks, and platform presentation options.

| Shared base | Responsibility |
|---|---|
| `ApplicationBuilderBase<TSelf, TApplication, TPlugin, TPluginBuilder, TResult>` | Application title, existing plugins, `AddPlugin(builder => ...)`, deferred application callbacks and a terminal `Build`. The platform registers or creates its host without the base resolving services. |
| `ApplicationPluginBuilderBase<TSelf, TBlock, TBlockBuilder, TPlugin>` | Plugin name, service callbacks, `AddInitializer<T>()`, `AddFinalizer<T>()`, existing blocks and `AddBlock(builder => ...)`. |
| `ApplicationBlockBuilderBase<TSelf, TMenu, TMenuBuilder, TBlock>` | Block metadata, default screen policy, existing menus and `AddMenu(builder => ...)`. |
| `ApplicationMenuBuilderBase<TSelf, TItem, TItemBuilder, TMenu>` | Menu metadata, existing items and `AddItem(builder => ...)`. |
| `ApplicationMenuItemBuilderBase<TSelf>` | Command presentation, actions and references to independently registered screens. Platform factories reject unsupported item kinds. |
| `WizardBuilderBase<TSelf, TModel, TStep, TWizard>` | Fluent wizard setup, reset, ordered steps, cancellation, callbacks and build validation. |

`AddPlugin`, `AddBlock`, `AddMenu` and `AddItem` follow `ProtocolBuilder`: create a child builder, invoke its callback, call `Build()` and register the product through the corresponding `Add` method. `AddPlugin`, `AddBlock`, `AddMenu` and `AddItem` accept existing definitions. Convenience `Add` methods construct definitions directly from their arguments without invoking a child builder.

`IApplicationUIBuilder<out TProduct>` lets a parent accept child builders through their product interfaces. The self type preserves fluent platform return types; the smaller state bases remain available for custom builders that need a different facade. New platform adapters should override factories and validation hooks or add genuine platform options, rather than repeat `WithName`, `AddBlock`, `AddMenu` or wizard lifecycle methods. Web icon URLs, native `Image` objects, Razor step constraints and DI registration stay in their respective libraries.

Native builders retain their existing live-product ownership; Blazor factories produce registration snapshots. Application host registration is terminal so repeating `Build` cannot repeat service-registration side effects. Plugin service callbacks and application configuration callbacks remain deferred to the existing startup and scoped application paths.

Keep each builder invocation as one continuous indented block without blank separator lines. Put each configuration call on its own line, indent nested calls one tab below their owner and align multiline closing parentheses with the owning call. See [the code guide](../../docs/guidelines/code-styling.md#fluent-configuration-hierarchy), [WinForms plugin configuration](../Sphere10.Framework.Windows.Forms/README.md#grouping-blocks-into-application-plugins), and [Blazor examples](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md). Both testers and the package-consumer fixtures use these inherited APIs.

## Default, permanent and empty screens

Ordinary Blazor and WinForms screens default to `ScreenActivationMode.MultiInstance`: each activation creates a new screen. Choose `SingleInstance` explicitly for a retained screen, such as Settings; permanent and empty-screen test scenarios keep their declared lifetimes. Enum numeric values are unchanged.

Both UI adapters use the same screen metadata and selection rules. Add the screen
to the block, then select its canonical definition/moniker with
`WithDefaultScreen(registration)` or use the unambiguous
`WithDefaultScreen<TScreen>()` convenience. Defaults do not add a screen or create
another definition. Startup follows plugin contribution, block and screen
registration order; `Position` controls visible block order. Explicit navigation
takes precedence over the startup selection.

The block builder resolves defaults against independently added owned definitions.
Missing/foreign references and ambiguous type defaults fail graph validation.
Menus and toolbar items resolve references to those same canonical definitions;
their captions do not change screen metadata. Type-based menu conveniences choose
the first matching registration in the owning block; exact monikers choose their
specific registration. Activation policy, activator and settings remain owned by
the screen definition. Explicit policies for different registrations of the same
type can differ; an unspecified sibling retains its own default.

| Configuration | Behavior |
|---|---|
| `ScreenActivationMode.MultiInstance` (default) | Each activation opens an independent screen instance. |
| `ScreenActivationMode.SingleInstance` | Reopening selects the retained instance. Ordinary tabs can be closed. |
| `ScreenActivationMode.PermanentSingleton` | Automatically opens one instance, remains open for the block's lifetime and cannot be closed through tab commands. Other screens can still be selected. |
| `ScreenKind.Empty` + `SingleInstance` | A tabless placeholder appears only when no normal screen is open; its instance is retained while hidden. |
| `ScreenKind.Empty` + `MultiInstance` | The placeholder is destroyed when a normal screen opens and recreated when the workspace becomes empty again. |

`Empty` describes the screen's role, separately from its activation mode. It never has a tab and cannot be combined with `PermanentSingleton`. A permanently open normal screen therefore keeps an empty placeholder hidden. Administrative block removal ends its screens' lifetime, including permanent screens, after applicable navigation guards permit removal; normal application shutdown also disposes them. A permanent screen does not make application shutdown impossible.

Application startup resolves plugin membership against registered blocks and appends standalone blocks in their supplied order. Captured startup membership preserves registration order even when a plugin later changes its live `Blocks`. Hosts use the immutable definitions already owned by each block, validating command references and lifetime/role consistency, and inspect the blocks' selected default references. Hosts can specialize startup order through `GetStartupScreen` without changing the definition model. The first explicitly marked empty candidate is preferred; otherwise the first empty candidate is used. Platform snapshots resolve source monikers to their own canonical definitions without retargeting another graph or sharing retained instances.

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

`MoveNextAsync` and `MovePreviousAsync` provide awaited navigation. `UpdateStepsAsync` and `RemoveStepAsync` let platform adapters update their displayed controls before returning. `WizardStepUpdateType` supports injecting steps, replacing the remaining branch, replacing all steps and removing subsequent steps. The workflow returns defensive `Steps` arrays while retaining step and model identities. Finish and Cancel share an explicit lifecycle state and operation gate: concurrent terminal operations are rejected while busy; a declined or failed callback leaves a retryable workflow; successful completion runs once. Callbacks must not recursively finish or cancel the same wizard.

Dynamic steps implement `IWizardStep` and expose `Traits = WizardStepTraits.AssumeHasNext` when their navigation callback creates the next step. `HasNext` then advertises Next before a successor is registered. The callback must register a real successor before navigation; a missing successor returns an error without changing the current index or finishing. An assumed-next step cannot finish directly. Ordinary terminal steps use `WizardStepTraits.None`. Hosts rendering definitions such as component `Type` values bind the live instance's traits through `StepTraitsResolver`; they restore the previous resolver when detaching. `WizardDecorator` forwards this resolver and its change notification.

`WizardBuilderBase<TSelf, TModel, TStep, TWizard>` centralizes the fluent setup methods, reset/start requirements, callbacks and build validation over its shared state base. The shared `IWizardBuilder<TSelf, TModel, TStep, TWizard>` also carries the fluent contract, so interface-returning chains retain the same capabilities. Platform builders supply their step validation and factories. Use `WinFormsWizardBuilder<TModel>` for native dialogs, or `BlazorWizardBuilder<TModel>` from the chosen Blazor wizard generation. Their adapters keep rendering, screen initialization and component validation in the UI libraries while consuming the same workflow. `WizardDecorator` forwards the common contract for behavior extensions.

## Awaitable UI services

`IUserInterfaceServices.ShowSendCommentDialog`, `ShowSubmitBugReportDialog`, `ShowRequestFeatureDialog`, and `ShowAboutBox` return `Task`. Await them to preserve sequencing and observe errors. Implementations with no UI work return `Task.CompletedTask`.

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
			.AddModule<Sphere10.Framework.Application.ModuleConfiguration>()
			.AddModule<Sphere10.Framework.Windows.Forms.ModuleConfiguration>()
			.StartWinFormsApplication();
	}
}
```

## 🏗️ Core Architecture

### Process framework composition

`Sphere10Framework.Instance` composes process services and modules. Its builder registers explicitly selected modules, builds or borrows a service provider, and executes module hooks plus unkeyed process tasks. `IApplication` owns the running UI application's blocks, initialization, shutdown and exit. A Blazor circuit has its own scoped application even when startup plugin definitions are shared.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;

Sphere10Framework.Instance
	.Build()
	.AddModule<Sphere10.Framework.Application.ModuleConfiguration>()
	.AddModule<StartupTasksModule>()
	.Start();

var productInformation = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IProductInformationProvider>();
// Call EndFramework() during process shutdown after the running application's finalization.
```

Modules are explicitly selected with `AddModule<T>()` or `AddModule(instance)`. They are registered and initialized in descending module priority order. `Start()` owns the provider it builds. To use an external provider, register the builder's modules into its service collection with `RegisterModules(services)`, then call `Start(provider)`; that provider remains owned by its host. Platform startup builders compose these operations for their runtime.

### Process and application lifecycle

```
Sphere10Framework.Instance.Build().AddModule<...>().Start()
    ↓
    1. Apply AddServices callbacks and module RegisterComponents()
    2. Build ServiceProvider
    3. Call module OnInitialize() hooks
    4. Execute unkeyed IApplicationInitializer process tasks
    ↓
Platform starts IApplication.InitializeAsync()
    ↓
    Execute application-owned and successfully loaded plugin tasks
    ↓
Application runs...
    ↓
Approved IApplication.ExitAsync() / host application finalization
    ↓
    Complete application tasks and owned resources
    ↓
Sphere10Framework.Instance.EndFramework()
    ↓
    1. Execute unkeyed IApplicationFinalizer process tasks once
    2. Call OnFinalize() on each module
    3. Dispose the owned ServiceProvider and process resources
```

The WinForms runner completes process shutdown tasks under the shutdown splash before closing the main window, then calls `EndFramework()` after the message loop to dispose services. Blazor circuit shutdown completes its application tasks; the ASP.NET Core host retains responsibility for process service lifetime.

## 🧩 ModuleConfiguration Pattern

Use `ModuleConfiguration` for process service composition and framework hooks. Plugins use `AddServices` for component registration and their named `AddInitializer` / `AddFinalizer` helpers for running application tasks. Unkeyed module task registrations belong to the process lifetime.

### Creating a Module

```csharp
using System;
using Sphere10.Framework;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

public class StartupTasksModule : ModuleConfigurationBase {
	public override void RegisterComponents(IServiceCollection services) {
		services.AddInitializer<FirstRunInitializer>();
		services.AddFinalizer<LogShutdownFinalizer>();
	}

	public override void OnInitialize(IServiceProvider serviceProvider) {
		base.OnInitialize(serviceProvider);
		// Process initialization before unkeyed startup tasks.
	}

	public override void OnFinalize(IServiceProvider serviceProvider) {
		base.OnFinalize(serviceProvider);
		// Process cleanup after unkeyed shutdown tasks.
	}
}
```

The task classes are shown below. Register this module with `AddModule<StartupTasksModule>()` together with the application module that supplies product services.

### Module Priority

Modules are executed in priority order. Set `Priority` to control execution order:

```csharp
public class ModuleConfiguration : ModuleConfigurationBase {
	public override int Priority => int.MinValue;  // Execute last (lowest priority)
	// Use int.MaxValue to execute first.
}
```

## ⚙️ Settings Management

### Defining Settings Classes

Settings inherit from `SettingsObject` and use `[DefaultValue]` attributes:

```csharp
using System;
using System.ComponentModel;
using Sphere10.Framework.Application;

public class AutoMouseSettings : SettingsObject {

	[DefaultValue(true)]
	public bool AutoStartProgram { get; set; }

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

Native initialization supplies these facades with the configured process providers. Blazor uses the same `UserSettings` facade with its circuit-bound browser provider, isolating browser/account preferences from the operating-system user and other circuits. See the [settings guide](Settings/README.md) for provider registration, caching, encryption and lifetime details.

```csharp
// User-specific settings
var userPrefs = UserSettings.Get<UserPreferences>();

// System-wide settings (shared by all users)
var globalConfig = GlobalSettings.Get<SystemConfiguration>();
```

### Settings in UI Controls

Hosted screens declare `[Settings(typeof(...))]` and use the existing
`ApplicationControl` model/UI lifecycle. `CopyModelToUI` presents the actual
owner values; `CopyUIToModel` captures deliberate edits and marks changed settings.
The accepted save/close boundary awaits persistence. Copying values does not itself
authorize a write. See the [native screen settings pattern](../../docs/guidelines/patterns/winforms-screen-settings.md).

Standalone controls can retain a provider-bound `SettingsObject` obtained through
`UserSettings`, present/capture its values through their existing hooks, and save
it from an explicit accepted action. A screen declaration attribute does not
replace a standalone control's persistence owner. Reusable grid/log controls bind
to a screen's typed section when hosted, suppressing their independent record.

## 🚀 Process initialization and finalization

Unkeyed initializers execute during `Sphere10FrameworkBuilder.Start()` after module initialization, through the same `Tools.Application` runner used by runtime applications. `StartWinFormsApplication` constructs its single application after the module hooks and uses that exact `IApplication.Logger` for global initializer diagnostics before registering blocks or creating screens. Native process tasks can inject this application and log through `application.Logger`. Task progress also raises `Sphere10Framework.Progress`, so a startup splash works with a custom logger.

Default and ASP.NET Core startup use the registered Sphere10 `ILogger`, with `SystemLog.Logger` as fallback, without constructing a UI application: a browser circuit's application does not exist at this stage. Register that logger as a singleton to share it with `ApplicationBase.Logger` and inject it into portable process tasks that emit their own diagnostics. Unkeyed finalizers execute during process shutdown before module finalization and provider disposal. Use plugin builder helpers for tasks belonging to a running application or Blazor circuit; those named registrations are described under [application initialization and shutdown](#application-initialization-and-shutdown).

If startup fails, a provider created by the builder disposes its constructed services without running process finalizers for the incomplete startup. A native root that has not begun application initialization releases its owned bag without running application tasks. An externally supplied provider retains its external owner.

### Creating process tasks

```csharp
using Sphere10.Framework;
using Sphere10.Framework.Application;

public sealed class FirstRunInitializer : SyncApplicationInitializerBase {
	private readonly IProductUsageServices _usage;
	private readonly ILogger _logger;

	public FirstRunInitializer(IProductUsageServices usage, ILogger logger) {
		Guard.ArgumentNotNull(usage, nameof(usage));
		Guard.ArgumentNotNull(logger, nameof(logger));
		_usage = usage;
		_logger = logger;
	}

	public override void Initialize() {
		if (_usage.ProductUsageInformation.NumberOfUsesByUser == 1)
			_logger.Info("Preparing the first application session...");
	}
}

public sealed class LogShutdownFinalizer : SyncApplicationFinalizerBase {
	private readonly ILogger _logger;

	public LogShutdownFinalizer(ILogger logger) {
		Guard.ArgumentNotNull(logger, nameof(logger));
		_logger = logger;
	}

#pragma warning disable CS0465 // The existing task contract names its synchronous operation Finalize.
	public override void Finalize() => _logger.Info("Completing process shutdown...");
#pragma warning restore CS0465
}
```

`StartupTasksModule` registers these classes through `services.AddInitializer<FirstRunInitializer>()` and `services.AddFinalizer<LogShutdownFinalizer>()`, alongside `services.AddSingleton<ILogger>(new ConsoleLogger())`. Both are synchronous tasks; their directional bases provide async adapters. For awaited work, derive from the corresponding `Async...Base` and override only the asynchronous primitive. Process tasks use process services and the configured logger; plugin tasks can resolve their running `IApplication` and its scoped services and should log through `application.Logger`.

### Initializer priority and parallel execution

```csharp
public class ParallelInitializer : SyncApplicationInitializerBase {
	public override int Priority => 50;
	public override bool Parallelizable => true;

	public override void Initialize() {
		// Independent initialization work.
	}
}
```

Sequential initializers run first in ascending task priority order; parallelizable tasks run in the subsequent parallel phase. Task priority does not interleave those phases or change finalizer registration order. Keep dependencies between startup tasks sequential. These rules also apply to the combined application-owned and plugin-owned inventory run by `Tools.Application`.

## 📋 Product Information

Product information is extracted from assembly attributes:

### Assembly Attributes

```csharp
// Properties/AssemblyInfo.cs
using System.Reflection;
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

Application builders can select assembly attributes with `.UseAssemblyInfo()` or
`.UseAssemblyInfo(typeof(Program).Assembly)`, or supply information directly:

```csharp
Sphere10Framework.Instance
	.Build()
	.WithProductInformation(product => {
		product.ProductName = "Operations";
		product.CompanyName = "Example Company";
		product.ProductVersion = new ApplicationVersion { Major = 1 };
	})
	.AddModule<Sphere10.Framework.Application.ModuleConfiguration>()
	.Start();
```

The native `BuildWinFormsApplication()` and web `BuildBlazorApplication()` builders
expose the same selection methods. `AssemblyInfoProductServicesProvider` reuses
the existing attribute reader; `ProductServicesProvider` retains explicitly
supplied information. Both expose `IProductServicesProvider`, which combines the
existing information, usage and instance contracts. Usage and instance services
are resolved only when requested, so reading a product name does not load usage
settings. The existing `IProductInformationProvider` resolves the selected provider;
module defaults preserve an already registered information provider. Explicit
selections replace earlier selections. No DRM configuration is added here.

Options uses the product name for its application root. `WithTitle` remains the
web host's displayed caption; settings IDs remain independent of both captions.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework;
using Sphere10.Framework.Application;

var productInfo = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IProductInformationProvider>();
SystemLog.Info($"Product: {productInfo.ProductInformation.ProductName}");
SystemLog.Info($"Version: {productInfo.ProductInformation.ProductVersion}");
SystemLog.Info($"Company: {productInfo.ProductInformation.CompanyName}");
```

### Product Usage Tracking

```csharp
var usageServices = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IProductUsageServices>();
var usage = usageServices.ProductUsageInformation;
SystemLog.Info($"Times launched: {usage.NumberOfUsesByUser}");
SystemLog.Info($"First used (UTC): {usage.FirstUsedDateByUserUTC}");
SystemLog.Info($"Days used: {usage.DaysUsedByUser}");
```

## 📝 Command-Line Parsing

Built-in command-line parser with attribute-based configuration:

### Defining Options

```csharp
using CommandLine;

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
| `IProductInformationProvider` | Product information selected from assembly attributes or the builder |
| `IProductServicesProvider` | Selected product information and the existing usage/instance services |
| `IProductUsageServices` | Usage tracking and statistics |
| `IHelpServices` | Help file/URL launching |
| `IWebsiteLauncher` | Open URLs in default browser |
| `IDuplicateProcessDetector` | Detect multiple instances |
| `IProductInstancesCounter` | Count running instances |
| `IAutoRunServices` | Windows startup registration |

## 🎯 Framework Options

Configure framework behavior with options:

```csharp
Sphere10Framework.Instance
	.Build()
	.WithOptions(Sphere10FrameworkOptions.EnsureSystemDataDirGloballyAccessible)
	.AddModule<Sphere10.Framework.Application.ModuleConfiguration>()
	.Start();
```

| Option | Description |
|--------|-------------|
| `EnsureSystemDataDirGloballyAccessible` | Make system data dir accessible to all users |

Application licensing is no longer part of Framework. License-specific DTOs, assembly attributes, services, background verification and framework-option flags have been removed. Generic hosts perform application lifecycle work without implicit license checks; applications supply their own licensing integrations. Product metadata and per-user/system usage counting remain general-purpose.

## 📖 Related Projects

- [Sphere10.Framework](../Sphere10.Framework) - Core framework
- [Sphere10.Framework.Windows.Forms](../Sphere10.Framework.Windows.Forms) - Windows Forms integration
- [Sphere10.Framework.Web.AspNetCore.Blazor](../Sphere10.Framework.Web.AspNetCore.Blazor) - Blazor application integration
- [Sphere10.Framework.Web.AspNetCore](../Sphere10.Framework.Web.AspNetCore) - ASP.NET Core integration
- [Sphere10.Framework.Communications](../Sphere10.Framework.Communications) - RPC services with DI

## 🌍 Demo applications

The repository testers exercise the current application root, plugin loading, multiple blocks, object-based commands, parent ownership and directional lifecycle tasks:

- [WinForms tester](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md): two plugins, process and plugin lifecycle tasks, startup/shutdown splashes and coordinated main-window closure.
- [Blazor tester](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md): four plugins, circuit-scoped startup/shutdown tasks, application commands and retained screens.

## ✅ Status & Maturity

- **Core Framework**: Production-tested, stable
- **DI Integration**: Full support for Microsoft.Extensions.DependencyInjection
- **.NET Target**: .NET 10 (`net10.0`)
- **Thread Safety**: Application lifecycle operations coordinate their own state; services remain responsible for their own concurrency and platform thread affinity.

## ⚖️ License

Distributed under the **MIT License**.

See the LICENSE file for full details. More information: [MIT License](https://opensource.org/license/mit)

## 👤 Author

**Herman Schoenfeld** - Software Engineer

Subclasses of `Sphere10Framework` can raise the existing `VersionChangeDetected` event through `OnVersionChangeDetected(previousVersion, currentVersion)`.
