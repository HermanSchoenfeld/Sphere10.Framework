# Application

Native WinForms application shell with plugins, blocks, menus, hosted screens, main forms, Options dialogs and startup integration.

This domain is the Windows Forms implementation of the shared `Sphere10.Framework.Application.UI` contracts. An application groups its features into plugins. Each plugin contributes blocks, and each block owns screen registrations and menus. Describe them with `ApplicationPluginBuilder`, `ApplicationBlockBuilder`, `ApplicationMenuBuilder` and `ApplicationScreenBuilder`, then register the result with `IoCExtensions.AddWinFormsApplicationPlugin` from [Extensions](../Extensions/README.md).

`BuildWinFormsApplication`, `UseMainForm<TMainForm>`, `UseSplashScreen` and `StartWinFormsApplication` in `Sphere10FrameworkExtensions` start the framework with the native message loop. A main form derived from `BlockMainForm` also registers `IApplication`, whose `ScreenHost` owns screen instances, selection, docking and detached windows. Derive feature UIs from `ApplicationScreen` and override `OnShowFirstTime`, `OnShow`, `OnHide` and `OnDestroyScreen` for lifecycle work. The project [README](../README.md) describes exit, lifecycle, screen hosting and screen construction rules in detail.

Screens bind grid and log-output preferences to their `ScreenSettings` through [VisualGrid](../VisualGrid/README.md) and [Logging](../Logging/README.md). General dialogs live in [Dialogs](../Dialogs/README.md) and [Forms](../Forms/README.md).

