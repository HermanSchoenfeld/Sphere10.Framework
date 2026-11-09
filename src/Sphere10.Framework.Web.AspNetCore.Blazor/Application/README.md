# Application

The Blazor application workspace model of blocks, menus, screen registrations, the circuit-scoped screen host and the per-circuit application aggregate.

These types specialize the shared contracts and bases of `Sphere10.Framework.Application.UI` for Blazor. They live in the root `Sphere10.Framework.Web.AspNetCore.Blazor` namespace and reuse the shared names, so qualify a shared type with an alias such as `using SharedUI = Sphere10.Framework.Application.UI;` when both are in scope. Start with `services.BuildBlazorApplication()`, which returns an `ApplicationBuilder`. Its `Build()` registers the branding, plugins and application configuration in the existing service collection and must be called once. Blocks come from `ApplicationBlockBuilder`, either directly through `AddApplicationBlock` or inside a plugin built with the [Plugins](../Plugins/README.md) builder.

Each interactive circuit gets one `ApplicationScreenHost`, registered under `IApplicationScreenHost` and the shared single and multi host contracts, and one `Application` that projects that host's blocks and the loaded plugins. The host retains an `ApplicationScreenSession` for every open screen. The Razor renderer creates and disposes the screen component, which derives from `ApplicationScreen` and attaches itself to its session. Host transitions are serialized, so screen guard and lifecycle callbacks must not await another transition on the same host. The shell that renders this model is in [UI](../UI/README.md), and per-user preferences are stored by the browser settings provider in the `Settings` subfolder.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Application`](Application.cs) | class | Circuit-local application that projects the screen host's live blocks and the loaded startup plugins, holds application menus and toolbar items, and runs initialize, finalize and exit. |
| [`ApplicationBlock`](ApplicationBlock.cs) | class | Web block with a title, icon URL, tooltip, menus, toolbar items, settings, screen registrations and an optional default screen. |
| [`ApplicationBlockBuilder`](ApplicationBlockBuilder.cs) | class | Fluent builder that adds screens, menus, menu items and toolbar items to a block and builds an `ApplicationBlock`. |
| [`ApplicationBuilder`](ApplicationBuilder.cs) | class | Returned by `BuildBlazorApplication`, it collects the title, favicon, plugins and application callbacks and registers them in the service collection on `Build()`. |
| [`ApplicationMenu`](ApplicationMenu.cs) | class | Immutable block menu with an ID, icon, text and an array of menu items. |
| [`ApplicationMenuBuilder`](ApplicationMenuBuilder.cs) | class | Fluent builder for a block menu that adds show-screen items, action items and other items. |
| [`ApplicationMenuGroup`](ApplicationMenuGroup.cs) | class | Group of action commands that application toolbars render as a dropdown. |
| [`ApplicationMenuItemBase`](ApplicationMenuItemBase.cs) | abstract class | Base Blazor menu item with ID, icon, title, visibility and enablement, and hover and select notifications. |
| [`ApplicationMenuItemBuilder`](ApplicationMenuItemBuilder.cs) | class | Fluent builder for one menu item's ID, title, icon, visibility, enablement condition and child items. |
| [`ApplicationOptions`](ApplicationOptions.cs) | record | Immutable singleton host branding with the title, favicon URL and favicon content type, readable without a circuit. |
| [`ApplicationScreen`](ApplicationScreen.cs) | abstract class | Base Razor component for a hosted screen that attaches to its session and exposes settings, commands, unsaved-change state and activation callbacks. |
| [`ApplicationScreenActivation`](ApplicationScreenActivation.cs) | class | Immutable component parameters and optional instance key that an activator returns for a new screen session. |
| [`ApplicationScreenActivationContext`](ApplicationScreenActivationContext.cs) | class | Circuit services, owning block and screen definition passed to a screen activator. |
| [`ApplicationScreenBuilder`](ApplicationScreenBuilder.cs) | class | Configures one screen registration's settings, settings editor, default component parameters and activator. |
| [`ApplicationScreenBuilder<TScreen>`](ApplicationScreenBuilder.cs) | class | Screen registration builder whose component type is fixed at compile time. |
| [`ApplicationScreenDefinition`](ApplicationScreenDefinition.cs) | class | A block's screen registration that owns its component parameters, optional activator and settings editor. |
| [`ApplicationScreenHost`](ApplicationScreenHost.cs) | class | Default circuit-scoped screen host that creates `ApplicationScreenSession` proxies for registered screens. |
| [`ApplicationScreenHostBase`](ApplicationScreenHostBase.cs) | abstract class | Base host for block registration, screen activation and closing, navigation guards, startup-screen selection and component attachment. |
| [`ApplicationScreenHostDecorator`](ApplicationScreenHostDecorator.cs) | abstract class | Decorator base over an inner `IApplicationScreenHost`. |
| [`ApplicationScreenHostDecorator<TConcrete>`](ApplicationScreenHostDecorator.cs) | abstract class | Forwards every Blazor and shared host operation to an inner host of type `TConcrete`. |
| [`ApplicationScreenSession`](ApplicationScreenSession.cs) | class | Logical screen retained by the host, with its GUID, registration, immutable parameters and the attached renderer-created component. |
| [`ApplicationStartupScreen`](ApplicationStartupScreen.cs) | record | Saved identity of the startup screen by plugin, block, menu item, screen type and registration key. |
| [`BlazorActionMenuItem`](BlazorActionMenuItem.cs) | class | Menu command that runs an `Action` or an async delegate receiving the circuit's `IServiceProvider` and a cancellation token. |
| [`BlazorMenuSeparator`](BlazorMenuSeparator.cs) | class | Menu separator that command merging removes when it is leading, trailing or repeated. |
| [`BlazorScreenMenuItem`](BlazorScreenMenuItem.cs) | class | Navigation command that opens a screen registration owned by its block. |
| [`BlazorUserInterfaceServices`](BlazorUserInterfaceServices.cs) | class | Circuit-local command execution, error and information reporting, yes or no prompts and exit coordination over the screen host. |
| [`IApplication`](IApplication.cs) | interface | Circuit-local application contract over the screen host, with blocks, loaded plugins, application commands and lifecycle. |
| [`IApplicationBlock`](IApplicationBlock.cs) | interface | Blazor block contract that adds a title, icon URL, tooltip and default-screen metadata to the shared block. |
| [`IApplicationMenu`](IApplicationMenu.cs) | interface | Blazor menu contract with its parent block, ID and icon. |
| [`IApplicationMenuItem`](IApplicationMenuItem.cs) | interface | Blazor menu item metadata and registration-time notification handlers. |
| [`IApplicationScreen`](IApplicationScreen.cs) | interface | Contract for a renderer-created screen component that implements the shared screen lifecycle and command provider. |
| [`IApplicationScreenHost`](IApplicationScreenHost.cs) | interface | Blazor host contract that adds tab identity, component attachment and activation parameters to the shared screen hosts. |
| [`IBlazorUserInterfaceServices`](IBlazorUserInterfaceServices.cs) | interface | Blazor-specific interaction services over the shared `IUserInterfaceServices`. |

### Settings

| Type | Kind | Purpose |
|---|---|---|
| [`BlazorSettingsProvider`](Settings/BlazorSettingsProvider.cs) | class | Scoped asynchronous settings provider that stores typed settings in browser storage for the current circuit's user or anonymous profile. |
| [`IApplicationSettingsEditor`](Settings/IApplicationSettingsEditor.cs) | interface | Contract for a component that edits one settings object, while the containing dialog keeps validation, saving and acceptance. |
