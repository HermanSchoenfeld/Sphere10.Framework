# Shared application UI model

UI-independent contracts, bases, fluent builders, decorators and the screen-host engine for applications made of plugins, blocks, menus, screens and wizards, which the WinForms and Blazor adapters implement.

A running `IApplication` owns its blocks through an `IApplicationScreenHost`, and each `IApplicationBlock` owns `ApplicationScreenDefinition` objects and the menus that activate them. A plugin is a named startup contribution of blocks, services and lifecycle tasks. The builders in this folder hold the shared configuration and validation, and the platform builders in `Sphere10.Framework.Windows.Forms` and `Sphere10.Framework.Web.AspNetCore.Blazor` supply the native products, rendering and dispatch. `ApplicationScreenHost<TBlock, TScreen>` is the shared engine for block navigation, screen retention, ordering and leave guards. The wizard workflow in the `Wizard` subfolder works with any step type, such as a component `Type` or a native screen.

Most types are in the `Sphere10.Framework.Application.UI` namespace. The help, website-launcher and user-interface services stay in `Sphere10.Framework.Application`. Lifecycle tasks are in [Initialization and finalization tasks](../Lifecycle/README.md), and the settings that screens, blocks and plugins own are in [Settings](../Settings/README.md). The [Application guide](../README.md#shared-ui-framework) covers ownership, lifecycles, navigation, builders and wizards in detail.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ApplicationAction`](ApplicationAction.cs) | class | Menu callback that runs a plain, UI-aware or asynchronous service-provider action through `Execute` or `ExecuteAsync` |
| [`ApplicationBase`](ApplicationBase.cs) | abstract class | Shared application lifecycle, task inventories, owned-resource cleanup, notifications and live projections of the host's blocks |
| [`ApplicationBlockBase`](ApplicationBlockBase.cs) | abstract class | Block metadata, owned screen definitions and the default screen definition |
| [`ApplicationBlockBuilderBase<TMenu, TBlock>`](ApplicationBlockBuilderBase.cs) | abstract class | Accumulates screen registrations and menu commands for platform block builders |
| [`ApplicationBlockBuilderBase<TSelf, TMenu, TMenuBuilder, TBlock>`](ApplicationBlockBuilderBase.cs) | abstract class | Fluent block metadata, default-screen policy and child menu-builder composition |
| [`ApplicationBuilderBase<TSelf, TApplication, TPlugin, TPluginBuilder, TResult>`](ApplicationBuilderBase.cs) | abstract class | Collects the application title, plugins and deferred callbacks before a platform registers or creates its host |
| [`ApplicationCommandLayer`](ApplicationCommandLayer.cs) | class | A command provider's menu and toolbar contribution at an explicit `ApplicationCommandLevel` |
| [`ApplicationCommandLevel`](ApplicationCommandLevel.cs) | enum | Scope of a command contribution: `Application`, `Block`, `Screen` or `Plugin` |
| [`ApplicationDecorator`](ApplicationDecorator.cs) | abstract class | `ApplicationDecorator<TConcrete>` over a plain `IApplication` |
| [`ApplicationDecorator<TConcrete>`](ApplicationDecorator.cs) | abstract class | Forwards every `IApplication` member, event and command array to a wrapped application |
| [`ApplicationMenuBase<TItem>`](ApplicationMenuBase.cs) | abstract class | Menu metadata and item storage independent of controls and images |
| [`ApplicationMenuBuilderBase<TItem, TMenu>`](ApplicationMenuBuilderBase.cs) | abstract class | Menu builder state: text, icon and items, with an abstract `CreateMenu` factory |
| [`ApplicationMenuBuilderBase<TSelf, TItem, TItemBuilder, TMenu>`](ApplicationMenuBuilderBase.cs) | abstract class | Fluent menu metadata and child item-builder composition |
| [`ApplicationMenuItemBase`](ApplicationMenuItemBase.cs) | abstract class | Menu item metadata and registration-time notification handlers |
| [`ApplicationMenuItemBuilderBase`](ApplicationMenuItemBuilderBase.cs) | abstract class | Configures command presentation and a screen reference or runtime action without registering screens |
| [`ApplicationMenuItemBuilderBase<TSelf>`](ApplicationMenuItemBuilderBase.cs) | abstract class | Fluent command presentation, registered-screen references and runtime action callbacks |
| [`ApplicationPlugin`](ApplicationPlugin.cs) | class | Plugin definition with a validated name and an owned copy of its block membership |
| [`ApplicationPluginBase`](ApplicationPluginBase.cs) | abstract class | Plugin `Load` and `Unload` with service registration and `Loaded` and `Unloaded` notifications |
| [`ApplicationPluginBase<TBlock>`](ApplicationPluginBase.cs) | abstract class | Typed plugin storage and lifecycle that stays assignable to `ApplicationPlugin` |
| [`ApplicationPluginBuilder`](ApplicationPluginBuilder.cs) | class | Builds UI-neutral `ApplicationPlugin` definitions, including service-only plugins with no blocks |
| [`ApplicationPluginBuilderBase<TBlock, TPlugin>`](ApplicationPluginBuilderBase.cs) | abstract class | Plugin builder state: name, settings, blocks and service callbacks, with a platform plugin factory |
| [`ApplicationPluginBuilderBase<TSelf, TBlock, TPlugin>`](ApplicationPluginBuilderBase.cs) | abstract class | Fluent plugin configuration, `AddInitializer<T>` and `AddFinalizer<T>` over existing block definitions |
| [`ApplicationPluginBuilderBase<TSelf, TBlock, TBlockBuilder, TPlugin>`](ApplicationPluginBuilderBase.cs) | abstract class | Adds child block-builder composition to the fluent plugin builder |
| [`ApplicationPluginDecorator`](ApplicationPluginDecorator.cs) | abstract class | `ApplicationPluginDecorator<TConcrete>` over a plain `IApplicationPlugin` |
| [`ApplicationPluginDecorator<TConcrete>`](ApplicationPluginDecorator.cs) | abstract class | Forwards plugin definitions and startup lifetime to a wrapped plugin without taking ownership |
| [`ApplicationPluginRegistration<TPlugin>`](ApplicationPluginRegistration.cs) | record | Receipt of a successfully registered startup plugin and its captured block membership |
| [`ApplicationScreenBuilder`](ApplicationScreenBuilder.cs) | class | Declares UI-independent screen metadata for an explicitly supplied screen type |
| [`ApplicationScreenBuilder<TScreen>`](ApplicationScreenBuilder.cs) | class | Declares a typed UI-independent screen registration |
| [`ApplicationScreenBuilderBase<TSelf>`](ApplicationScreenBuilderBase.cs) | abstract class | Fluent screen title, activation mode, kind, stable key and settings, leaving activation to the platform |
| [`ApplicationScreenDefinition`](ApplicationScreenDefinition.cs) | class | Screen registration metadata, its settings and the commands that activate it, with whole-collection validation |
| [`ApplicationScreenHost`](ApplicationScreenHost.cs) | class | Screen host for neutral `IApplicationBlock` and `IApplicationScreen` types |
| [`ApplicationScreenHost<TBlock, TScreen>`](ApplicationScreenHost.cs) | class | Shared block registration, navigation, screen retention, ordering and guarded lifecycle, with overridable creation and removal hooks |
| [`ApplicationScreenHostBase<TBlock, TScreen>`](ApplicationScreenHostBase.cs) | abstract class | Overridable host contract that implements the single-screen and multiple-screen interfaces through virtual members |
| [`ApplicationScreenHostDecorator`](ApplicationScreenHostDecorator.cs) | abstract class | `ApplicationScreenHostDecorator<TConcrete>` over a plain `IApplicationScreenHost` |
| [`ApplicationScreenHostDecorator<TBlock, TScreen>`](ApplicationScreenHostDecorator.cs) | abstract class | Typed host decorator over an `IApplicationScreenHost<TBlock, TScreen>` |
| [`ApplicationScreenHostDecorator<TBlock, TScreen, TConcrete>`](ApplicationScreenHostDecorator.cs) | abstract class | Forwards typed host operations while keeping neutral interface dispatch |
| [`ApplicationScreenHostDecorator<TConcrete>`](ApplicationScreenHostDecorator.cs) | abstract class | Forwards neutral host operations to a wrapped host |
| [`ApplicationScreenRegistration`](ApplicationScreenRegistration.cs) | class | Identity token of one screen registration, resolved by each block graph without retaining a screen |
| [`ApplicationScreenRegistration<TScreen>`](ApplicationScreenRegistration.cs) | class | Typed screen registration identity token |
| [`ApplicationUserInterfaceServices`](ApplicationUserInterfaceServices.cs) | class | Disposable `IUserInterfaceServices` that executes menu items through a borrowed host, while its dialogs and reports throw until a platform overrides them |
| [`AsyncApplicationBase`](AsyncApplicationBase.cs) | abstract class | Application whose lifecycle and exit work is awaited, with blocking adapters for synchronous callers |
| [`ConsoleApplicationUserInterfaceServices`](ConsoleApplicationUserInterfaceServices.cs) | class | `IUserInterfaceServices` that writes reports to the console and exits the process, and whose dialogs and `AskYN` throw `NotSupportedException` |
| [`IApplication`](IApplication.cs) | interface | Running application: logger, owned resources, loaded plugins, blocks, screen host, settings, lifecycle and exit |
| [`IApplication<TPlugin, THost>`](IApplication.cs) | interface | `IApplication` with typed loaded plugins and screen host |
| [`IApplicationBlock`](IApplicationBlock.cs) | interface | Block metadata: parent application, source plugin, name, position, settings, screen definitions, default screen and exit guard |
| [`IApplicationCommandProvider`](IApplicationCommandProvider.cs) | interface | Portable `Menus` and `ToolBarItems` descriptors with empty defaults |
| [`IApplicationCommandProvider<TMenu, TItem>`](IApplicationCommandProvider.cs) | interface | Typed menu and toolbar descriptors bridged to the neutral provider |
| [`IApplicationMenu`](IApplicationMenu.cs) | interface | Menu with a parent block, text and items |
| [`IApplicationMenu<TItem>`](IApplicationMenu.cs) | interface | Menu with typed items bridged to the neutral menu |
| [`IApplicationMenuItem`](IApplicationMenuItem.cs) | interface | Menu or toolbar command with a parent menu and title |
| [`IApplicationMenuSeparator`](IApplicationMenuSeparator.cs) | interface | Non-executable boundary between command groups |
| [`IApplicationPlugin`](IApplicationPlugin.cs) | interface | Named application extension with settings, blocks, `Load`, `Unload` and their notifications |
| [`IApplicationPlugin<TBlock>`](IApplicationPlugin.cs) | interface | Plugin with typed blocks bridged to the neutral plugin |
| [`IApplicationScreen`](IApplicationScreen.cs) | interface | Screen with host and block slots, title, settings, dirty state, help metadata, activation hooks and a leave guard |
| [`IApplicationScreenHost`](IApplicationScreenHost.cs) | interface | Hosts application blocks and screens: registration, block selection and activation, screen opening, view mode and unsaved-state checks |
| [`IApplicationScreenHost<TBlock, TScreen>`](IApplicationScreenHost.cs) | interface | Typed block and screen projections shared by all application hosts |
| [`IApplicationUIBuilder<TProduct>`](IApplicationUIBuilder.cs) | interface | Builder whose `Build` produces a UI definition, so parents can compose child builders through product interfaces |
| [`IApplicationUpgradeDialog`](IUserInterfaceServices.cs) | interface | Upgrade progress dialog with a `ProgressHandler` and a `Finish` call that takes an error message |
| [`IHelpableObject`](IHelpableObject.cs) | interface | Help metadata: help type, file name, URL, page number and topic identifiers |
| [`IHelpServices`](IHelpServices.cs) | interface | Shows general help or the help of an `IHelpableObject` |
| [`IMultiApplicationScreenHost`](IMultiApplicationScreenHost.cs) | interface | Operations on retained and open screens in a host with several screens: show, close, move and batch close |
| [`IMultiApplicationScreenHost<TBlock, TScreen>`](IMultiApplicationScreenHost.cs) | interface | Typed multiple-screen host operations |
| [`IScreenMenuItem`](IScreenMenuItem.cs) | interface | Command that references a screen definition registered by its owning block |
| [`ISingleApplicationScreenHost`](ISingleApplicationScreenHost.cs) | interface | Operations on the current screen only: close and check closure |
| [`ISingleApplicationScreenHost<TBlock, TScreen>`](ISingleApplicationScreenHost.cs) | interface | Typed single-screen host operations |
| [`IUserInterfaceServices`](IUserInterfaceServices.cs) | interface | Application dialogs, status, error and information reports, questions, exit, UI-thread dispatch and menu-command execution |
| [`IWebsiteLauncher`](IWebsiteLauncher.cs) | interface | Opens a URL or the company, product or purchase website |
| [`MultiApplicationScreenHostDecorator`](MultiApplicationScreenHostDecorator.cs) | abstract class | `MultiApplicationScreenHostDecorator<TConcrete>` over a plain `IMultiApplicationScreenHost` |
| [`MultiApplicationScreenHostDecorator<TBlock, TScreen>`](MultiApplicationScreenHostDecorator.cs) | abstract class | Typed multiple-screen host decorator over an `IMultiApplicationScreenHost<TBlock, TScreen>` |
| [`MultiApplicationScreenHostDecorator<TBlock, TScreen, TConcrete>`](MultiApplicationScreenHostDecorator.cs) | abstract class | Forwards typed multiple-screen host operations to a wrapped host |
| [`MultiApplicationScreenHostDecorator<TConcrete>`](MultiApplicationScreenHostDecorator.cs) | abstract class | Forwards neutral multiple-screen host operations to a wrapped host |
| [`ScreenActivationMode`](ScreenActivationMode.cs) | enum | Screen lifetime: `SingleInstance`, `MultiInstance` or `PermanentSingleton` |
| [`ScreenKind`](ScreenKind.cs) | enum | `Normal` screen or `Empty` placeholder shown only while no other screen is open |
| [`ScreenMode`](ScreenMode.cs) | enum | Host view mode: `SingleView` or `MultiView` |
| [`SingleApplicationScreenHostDecorator`](SingleApplicationScreenHostDecorator.cs) | abstract class | `SingleApplicationScreenHostDecorator<TConcrete>` over a plain `ISingleApplicationScreenHost` |
| [`SingleApplicationScreenHostDecorator<TBlock, TScreen>`](SingleApplicationScreenHostDecorator.cs) | abstract class | Typed single-screen host decorator over an `ISingleApplicationScreenHost<TBlock, TScreen>` |
| [`SingleApplicationScreenHostDecorator<TBlock, TScreen, TConcrete>`](SingleApplicationScreenHostDecorator.cs) | abstract class | Forwards typed single-screen host operations to a wrapped host |
| [`SingleApplicationScreenHostDecorator<TConcrete>`](SingleApplicationScreenHostDecorator.cs) | abstract class | Forwards neutral single-screen host operations to a wrapped host |
| [`StandardHelpServices`](StandardHelpProvider.cs) | class | `IHelpServices` that opens the product website for general and context help |
| [`StandardWebsiteLauncher`](StandardWebsiteLauncher.cs) | class | `IWebsiteLauncher` that opens URLs through the shell and reads the product URLs from `IProductInformationProvider` |
| [`SyncApplicationBase`](SyncApplicationBase.cs) | abstract class | Application whose lifecycle and exit work runs synchronously, with worker-backed asynchronous adapters |
| [`UserInterfaceServicesDecorator`](UserInterfaceServicesDecorator.cs) | abstract class | `UserInterfaceServicesDecorator<TConcrete>` over a plain `IUserInterfaceServices` |
| [`UserInterfaceServicesDecorator<TConcrete>`](UserInterfaceServicesDecorator.cs) | abstract class | Forwards application interactions and command execution to a borrowed service |

### Settings

| Type | Kind | Purpose |
|---|---|---|
| [`ApplicationGridColumnPreferences`](Settings/ApplicationGridColumnPreferences.cs) | class | Sealed grid column preference with a schema `Key` and `Title`, an optional manual `Width` and an optional `ExpandsToFit` |

### Wizard

| Type | Kind | Purpose |
|---|---|---|
| [`IWizard<TModel, TStep>`](Wizard/IWizard.cs) | interface | Wizard over a `Model` |
| [`IWizard<TStep>`](Wizard/IWizard.cs) | interface | Ordered wizard steps, current position, navigation, step updates, finish and cancel |
| [`IWizardBuilder<TSelf, TModel, TStep, TWizard>`](Wizard/IWizardBuilder.cs) | interface | Fluent wizard configuration contract with platform step and product types |
| [`IWizardStep`](Wizard/IWizardStep.cs) | interface | Step that declares its `WizardStepTraits` |
| [`Wizard<TModel, TStep>`](Wizard/Wizard.cs) | class | Owns step membership, navigation and one successful finish or cancellation |
| [`WizardBase<TModel, TStep>`](Wizard/WizardBase.cs) | abstract class | Abstract `IWizard<TModel, TStep>` members with a protected `OnChanged` notification |
| [`WizardBuilderBase<TModel, TStep, TWizard>`](Wizard/WizardBuilderBase.cs) | abstract class | Wizard builder state, whose platform builders supply steps and the product factory |
| [`WizardBuilderBase<TSelf, TModel, TStep, TWizard>`](Wizard/WizardBuilderBase.cs) | abstract class | Fluent wizard setup, reset, ordered steps, cancellation, callbacks and build validation |
| [`WizardDecorator<TModel, TStep>`](Wizard/WizardDecorator.cs) | abstract class | `WizardDecorator<TModel, TStep, TConcrete>` over a plain `IWizard<TModel, TStep>` |
| [`WizardDecorator<TModel, TStep, TConcrete>`](Wizard/WizardDecorator.cs) | abstract class | Forwards every wizard member, including the step-traits resolver, to a wrapped wizard |
| [`WizardResult`](Wizard/WizardResult.cs) | enum | Wizard outcome: `Success`, `Error` or `Cancelled` |
| [`WizardState`](Wizard/WizardState.cs) | enum | Wizard lifecycle state: `Active`, `Finishing`, `Cancelling`, `Finished` or `Cancelled` |
| [`WizardStepTraits`](Wizard/WizardStepTraits.cs) | enum | Step flags: `None` or `AssumeHasNext` for a step that supplies its successor during navigation |
| [`WizardStepUpdateType`](Wizard/WizardStepUpdateType.cs) | enum | Step update operation: `Inject`, `ReplaceAllNext`, `ReplaceAll` or `RemoveNext` |

`ApplicationHelper`, `ApplicationPluginRegistrationScope` and `UserInterfaceServicesDispatch` are internal.