```csharp
// StockScreen is an application class derived from ApplicationScreen.
Sphere10Framework.Instance
	.BuildWinFormsApplication()
	.AddServices(services => services.AddWinFormsApplicationPlugin(plugin => plugin
		.WithName("Inventory")
		.AddBlock(block => block
			.WithName("Stock")
			.AddScreen<StockScreen>("Stock Levels")
			.AddMenu("Stock", configure: menu => menu.AddItem<StockScreen>("Stock Levels"))
			.WithDefaultScreen(typeof(StockScreen)))))
	.UseMainForm<BlockMainForm>()
	.UseSplashScreen()
	.StartWinFormsApplication();
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Application`](Application.cs) | class | `IApplication` adapter over a running `BlockMainForm` that installs blocks on the UI thread and runs application lifecycle and exit workflows |
| [`ApplicationBlock`](ApplicationBlock.cs) | class | Native block with title, 32x32 and 8x8 images, CHM help file, menus, toolbar items and menu or tool strip placement |
| [`ApplicationBlockBuilder`](ApplicationBlockBuilder.cs) | class | Fluent builder that adds screens, menus, images and help to an `ApplicationBlock` |
| [`ApplicationMenu`](ApplicationMenu.cs) | class | Native menu with title, optional 32x32 image and menu items, parented to its block |
| [`ApplicationMenuBuilder`](ApplicationMenuBuilder.cs) | class | Fluent builder that adds action items and screen items to an `ApplicationMenu` |
| [`ApplicationMenuItemBase`](ApplicationMenuItemBase.cs) | abstract class | Base menu item with 16x16 image, explorer bar and tool strip visibility, and execute-on-load flag |
| [`ApplicationMenuItemBuilder`](ApplicationMenuItemBuilder.cs) | class | Menu item builder whose `AsScreenItem` and `AsActionItem` return nested builders for `WinFormsScreenMenuItem` and `WinFormsActionMenuItem` |
| [`ApplicationPlugin`](ApplicationPlugin.cs) | class | Named group of native blocks and service registrations that aggregates their menus and toolbar items |
| [`ApplicationPluginBuilder`](ApplicationPluginBuilder.cs) | class | Fluent builder that creates an `ApplicationPlugin` from a name, blocks and services |
| [`ApplicationScreenActivationContext`](ApplicationScreenActivationContext.cs) | class | Application, block, host, definition, services and owner form passed to a screen activator |
| [`ApplicationScreenBuilder`](ApplicationScreenBuilder.cs) | class | Configures one native screen registration, including an optional UI-thread activator and settings editor |
| [`ApplicationScreenBuilder<TScreen>`](ApplicationScreenBuilder.cs) | class | Typed screen builder that fixes the screen type and registration identity at compile time |
| [`ApplicationScreenDefinition`](ApplicationScreenDefinition.cs) | class | Native screen definition that adds an `Activator` factory and a `SettingsEditor` factory to the shared definition |
| [`ApplicationScreenHostDecorator`](ApplicationScreenHostDecorator.cs) | abstract class | Decorator base over an inner `IApplicationScreenHost` |
| [`ApplicationScreenHostDecorator<TConcrete>`](ApplicationScreenHostDecorator.cs) | abstract class | Decorator base that forwards every native host member and event to a typed inner host |
| [`ApplicationSettingsEditorContext`](ApplicationSettingsEditorContext.cs) | class | Settings object, application, services and owner passed to a custom Options editor, with `IncludeSettings` for extra members |
| [`ApplicationSettingsScreenPolicy`](ApplicationSettingsScreenPolicy.cs) | enum | Flags that choose reset scope and pending-reset application in the Options dialog |
| [`ApplicationStartupScreenSettings`](ApplicationStartupScreenSettings.cs) | class | Per-user settings record of the plugin, block, screen type and registration key restored at startup |
| [`CHMHelpProvider`](CHMHelpProvider.cs) | class | `IHelpServices` implementation that opens the product CHM or a topic on the requesting form's monitor |
| [`DefaultApplicationIconProvider`](DefaultApplicationIconProvider.cs) | class | Returns the icon set with `UseIcon`, otherwise the registered main form's icon |
| [`FormWindowSettings`](FormWindowSettings.cs) | class | Per-user window bounds, work area, monitor, DPI, state and navigation pane width |
| [`HideScreenEventArgs`](HideScreenEvent.cs) | class | Screen hide event data whose `Cancel` stays true once any handler sets it |
| [`IAboutBox`](IAboutBox.cs) | interface | Marker dialog contract resolved for the Help > About command |
| [`IApplication`](IApplication.cs) | interface | Native application aggregate exposing plugins, blocks, the block manager and the screen host |
| [`IApplicationBlock`](IApplicationBlock.cs) | interface | Native block contract with images, CHM help file, menu placement and typed parent and source plugin |
| [`IApplicationDialog`](IApplicationDialog.cs) | interface | Form-shaped dialog contract with `ShowDialog`, `ShowDialogAsync`, start position and visibility |
| [`IApplicationIconProvider`](IApplicationIconProvider.cs) | interface | Supplies the application's borrowed icon |
| [`IApplicationMenu`](IApplicationMenu.cs) | interface | Native menu contract with text, 32x32 image, menu strip placement and parent block |
| [`IApplicationMenuItem`](IApplicationMenuItem.cs) | interface | Native menu item contract with 16x16 image, explorer bar and tool strip visibility and parent menu |
| [`IApplicationPlugin`](IApplicationPlugin.cs) | interface | Named group of native block definitions, startup services and commands |
| [`IApplicationScreen`](IApplicationScreen.cs) | interface | Native screen contract that exposes its synchronous hooks through the shared screen lifecycle |
| [`IApplicationScreenHost`](IApplicationScreenHost.cs) | interface | Native host contract that owns screens, selection, docking and detached presentation |
| [`IBlockManager`](IBlockManager.cs) | interface | Registers, unregisters and queries blocks and executes their menu items |
| [`IMainForm`](IMainForm.cs) | interface | Main form contract combining UI services, the application icon and application commands |
| [`IMainFrame`](IMainFrame.cs) | interface | Combines UI services, the application icon and block management in one frame contract |
| [`IProductReportBugDialog`](IProductReportBugDialog.cs) | interface | Dialog contract resolved for the Report Bug command |
| [`IProductRequestFeatureDialog`](IProductRequestFeatureDialog.cs) | interface | Dialog contract resolved for the Request Feature command |
| [`IProductSendCommentsDialog`](IProductSendCommentsDialog.cs) | interface | Dialog contract resolved for the Send Comments command |
| [`IWinFormsControlMenuItem`](IWinFormsControlMenuItem.cs) | interface | Menu item that names an `ApplicationControl` to show |
| [`IWinFormsLinkMenuItem`](IWinFormsLinkMenuItem.cs) | interface | Menu item with text and an `OnSelect` callback that can receive the executing UI services |
| [`IWinFormsScreenMenuItem`](IWinFormsScreenMenuItem.cs) | interface | Link menu item that opens a registered screen |
| [`MainFormSettingsFinalizer`](MainFormSettingsFinalizer.cs) | class | Application finalizer that restores main-window placement on load and saves it once at approved shutdown |
| [`MinimizedStartupFolderAutoRunServicesProvider`](MinimizedStartupFolderAutoRunServicesProvider.cs) | class | Startup-folder auto-run provider whose shortcut starts the application minimized |
| [`RegistryAutoRunServicesProvider`](RegistryAutoRunServicesProvider.cs) | class | `IAutoRunServices` implementation that stores auto-run entries in the registry Run keys |
| [`ScreenDisplayMode`](ScreenDisplayMode.cs) | enum | Normal, maximized, filled or filled-and-maximized screen presentation |
| [`SessionEndingHandlerInitializer`](SessionEndingHandlerTask.cs) | class | Initializer that handles Windows session ending so the application closes gracefully |
| [`StartupFolderAutoRunServicesProvider`](StartupFolderAutoRunServicesProvider.cs) | class | `IAutoRunServices` implementation that creates a shortcut in the user's Startup folder |
| [`WinFormsActionMenuItem`](WinFormsActionMenuItem.cs) | class | Menu item that runs an `Action` or `Action<IUserInterfaceServices>` when selected |
| [`WinFormsLinkMenuItemBase`](WinFormsLinkMenuItemBase.cs) | abstract class | Base for text menu items that raise a selection notification |
| [`WinFormsMenuSeparator`](WinFormsMenuSeparator.cs) | class | Separator between native menu or toolbar command groups |
| [`WinFormsScreenMenuItem`](WinFormsScreenMenuItem.cs) | class | Menu item that opens the screen identified by a registration, definition or screen type |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`ApplicationBanner`](Controls/ApplicationBanner.cs) | class | Gradient banner that shows the application icon, title, company name and version |
| [`ApplicationBannerHost`](Controls/ApplicationBannerHost.cs) | class | Hosts the registered `ApplicationBanner` at runtime and a stock banner in the designer |
| [`ApplicationBar`](Controls/ApplicationBar.cs) | class | Navigation bar of stacked buttons that shows the selected item's control above them, where a nested `Item` pairs a button with its control |
| [`ApplicationControl`](Controls/ApplicationControl.cs) | class | User control base with settings and UI services that refreshes when its settings change |
| [`ApplicationScreenHost`](Controls/ApplicationScreenHost.cs) | class | Native screen host that creates screens, presents them in tabs or detached windows and guards navigation |
| [`ApplicationScreenHostBase`](Controls/ApplicationScreenHostBase.cs) | abstract class | User control base that implements `IApplicationScreenHost` over an abstract host engine |
| [`ApplicationScreenTabControl`](Controls/ApplicationScreenTabControl.cs) | class | Screen tabs with close buttons, context menu, reordering, dock preview and drag-out requests |
| [`ApplicationSettingsEditor`](Controls/ApplicationSettingsEditor.cs) | class | Settings property editor that hides transient members and runtime ownership |
| [`BasicContactDetailsControl`](Controls/BasicContactDetailsControl.cs) | class | Collects a contact email or an anonymous choice for support submissions |
| [`ButtonPressedHandler`](Controls/ApplicationBar.cs) | delegate | Handler for `ApplicationBar.ButtonPressed` that receives the bar and the pressed item |
| [`WhoAreYouControl`](Controls/WhoAreYouControl.cs) | class | Radio-button picker of the submitter's `UserType` for support forms |

### Extensions

| Type | Kind | Purpose |
|---|---|---|
| [`ApplicationSettingsExtensions`](Extensions/ApplicationSettingsExtensions.cs) | static class | `AddApplicationSettings` registers the stable key that namespaces screen settings |
| [`Sphere10FrameworkExtensions`](Extensions/Sphere10FrameworkExtensions.cs) | static class | `BuildWinFormsApplication`, `UseMainForm`, `UseSplashScreen`, `UseIcon`, `UseWindowSize`, `UseMainFormSettings` and `StartWinFormsApplication` |

### Forms

| Type | Kind | Purpose |
|---|---|---|
| [`ApplicationForm`](Forms/ApplicationForm.cs) | class | `FormEx` base that formats its caption tokens with `StringFormatter` |
| [`ApplicationScreen`](Forms/ApplicationScreen.cs) | class | Base for hosted screens with lifecycle hooks and events, menus, toolbar, image, settings and preference binding |
| [`ApplicationScreenForm`](Forms/ApplicationScreenForm.cs) | class | Detached window that presents a screen still owned by its originating host |
| [`ApplicationSettingsDialog`](Forms/ApplicationSettingsDialog.cs) | class | Options dialog that edits the plugin, block and screen settings of the running application |
| [`BlockConfigurationScreen`](Forms/BlockConfigurationScreen.cs) | class | Empty designer user control for block configuration layouts |
| [`BlockMainForm`](Forms/BlockMainForm.cs) | class | Main form with a collapsible navigation pane of block task panes that implements `IBlockManager` |
| [`LiteMainForm`](Forms/LiteMainForm.cs) | class | Main form base with first-run events, exit confirmation, closing permissions and Screen Options |
| [`MainForm`](Forms/MainForm.cs) | class | Main form that composes menus and toolbar commands around its `ApplicationScreenHost` |
| [`ProductAboutBox`](Forms/ProductAboutBox.cs) | class | Default `IAboutBox` whose labels and link are filled from product information tokens |
| [`ProductProductReportBugDialog`](Forms/ProductProductReportBugDialog.cs) | class | Default `IProductReportBugDialog` that submits a bug report through the product support service |
| [`ProductProductRequestFeatureDialog`](Forms/ProductProductRequestFeatureDialog.cs) | class | Default `IProductRequestFeatureDialog` that submits a feature request through the product support service |
| [`ProductProductSendCommentsDialog`](Forms/ProductProductSendCommentsDialog.cs) | class | Default `IProductSendCommentsDialog` that submits comments through the product support service |
| [`ServerPostBackForm`](Forms/ServerPostBackForm.cs) | class | Wait form that says the application is contacting software support |
| [`SplashScreen`](Forms/SplashScreen.cs) | class | Startup and shutdown splash with artwork, progress title and message, and fade in and out |
