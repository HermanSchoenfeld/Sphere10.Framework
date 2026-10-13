<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 🖼️ Sphere10.Framework.Windows.Forms

**Windows Forms UI framework and component library** providing data binding controls, database connection panels, validation components, and presentation utilities for desktop applications.

Sphere10.Framework.Windows.Forms enables **rapid Windows desktop application development** with pre-built UI components for database connectivity, data display, user input validation, and common desktop patterns.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Windows.Forms
```

## Async modal dialogs (.NET 10)

Await modal operations from UI event handlers or methods returning `Task`:

```csharp
private async void EditButton_Click(object sender, EventArgs args) {
	var (accepted, userInput) = await EnterTextDialog.ShowAsync(this, "Edit name", "Name", "Current name");
	if (accepted)
		await DialogEx.ShowAsync(this, SystemIconType.Information, "Saved", userInput, "OK");
}
```

Use `ShowAsync` on `DialogEx`, `ExceptionDialog`, `EnterTextDialog`, `PasswordDialog`, `LogonDialog`, and `CrudDialog`; use `GenericEditorForm.ShowFormAsync` and `QuestionDialogSession.AskQuestionAsync`. Password input returns `(DialogResult Result, string Password)`; text input returns `(bool Accepted, string UserInput)`. These helpers dispose the dialogs they create after completion. When calling native `Form.ShowDialogAsync` on your own form, keep it in a `using` scope across the `await`.

Custom forms use .NET WinForms directly: `using var dialog = new MyDialog(); var result = await dialog.ShowDialogAsync(this);`. `InvokeAsyncEx` remains a UI dispatch convenience over native `Control.InvokeAsync`, propagating completion, exceptions, and cancellation; it does not implement a modal loop.

`IApplicationDialog` declares the native `ShowDialogAsync` overloads, implemented directly by the inherited `Form` methods. There is no legacy synchronous fallback. The caller retains ownership of an existing dialog. CRUD create, edit, and delete methods return `Task`; SourceGrid deletion uses `DeleteSelectedRowsAsync`. Custom `DialogEx` button behavior overrides `OnProcessButtonAsync`.

`await wizard.Start(owner)` on `IWinFormsWizard<TModel>` also awaits native `Form.ShowDialogAsync`, keeping its owner disabled until completion or cancellation and disposing the wizard dialog afterward.

Wizard screens receive their `Wizard` and `Model` before `Initialize`, including screens added with `InjectScreen`. Navigation calls `Validate` before `OnNext`; commit accepted input in `OnNext` when invalid input must not change the model. Set `UpdateModelOnStateChanged = false` for this pattern. The default `OnPresent` calls `CopyModelToUI`, so input and summary screens can refresh their controls each time they are shown.

WinForms closing and validation events still require cancellation and handled flags to be set before the first `await`. Synchronous grid queries and model construction can only schedule asynchronous error reporting; they cannot await a modal result. `IWindowsFormsEditorService.ShowDialog`, common file/folder/color dialogs, and the synchronous `LiteMainForm.AskYN` contract retain synchronous modal calls. `AskYN` uses a native message box; blocking `ShowDialogAsync` with `.Result` or `.GetAwaiter().GetResult()` on the UI thread would deadlock.

Run the STA regression tests with `dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj` on Windows. They exercise actual forms with a message loop and close their dialogs automatically.

Call `await application.ExitAsync()` to request shutdown and receive whether the main window actually closed. File > Exit and the close button delegate to that same application workflow. It owns one asynchronous native confirmation, the retained screens' close guards, the active screen's block and the selected block's `CanExitAsync(application)` permissions, and existing application-exit observers. Each distinct block is checked once. Repeated requests share the pending decision. A native `FormClosing` observer can still decline normal exit, and another request can retry. Window close continues to hide or minimize forms configured for those actions; explicit application exit closes the window. `application.Exit()` provides the synchronous counterpart, using the same pending decision and native message pumping on the UI thread. `IsExiting` identifies a pending request; `HasExited` identifies completed closure.

`await application.ExitAsync(force: true)` and `application.Exit(force: true)` skip confirmation and permission vetoes, including native `FormClosing` vetoes, while completing normal WinForms closure. Force also upgrades a pending normal request and dismisses its default confirmation dialog. Cancellation during confirmation dismisses that dialog and leaves the application available for another request. Once shutdown is committed, finalizers and resource cleanup finish even if a caller stops waiting. Exit dispatches onto the UI thread even when requested from a worker. Session-ending messages use forced graceful closure. A standalone main form retains its existing closing behavior and confirmation override. Once exit is approved, application and framework shutdown tasks complete before the main window closes; service-provider disposal follows after `Application.Run` returns.

## Application lifecycle and splash

Declare `WithName(...)` before calling `RegisterInitializer` or `RegisterFinalizer`. Both type and factory overloads throw `InvalidOperationException` immediately when the plugin has no name.

`IApplication` exposes `Initialize()` / `InitializeAsync(...)`, `Finalize()` / `FinalizeAsync(...)`, `Exit(...)` / `ExitAsync(...)`, `Logger`, `Disposables`, lifecycle events and `Progress`. Initializing an application runs its tasks once; concurrent calls await the same operation. Merely building a plugin, resolving the application or reading its properties does not run its tasks. Finalization waits for attempted initialization, runs shutdown tasks once and releases application-owned resources; disposing the application also completes that cleanup. Finalization alone does not close the main window: request closure through `ExitAsync`. Task collections remain protected implementation details of `ApplicationBase`.

`WinFormsApplicationPluginBuilder` inherits `RegisterInitializer<T>()` and `RegisterFinalizer<T>()` from the shared plugin builder. Their factory overloads accept `Func<IServiceProvider, T>`. These helpers register transient lifecycle tasks keyed by the built plugin's `Name`; duplicate names are rejected at load, and custom plugin names must remain stable. Different plugins can register the same task type without sharing its instance. The application resolves and caches tasks for its successful `LoadedPlugins` through its own provider. Host registrations using `services.AddInitializer<T>(typeof(IApplication))` and `services.AddFinalizer<T>(typeof(IApplication))` remain supported for application-owned tasks. The combined inventory starts with host-owned registrations in registration order, followed by plugins in `LoadedPlugins` order and each plugin's tasks in declaration order. Initializer priorities apply across this inventory within the existing sequential and parallelizable phases; finalizers execute in combined inventory order. Existing unkeyed framework tasks retain their process startup and shutdown behavior. Use `ConfigureServices` for other plugin dependencies.

Derive synchronous tasks from `SyncApplicationInitializerBase` or `SyncApplicationFinalizerBase` and override only their synchronous method; the base supplies the worker-task async bridge. Derive asynchronous tasks from `AsyncApplicationInitializerBase` or `AsyncApplicationFinalizerBase` and override only their asynchronous method; the base supplies the synchronous bridge. The common `ApplicationInitializerBase` and `ApplicationFinalizerBase` leave both methods abstract. Tasks requiring native UI affinity can implement both contracts there and dispatch to their owning UI thread.

`WinFormsApplication` derives from `AsyncApplicationBase`: its asynchronous readiness hook installs blocks on the owning UI thread before awaiting application tasks. Worker lifecycle requests start through `InvokeAsyncEx`; synchronous native lifecycle and disposal calls pump UI messages while waiting for the same asynchronous operation. This preserves UI continuations without sending block registration to a worker. Pending lifecycle work still follows the shared coalescing, cancellation and disposal guards.

`StartWinFormsApplication` starts the framework first, preserving `Framework.Initialized` configuration. With the native message loop running, it installs blocks and awaits `application.InitializeAsync()` before showing the main window. A vetoed block installation can retry without running application tasks. Approved exit awaits application finalization and then runs framework finalizers while the main window remains open with interaction disabled. The window closes after these tasks; the runner waits for the root's exit result before ending the message loop and disposing the service provider. Cleanup still proceeds when a finalizer fails. `Disposables` holds resources owned by the application, independently of the borrowed form and screen host.

Enable the startup and shutdown splash with `.UseSplashScreen()` or `.UseSplashScreen(image)` on the `BuildWinFormsApplication()` chain. It retains its framework events and global information logger, and subscribes to application lifecycle and progress before initialization. After exit confirmation and all closing permissions succeed, a fresh splash displays application and framework shutdown tasks while the main window remains behind it. Closing the main window is the last step after those tasks. The splash then fades out during resource cleanup. A failed initialization reuses its still-visible splash for cleanup. Updates from worker tasks queue onto the UI thread. Existing fade-in and fade-out behavior remains in place. Application tasks touching native controls must run on the UI thread; reserve parallelizable tasks for work that does not access controls directly.

The root's `Logger` is the framework `Sphere10.Framework.ILogger`, resolved from its provider with `SystemLog.Logger` as the fallback. Lifecycle tasks can inject `IApplication`; hosted screens can reach the same logger through `Block.Parent.Logger`. Add only application-owned resources to `application.Disposables`; services, the form and the host keep their existing disposal owners. The adapter does not call plugin `Unload()` during shutdown.

## Application screen hosting

`IApplicationScreenHost` in `Sphere10.Framework.Application.UI` owns available and registered blocks, active navigation, the active screen, UI services, view mode, unsaved changes and lifecycle guards. `ISingleApplicationScreenHost` adds operations on the current screen; `IMultiApplicationScreenHost` adds retained/open collections, ordering and instance operations. The native host implements both interfaces and can switch modes through the guarded `ScreenMode` setter or `TrySetScreenModeAsync`. `IUserInterfaceServices` owns dialogs, status, UI dispatch and command execution. Read them from `application.ScreenHost.UserInterfaceServices`. The application exposes live `Blocks`, successful startup `LoadedPlugins`, its host and application commands; `ScreenHost.ActiveBlock` owns the selected block.

`AddMainForm<TForm>()` registers shared, typed and native hosting interfaces against the existing host of a `MainForm`; it does not create another workspace. UI services resolve the form. Inject the common host for navigation and lifecycle operations:

```csharp
using System.Threading.Tasks;
using Sphere10.Framework.Application.UI;

public class WorkspaceNavigation {
	private readonly IApplicationScreenHost _host;

	public WorkspaceNavigation(IApplicationScreenHost host) => _host = host;

	public Task<bool> ExecuteAsync(IApplicationMenuItem item) =>
		_host.UserInterfaceServices.ExecuteMenuItemAsync(item);

	public Task<IApplicationScreen> OpenEditorAsync(IApplicationBlock block, ApplicationScreenDefinition definition) =>
		_host.OpenScreenAsync(block, definition);

	public Task<bool> CloseActiveEditorAsync() => _host.ActiveScreen == null
		? Task.FromResult(true)
		: _host.CloseScreenAsync(_host.ActiveScreen);
}
```

The native host exposes actual `WinFormsApplicationScreen` instances through every hosting interface. `Screens` includes retained singleton controls even when hidden; `OpenScreens` follows open order, including detached windows. Control identity remains stable across hide, show, docking and title changes. `SelectBlockAsync` browses a block without replacing the active editor; `ActivateBlockAsync` opens its default screen. Both navigation and hosting changes raise `Changed`. Showing a retained screen again selects its owner for application navigation.

The generic `IApplicationScreenHost<TBlock, TScreen>` provides typed blocks, screens and lifecycle operations once for every platform. `IWinFormsApplicationScreenHost` adds native docking and existing synchronous presentation APIs. A reference to a screen identifies the same object through neutral, generic and native interfaces. Blocks also use reference identity; distinct native block definitions can have matching names.

The native adapter composes the common logical host and performs native presentation on the UI thread. Its binding dictionary contains only tabs and detached windows. A screen exposes its public native `Host` and owning `Block` through both native and shared contracts; `ApplicationBlock` remains an alias of `Block`. These references are available during activation and destruction hooks, remain attached while a singleton is hidden or a window is undocked, and clear when the instance leaves the host. A `WinFormsApplicationScreen` can also access the owning shared host through protected `ApplicationScreenHost` and close itself with `await ApplicationScreenHost.CloseScreenAsync(this)`.

`OpenScreenAsync(block, definition)` accepts a registered block and an exact definition reference from its `ScreenDefinitions`. Blocks own immutable definitions independently of menus, so a screen can open without a navigation item. A menu item references `ScreenDefinition`, and the block's `DefaultScreenDefinition` references the desired startup definition. Use `RegisterBlockAsync` and `UnregisterBlockAsync` for application membership; guarded removal closes its native controls and updates the same host registry.

Menu items expose their declaring menu through `Parent`, and menus expose their block through `Parent`. Adding and removing items and menus through their containers maintains these references. A screen definition's `Activators` array is a snapshot of the commands attached to menus that reference it; replacing a command's definition or removing the command updates that reverse membership. Application-level menus have no block parent. Rendering merged menus preserves each command's original parent. Screen commands, including toolbar commands, require their declaring menu's block through `item.Parent.Parent`. Ordinary application actions can run without a block or menu parent.

A registered block's `Parent` references the running application. That link follows live host membership and clears when the block is removed or the application adapter is disposed. `Source` records the plugin that successfully contributed the captured block; standalone blocks have no source plugin. Removing and registering a contributed block again preserves its source, independently of its current application membership. Native and shared interfaces expose the same references.

Execute the actual menu item supplied by the registered block, and pass the actual block, definition or screen to host operations. Ownership comes from these references; command dispatch does not search blocks for matching captions or IDs. The typed WinForms contracts specialize `Parent`, `Source`, `Host` and `Block`, with inline bridges to the shared interfaces under `#region Base Interface Bridges`.

## ⚡ 10-Second Example

```csharp
using Sphere10.Framework.Windows.Forms;

// Create a database connection panel that adapts to selected DBMS
var connectionPanel = new DatabaseConnectionPanel();
connectionPanel.SelectedDBMSType = DBMSType.SQLServer;

// Get connection string and DAC from the panel
string connectionString = connectionPanel.ConnectionString;
IDAC dac = connectionPanel.GetDAC();
```

## 🏗️ Core Concepts

**Database Connection Controls**: `DatabaseConnectionPanel` dynamically loads appropriate UI based on selected database type (SQLite, SQL Server, Firebird).

**Custom Controls**: Enhanced controls like `PathSelectorControl`, `ProgressBarEx`, `PropertyGridEx`, `RadioGroupBox`, and validation indicators.

**Data Binding**: Enhanced data binding with validation and change notification.

**Wizard Framework**: Multi-step wizard UI pattern implementation.

**Source Grid**: Advanced data grid component for tabular data display.

## 🔧 Core Components

### CrudGrid

`CrudGrid` hides the create and delete buttons when their respective capabilities are unavailable. Delete is also hidden unless a visible row is selected. Toolbar and paging controls size to their contents, including scaled fonts and DPI.

Set `AllowCellEditing = true` to edit columns with `CanEditCell = true` and a `SetPropertyValue` binding; `CanUpdate` must also be enabled. Changing the editing setting updates existing cells immediately. `LeftClickToDeselect` toggles a row on successive single clicks, including Ctrl-clicks. When both settings are enabled, double-click an editable cell or press **F2** to edit while keeping its row selected; checkbox values also change only through that explicit edit gesture. These selection modes also support `SelectOnMouseUp`.

Manual page sizes range from **1 to 9999**, in steps of one. With paging enabled, `AutoPageSize` uses the grid viewport, measured header and row heights, and any horizontal scrollbar to fit complete rows. It recalculates when the control is resized and retains a minimum page size of one for very small controls.

### References from a data source

Assign a `CrudReferenceBinding<TEntity>` to a column's `ReferenceBinding` to select an existing record from a CRUD dropdown. Set `PropertyName` to the entity's property name so the default edit dialog uses the same picker:

```csharp
var ManagerBinding = new CrudReferenceBinding<Employee>(EmployeeDataSource, EmployeeColumns,
	Employee => $"{Employee.FirstName} {Employee.LastName}");
var ManagerColumn = new CrudGridColumn<Employee> {
	ColumnName = "Manager",
	PropertyName = nameof(Employee.Manager),
	DataType = typeof(Employee),
	DisplayType = CrudCellDisplayType.DropDownList,
	PropertyValue = Employee => Employee.Manager,
	SetPropertyValue = (Employee, Value) => Employee.Manager = (Employee)Value,
	CanEditCell = true,
	ReferenceBinding = ManagerBinding
};
```

The picker is read-only, automatically fits complete rows, and supports paging, search, and sorting when the data source supports them. Create, edit, delete, and action columns are omitted. Selecting a row assigns that exact record; **Clear** assigns null when allowed, and **Cancel** or Escape preserves the current reference. Set `AllowNull = false` to remove Clear. `DropDownSize` (also exposed as `MaximumDropDownSize`) sets the popup's content size in device pixels, defaulting to 760 × 380 and capped to the available screen space. The grid fills the popup. Search, paging, and empty results keep its dimensions stable; manual resizing lasts for that open picker.

Set `ExpandsToFit = true` on the column that should occupy spare width, such as the Name column in a reference picker. Other columns keep their measured content widths. Multiple configured columns share the extra space; no column stretches implicitly when none is configured. Resizing recalculates the configured columns from their content widths, and content that cannot fit still uses horizontal scrolling. Automatic page sizing continues to fit complete rows in the available grid height.

`CrudGrid.ReferenceBindings` and `DefaultCrudEntityEditor.ReferenceBindings` also accept bindings keyed by a property path, such as `"Manager"` or `"Address.Contact"`. The grid supplies its own data source automatically for properties whose type matches the edited entity type and which have no custom editor or converter. Explicit bindings take precedence. This avoids expanding recursive entity relationships while ordinary nested values such as Address remain editable.

`CrudComboBox` uses the same column configuration. Its `MaximumDropDownSize` and optional `SetCrudParameters(size: ...)` configure the popup size, capped to the available screen space. Use `ExpandsToFit` in its grid bindings to choose which columns fill spare width.

### Default entity editor

The default CRUD dialog expands ordinary reference properties such as `Address` into editable child rows without requiring attributes on the model. Existing interface and abstract instances can also be expanded. Writable concrete types with public parameterless constructors offer **(Create new)** and **(none)** choices. Circular references stop expanding when they reach an object already shown in the current path.

Nested changes retain object identity. Cancel restores edited values on the original objects, including shared references. Save accepts the new baseline only after persistence succeeds, so a failed save can still be cancelled. Change events report full paths such as `Address.Street`.

Custom converters, editors, and read-only metadata remain in use. Custom editor rollback covers editable object properties and `IList` membership. Applications needing domain-specific reference selection or factories, mutable collection-item editing, or opaque state should provide an `ICrudEntityEditor` implementation.

### DatabaseConnectionPanel

Dynamic database connection UI that loads appropriate controls based on selected provider:

```csharp
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Data;

public class MainForm : Form {
    private DatabaseConnectionPanel _connectionPanel;
    
    public MainForm() {
        InitializeComponent();
        
        _connectionPanel = new DatabaseConnectionPanel();
        
        // Optionally hide certain database types
        _connectionPanel.IgnoreDBMS = new[] { DBMSType.FirebirdFile };
        
        // Handle DBMS type changes
        _connectionPanel.DBMSTypeChanged += OnDBMSChanged;
        
        Controls.Add(_connectionPanel);
    }
    
    private void OnDBMSChanged(DatabaseConnectionPanel sender, DBMSType newType) {
        // Panel automatically loads correct connection UI:
        // - SQLServer: MSSQLConnectionPanel
        // - Sqlite: SqliteConnectionPanel  
        // - Firebird: FirebirdConnectionPanel
        // - FirebirdFile: FirebirdEmbeddedConnectionPanel
    }
    
    private async void TestConnection() {
        var result = await _connectionPanel.TestConnection();
        if (result.IsSuccess) {
            MessageBox.Show("Connection successful!");
            
            // Get the DAC for database operations
            using var dac = _connectionPanel.GetDAC();
            // Use dac...
        } else {
            MessageBox.Show($"Connection failed: {result.ErrorMessages.First()}");
        }
    }
}
```

### PathSelectorControl

File/folder path selection with browse button:

```csharp
using Sphere10.Framework.Windows.Forms;

var pathSelector = new PathSelectorControl();
pathSelector.Mode = PathSelectionMode.File;  // or Folder
pathSelector.Path = @"C:\Data\file.txt";

// User can type path or browse
string selectedPath = pathSelector.Path;
```

### ProgressBarEx

Enhanced progress bar with text display:

```csharp
using Sphere10.Framework.Windows.Forms;

var progressBar = new ProgressBarEx();
progressBar.DisplayStyle = ProgressBarDisplayText.Percentage;
progressBar.Value = 75;  // Shows "75%"
```

### ValidationIndicator

Visual validation state indicator:

```csharp
using Sphere10.Framework.Windows.Forms;

var validator = new ValidationIndicator();
validator.State = ValidationState.Valid;    // Green checkmark
validator.State = ValidationState.Invalid;  // Red X
validator.State = ValidationState.Pending;  // Yellow
```

### RadioGroupBox

Group box with built-in radio button management:

```csharp
using Sphere10.Framework.Windows.Forms;

var radioGroup = new RadioGroupBox();
// Radio buttons inside are mutually exclusive
```

### ServiceStatusControl

Display Windows service status:

```csharp
using Sphere10.Framework.Windows.Forms;

var serviceStatus = new ServiceStatusControl();
serviceStatus.ServiceName = "MyService";
// Displays: Running, Stopped, Starting, etc.
```

## 🛠️ Tools.WinForms Namespace

```csharp
using Tools;

// Create custom cursor from bitmap
Cursor cursor = WinForms.CreateCursor(bitmap, hotspotX, hotspotY);

// Load cursor from raw bytes
Cursor rawCursor = WinForms.LoadRawCursor(cursorBytes);
```

## 📋 Available Components

### Controls
| Control | Description |
|---------|-------------|
| `CheckedGroupBox` | Group box with checkbox header |
| `PathSelectorControl` | File/folder path selection |
| `ProgressBarEx` | Progress bar with text display |
| `PropertyGridEx` | Enhanced property grid |
| `RadioGroupBox` | Radio button group container |
| `ServiceStatusControl` | Windows service status display |
| `ValidationIndicator` | Visual validation state |
| `ExpandingCircle` | Animated expanding circle |
| `PictureBoxEx` | Enhanced picture box |
| `UserControlEx` | Enhanced user control base |

### Database Components
| Component | Description |
|-----------|-------------|
| `DatabaseConnectionPanel` | Dynamic DBMS connection UI |
| `DatabaseConnectionBar` | Compact connection bar |
| `ConnectionPanelBase` | Base class for connection panels |
| `IDatabaseConnectionProvider` | Interface for connection providers |

### Other
| Component | Description |
|-----------|-------------|
| `WinFormsActionWizard<TModel>` | Multi-step wizard with native presentation |
| `SourceGrid` | Advanced data grid |
| `LoadingCircle` | Loading animation |
| `ExplorerBar` | Explorer-style navigation bar |
| `AppointmentBook` | Appointment/calendar UI |
| `WinFormsApplicationBlock` | Modular application sections with menus |

## 🏗️ Application Blocks

Application blocks provide a modular way to organize application features with menu structures. The builder pattern provides a fluent API for construction:

```csharp
using Sphere10.Framework.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

// Register an application block using the builder pattern
serviceCollection
	.AddApplicationBlock(
		new WinFormsApplicationBlockBuilder()
			.WithName("My Block")
			.WithDefaultScreen<MainScreen>()
			.ConfigureMenu(mb => mb
				.WithText("File")
				.AddActionItem("New", () => CreateNew())
				.AddActionItem("Open", () => OpenFile())
				.AddScreenItem<SettingsScreen>("Settings")
			)
			.ConfigureMenu(mb => mb
				.WithText("Tools")
				.AddScreenItem<ToolA>("Tool A")
				.AddScreenItem<ToolB>("Tool B")
			)
			.Build()
	);

// Or create a reusable builder method
public class MyApplicationBlock {
	public static WinFormsApplicationBlock Build() {
		return new WinFormsApplicationBlockBuilder()
			.WithName("My Application")
			.WithDefaultScreen<DashboardScreen>()
			.ConfigureMenu(mb => mb
				.WithText("Main Menu")
				.AddScreenItem<Screen1>("Option 1")
				.AddScreenItem<Screen2>("Option 2")
				.AddActionItem("Custom Action", () => DoSomething())
			)
			.Build();
	}
}

// Register in ModuleConfiguration
serviceCollection.AddApplicationBlock(MyApplicationBlock.Build());
```

Owner-aware actions receive the executing `IUserInterfaceServices`, including actions marked `executeOnLoad`:

```csharp
menu
	.AddActionItem("Ready", userInterface => userInterface.Status = "Ready")
	.ConfigureItem(item => item
		.AsActionItem()
		.WithText("Report status")
		.WithAction(userInterface => userInterface.Status = "Ready")
	);
```

The parameterless `Action` overload remains available. Native action items take synchronous delegates; an `async` lambda supplied to one becomes `async void`, and command dispatch cannot await its completion. Await asynchronous dialogs and other work from a `Task`-returning method or a native async event handler as shown above. `PrimaryUIController` supplies the form when a callback needs its native owner. `AddItem(existingItem)` registers an existing item; `ConfigureItem(item => ...)` configures a child builder and adds its built item. `AddScreenItem` and `AddActionItem` construct their definitions directly without invoking a child builder.

The builder pattern supports:
- **Screen items**: Navigate to specific forms/screens
- **Action items**: Execute custom code when clicked
- **Menu organization**: Group related features logically
- **Fluent API**: Chain methods for clean, readable code
- **Type-safe**: Compile-time checking with generics

### Grouping blocks into application plugins

The running UI hierarchy is `IApplication` → `IApplicationBlock` → menus and screens. Native extensions use `IWinFormsApplication`, `IWinFormsApplicationBlock` and their native menu and screen contracts. A plugin contributes startup services and any number of blocks, including zero. `LoadedPlugins` records those startup contributors; every contributed block has its own navigation icon and menus.

In your existing module's `RegisterComponents(IServiceCollection serviceCollection)` method, group blocks with the native plugin builder:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;

serviceCollection
	.AddWinFormsApplicationPlugin(plugin => plugin
		.WithName("Administration")
		.RegisterInitializer<AdministrationInitializer>()
		.RegisterFinalizer<AdministrationFinalizer>()
		.ConfigureServices(services => services.AddSingleton<AdministrationService>())
		.ConfigureBlock(block => block
			.WithName("Administration")
			.WithDefaultScreen<DashboardScreen>()
			.ConfigureMenu(menu => menu
				.WithText("Management")
				.AddScreenItem<SettingsScreen>("Settings")
			)
		)
	);
```

`AdministrationService`, `DashboardScreen` and `SettingsScreen` are application types; screens derive from `WinFormsApplicationScreen`. The lifecycle types can use the root's logger and application-owned dependencies:

```csharp
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

public sealed class AdministrationInitializer : AsyncApplicationInitializerBase {
	private readonly IApplication _application;

	public AdministrationInitializer(IApplication application) => _application = application;

	public override Task InitializeAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		_application.Logger.Info("Administration components are ready.");
		return Task.CompletedTask;
	}
}

public sealed class AdministrationFinalizer : AsyncApplicationFinalizerBase {
	private readonly IApplication _application;

	public AdministrationFinalizer(IApplication application) => _application = application;

	public override Task FinalizeAsync(CancellationToken cancellationToken = default) {
		_application.Logger.Info("Administration components are finalized.");
		return Task.CompletedTask;
	}
}
```

Replace these demonstration messages with the plugin's actual startup and cleanup work. For a reusable definition, call `new WinFormsApplicationPluginBuilder()...Build()` and pass its result to `AddWinFormsApplicationPlugin(plugin)`. `ConfigureBlock(block => ...)` builds a nested block; `AddBlock(block)` registers an existing `IWinFormsApplicationBlock`.

Registration invokes `Load(IServiceCollection)` once, validates the completed contribution, and supplies its blocks to the existing `AddApplicationBlock` startup path. Only after those registrations succeed does it publish an `ApplicationPluginRegistration<IWinFormsApplicationPlugin>` receipt containing the original plugin and a defensive copy of its contributed block membership. The application factory reads these receipts; manually registering an unloaded plugin in DI does not add it to `LoadedPlugins`. Failed loading or contribution validation does not publish a receipt. Later changes to a plugin's block getter do not change the captured startup contribution.

Registration rejects both duplicate successful plugin names and recursive attempts to load the same name. A failed registration discards newly added lifecycle task descriptors under the attempted name, preventing a later successful plugin with that name from inheriting abandoned tasks. Ordinary `ConfigureServices` side effects retain their existing behavior.

The same plugin instance remains available through native and shared DI contracts. `WinFormsApplication.Initialize()` and `await WinFormsApplication.InitializeAsync()` register each native block once, retaining position ordering, default screens and `ExecuteOnLoad` behavior. Startup selection follows receipt and block declaration order, while `Position` orders the navigation icons. Resolving or disposing an application adapter does not load, unload or dispose plugin definitions. Block objects, menu parent links, images and disposal ownership remain with the existing native code.

`WinFormsApplicationPlugin` uses `ApplicationPluginBase<IWinFormsApplicationBlock>` and retains compatibility with `ApplicationPlugin`, including typed virtual `Blocks` overrides. Shared screen-host projections live in the generic interfaces. Native hosts and decorators inherit the shared mutable `ScreenMode` contract; virtual overrides dispatch through both shared and native views.

Existing `AddApplicationBlock` calls and the `WinFormsApplication(form, blocks)` constructor remain supported. Their blocks appear directly in the application's live `Blocks` array without creating a plugin. The three-argument constructor accepts `loadedPlugins` that the caller has already loaded and captures their contributions without invoking lifecycle methods. Successfully registered plugin names must be unique, and a block reference cannot be contributed by two plugins.

`IWinFormsApplication.LoadedPlugins` returns a snapshot of the original loaded plugin instances. `Blocks` projects the host's current registered blocks and follows direct registration and guarded removal. Removing a contributed block does not unload its plugin. Select navigation through `ScreenHost.ActiveBlock` and screen operations through the host; plugins have no active UI selection. The shared `IApplication` resolves to the same application object and projects the same loaded plugins and live blocks. `BlockMainForm.BlockBindings` maps each block to its native navigation pane.

### Remembering the main window

Enable automatic per-user window settings at application startup:

```csharp
using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;

Sphere10Framework.Instance
	.BuildWinFormsApplication()
	.UseMainForm<BlockMainForm>()
	.UseMainFormSettings()
	.StartWinFormsApplication();
```

`UseMainFormSettings()` restores the main window's normal bounds, monitor and maximized state, captures placement after all exit permissions approve shutdown, and writes it once through a framework shutdown task before the window closes and settings providers are disposed. The later `FormClosed` event does not queue another save. Moving, resizing, and dragging the menu divider do not write settings. Cancelled exit requests do not queue a save. A minimized window reopens in its last normal or maximized state. Missing monitors and changed working areas or DPI are handled by fitting the window onto an available display. Without saved settings, the configured initial size is used.

`BlockMainForm` also saves the left navigation pane's width in the same `FormWindowSettings`, including its last expanded width while collapsed or temporarily hidden. The restored width scales with DPI and is clamped to the current navigation limits. With no saved width, the wider default applies.

`FormWindowSettings` uses the existing `UserSettings` provider and `SettingsObject.Save()` contract. The default settings ID is `"MainForm"`; supply `SettingsID` to distinguish multiple main-window configurations. `UseMainFormSettings(false)` disables automatic persistence. Optional preference read/write failures are logged and do not prevent opening or closing the application.

Other forms can keep `Tools.WinForms.AutoPersistWindowSettings(Form, SettingsID)` in a `using` scope for the window's lifetime to save once on accepted closure, or use `CaptureWindowSettings` and `RestoreWindowSettings` with their own `FormWindowSettings` instance. See the repository [user-settings skill](../../agents/skills/user-settings/SKILL.md) for settings classes, defaults, lifecycle and storage conventions.

### Shared presentation contracts

The portable contracts and implementation state live in `Sphere10.Framework.Application.UI`. Native adapters use explicit `WinForms` names, so both namespaces can be imported together without aliases. `ScreenActivationMode` comes directly from the shared namespace; there is no separate native enum. WinForms `IWinFormsApplicationBlock`, `IWinFormsApplicationMenu`, `IWinFormsApplicationMenuItem` and `IWinFormsScreenMenuItem` extend their shared counterparts with typed collections and native presentation options. Custom blocks supply `ScreenDefinitions` and `DefaultScreenDefinition`; custom screen items supply a canonical `ScreenDefinition` reference. Shared metadata has no string IDs. `WinFormsApplicationBlock` and `WinFormsApplicationMenu` derive from abstract `ApplicationBlockBase` and `ApplicationMenuBase<TItem>`. `WinFormsApplicationMenuItemBase` and `WinFormsLinkMenuItemBase` are abstract native intermediates above the concrete screen and action items. Native APIs retain images, menu parents, array views and disposal ownership. The fluent native builders inherit common metadata, screen policies and child-builder composition from typed shared bases, then create WinForms products on the first `Build()`. Plugin `ConfigureBlock`, block `ConfigureMenu`, menu `ConfigureItem`, their corresponding `Add` methods and common setters live in `Sphere10.Framework.Application.UI`; native adapters retain images, menu/explorer/toolbar flags and control factories. Subsequent builds return the same product and incorporate additional canonical definitions. Later name, position, default and menu configuration updates the existing native product, preserving one owner for menus, items and images.

`IWinFormsApplicationScreen` extends the shared `IApplicationScreen` contract with synchronous native hooks. `WinFormsApplicationScreen` maps those hooks to its existing display and cancelable hide events on the calling UI thread. Shared `CanDeactivateAsync` invokes the native hide guard; `OnDeactivatedAsync` does not emit the hide notification again. Cancellation is checked before invoking a native hook. The native presentation hooks remain synchronous; the host also exposes the shared asynchronous operations for controls, tabs, docking and screen disposal.

Screen definitions carry activation modes for both UI hosts. The host resolves declarations and captures a constructor's mode in a definition when no mode was declared, then validates subsequent instances against that metadata. The native adapter uses the shared `ScreenActivationMode` enum directly; an unspecified menu mode still leaves the initial choice to the screen constructor. Help metadata remains available through the shared screen contract.

### Running application and shared wizards

`IWinFormsApplication : Sphere10.Framework.Application.UI.IApplication` exposes the running `BlockMainForm` through live blocks, loaded startup plugins, its screen host, retained unsaved state and change notifications. The host owns active block and screen selection. For `BlockMainForm` and its subclasses, `AddMainForm` registers the native and shared interfaces to the same adapter; framework startup initializes its configured blocks. The adapter borrows the form and screen host rather than creating a second owner. `SetMenus` and `SetToolBarItems` accept native command definitions and update the actual application menu and toolbar using the form's existing bindings and execution path.

`WinFormsWizardBuilder<TModel>`, `WinFormsActionWizard<TModel>`, `IWinFormsWizard<TModel>`, `WinFormsWizardScreen<TModel>` and `WinFormsWizardDialog<TModel>` replace the unprefixed native wizard names. Navigation, step membership, callbacks and completion policy use the shared `Application.UI` wizard workflow; native adapters retain control initialization, validation, presentation, sizing and disposal. Dynamically removed screens remain owned by their wizard and are disposed with it. `WizardResult`, `WizardStepUpdateType` and `ScreenMode` now come directly from the shared UI namespace.

### Multiple screens and detachable tabs

`MainForm.ScreenMode` defaults to `ScreenMode.SingleView`, which shows one screen without a tab bar. Set it to `ScreenMode.MultiView` to keep multiple screens open in titled tabs. `BlockMainForm` inherits this property and hosts the tabs in its main content panel.

```csharp
using Sphere10.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;

var block = new WinFormsApplicationBlockBuilder()
	.WithName("Workspace")
	.WithDefaultScreen<DashboardScreen>(title: "Dashboard")
	.ConfigureMenu(menu => menu
		.WithText("Views")
		.AddScreenItem<DashboardScreen>("Dashboard")
		.AddScreenItem<EditorScreen>("New editor", title: "Editor")
		.ConfigureItem(item => item
			.AsScreenItem()
			.WithText("New report")
			.WithScreen<ReportScreen>()
			.AsMultiInstance()
			.WithTitle("Report")
		)
	)
	.Build();

Sphere10Framework.Instance
	.BuildWinFormsApplication()
	.UseMainForm<BlockMainForm>(form => form.ScreenMode = ScreenMode.MultiView)
	.ConfigureServices(services => services.AddApplicationBlock(block))
	.StartWinFormsApplication();

public class DashboardScreen : WinFormsApplicationScreen {
	public DashboardScreen() => ActivationMode = ScreenActivationMode.SingleInstance;
}

public class EditorScreen : WinFormsApplicationScreen {
	public EditorScreen() => ActivationMode = ScreenActivationMode.MultiInstance;
}

public class ReportScreen : WinFormsApplicationScreen {
}
```

Screen builders declare the type's instance policy with `.AsSingleInstance()` or `.AsMultiInstance()`. `SingleInstance` allows one instance of that type per host across all blocks and menu entries, including detached windows. `MultiInstance` (the default) allows a new independent instance on each menu activation. Declare `.AsSingleInstance()` explicitly for settings or other screens that must be reused. The host validates all explicit declarations for a block before opening its screens; an unspecified menu entry inherits any declaration for the same type. Conflicting declarations or changes to a resolved type policy are rejected, even after its last instance closes. Without a builder declaration, the screen's constructor supplies `ActivationMode` through its protected setter. The property cannot change while hosted. The former `KeepAlive` and `AlwaysCreate` names are now `SingleInstance` and `MultiInstance`, respectively.

`WinFormsApplicationScreen.Title` uses the control's `Text` property. Menu navigation supplies the menu text as the initial title unless a specific builder title is provided. Screens can subsequently change `Title`; the tab and detached window update immediately. A block's default screen opens during registration if there is no active screen after its `ExecuteOnLoad` items have run.

Tab widths grow with their titles up to `WinFormsApplicationScreenTabControl.MaximumTabWidth`, which defaults to 260 logical pixels. Longer titles display an ellipsis, with the full title available on hover. Tab spacing, close buttons, docking markers, and the width limit scale for the current monitor's DPI.

Selecting a tab restores the previous screen's toolbar items and removes its screen menu, then merges the selected screen's `ToolBar` and registered `MenuItems` into the main form. The same item instances and event handlers are retained. When detached, `WinFormsApplicationScreenForm` displays the registered `MenuItems` directly as top-level menus, including File when supplied, regardless of `ShowInApplicationMenuStrip`. It retains the original `ToolBar` control, layout, and handlers instead of copying its items into a replacement strip. The selected docked screen continues to supply the main window's menu and toolbar. Redocking selects the returned tab and merges its items back into the main form.

Detached windows use a compact caption with accessible **Re-dock**, **Minimize**, **Maximize/Restore**, and **Close** icon buttons. The title area supports native dragging and double-click maximize/restore, and the border remains resizable. Close and re-dock preserve the screen's cancellation checks. Only menus and toolbars supplied by the screen occupy space; a content-only screen starts immediately below the caption, without empty bars or a detached tab row.

- Close a tab with its close button, middle-click, or its context menu.
- Undock using the tab context menu, or drag the selected tab away from the tab headers and release.
- Drag tabs along the tab bar to see them move immediately; Escape restores the original position.
- In a detached window, use the caption's **Re-dock** icon, or bring the window's title bar close to the main tab strip. Only the header band accepts drag docking; the screen content panel does not. A highlighted tab-sized **Release to dock** preview and text hint show the proposed position and screen title without changing tab widths, order, or selection. The highlight uses the ExplorerBar palette and follows the visible navigation pane's blue in `BlockMainForm`. Leaving the target or cancelling the drag clears the preview.

The left navigation pane starts at 320 logical pixels wide and scales with the form. Drag its divider to choose a different width. `MaximumNavigationPaneWidth` defaults to 480 logical pixels at 96 DPI; the effective limit also reserves 320 logical pixels for the screen when space permits. Limits scale with DPI and clamp oversized saved widths. Minimized or temporarily undersized layouts defer divider updates until both panels fit; the remembered width remains available for restoration and settings capture. Collapse and restore the entire left navigation pane with the **sidebar icon** in the main toolbar, or **Ctrl+Alt+M**. The icon remains available across screen switches and supplies Show/Hide sidebar tooltips. It does not reserve a separate header or collapsed gutter. `BlockMainForm.NavigationPaneCollapsed` exposes the preference. The pane remembers its width and collapsed state across screen switches. Filled screens temporarily hide navigation without changing that preference.

`MainForm.ScreenHost` exposes `ActivateScreen`, `ShowScreen`, `CloseScreen`, `CloseScreens`, `CanCloseScreens`, `UndockScreen`, `DockScreen`, `OpenScreens` and `Screens`. `ActiveScreen` identifies the selected docked screen; detached windows retain independent chrome. `Screens` also includes cached single-instance screens hidden in SingleView. `ActivateScreen` and menu navigation reuse an existing singleton. Direct `ShowScreen` calls reject a second singleton instance; the caller still owns a rejected instance. A screen belongs to one host at a time.

`LoadedPlugins`, `Blocks`, `Menus`, `Items`, `IBlockManager.RegisteredBlocks`, `Screens` and `OpenScreens` expose arrays. The built-in models, application, block manager and screen host return snapshots: replacing entries in a returned array does not change their owned collections. Use menu mutation methods, block registration methods and screen-host operations to change those collections. The referenced plugins, blocks, menus and screens retain their existing identity and disposal ownership.

SingleView retains hidden single-instance screens and disposes multi-instance screens when navigating away, preserving the previous behavior. Explicit close destroys an ordinary instance; opening it again constructs a fresh screen. Switching from MultiView to SingleView closes other ordinary open screens and retains the selected screen. Permanent singletons remain owned and return to the tab strip when MultiView is restored. Use `ScreenHost.TrySetScreenMode` to handle cancellation without an exception; assigning `ScreenMode` throws if a screen vetoes the change.

`ScreenActivationMode.PermanentSingleton` adds an anchored lifetime. Declare it with `.AsPermanentSingleton()` on a screen item, or pass `activationMode: ScreenActivationMode.PermanentSingleton` to `WithDefaultScreen`. These registered permanent screens are created once at startup, remain in `OpenScreens` across both view modes, and reject close and undock requests. Ordinary navigation away remains allowed. Declare the policy in registration when it must open automatically; a constructor-only default is learned when that type is first activated.

`ScreenKind.Empty` marks a tabless fallback shown only when no regular screen remains open (detached windows and permanent screens also count as open). It is excluded from `OpenScreens`, while `Screens` includes a cached Empty singleton. Navigating away retains a singleton's controls and disposes a multi-instance placeholder; returning either reuses or recreates it. Empty and PermanentSingleton cannot be combined.

```csharp
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;

var block = new WinFormsApplicationBlockBuilder()
	.WithName("Workspace")
	.ConfigureMenu(menu => menu
		.WithText("Screens")
		.ConfigureItem(item => item
			.AsScreenItem()
			.WithText("Start")
			.WithScreen<StartScreen>()
			.AsSingleInstance()
			.AsDefault()
		)
		.ConfigureItem(item => item
			.AsScreenItem()
			.WithText("Pinned notes")
			.WithScreen<NotesScreen>()
			.AsPermanentSingleton()
		)
	)
	.Build();

var emptyBlock = new WinFormsApplicationBlockBuilder()
	.WithName("Empty workspace")
	.WithDefaultScreen<EmptyScreen>("Nothing open",
		activationMode: ScreenActivationMode.SingleInstance, screenKind: ScreenKind.Empty)
	.Build();
```

The screen types in this example derive from `WinFormsApplicationScreen`. Startup selects the first `.AsDefault()` or block `WithDefaultScreen` in plugin, block, then menu declaration order. A permanent screen prevents an Empty default from taking selection. Block `Position` continues to order the displayed navigation independently. If no explicit default exists, existing `IsStartScreen` / `ExecuteOnLoad` screen registrations keep their behavior; startup actions still execute.

`BlockMainForm.RegisterBlock` initializes new permanent screens without replacing an ordinary active screen. If an active Empty screen refuses the transition, the new block remains unregistered and undisposed so the caller can retry after clearing the guard. For a standalone host, `InitializeScreens(blocks)` returns whether initialization was accepted. `UnregisterScreenTypes(block)` removes its definitions and owned screens after all close guards agree. The form's existing `UnregisterBlock` uses that path, allowing administrative removal of permanent screens and disposal of hidden Empty instances. `CanCloseScreens(Screens)` remains the application-exit guard: permanent lifetime does not prevent shutdown.

The existing `OnHide(ref bool CancelHide)` and `ScreenHidden` event can cancel navigation, close, undock, redock, block removal, and changes to SingleView. Batch close and mode changes check all affected screens before removing any. `ScreenDisplayedFirstTime` runs once per instance; `ScreenDisplayed` runs on each subsequent selection or hosting transition. Closing or disposing the host also disposes hidden and detached instances, with `ScreenDestroyed` raised once per instance.

The reusable host follows `IWinFormsApplicationScreenHost` → `WinFormsApplicationScreenHostBase` → `WinFormsApplicationScreenHost`, with `WinFormsApplicationScreenHostDecorator` for extensions. `WinFormsApplicationScreenTabControl` supplies the tab interactions and `WinFormsApplicationScreenForm` hosts detached screens.

The **WinForms Tester** loads two plugins through `AddWinFormsApplicationPlugin`: `WinFormsDemoPlugin.Configure` contributes Workspace and Notes, while `WinFormsComponentDemoPlugin.Configure` contributes Controls, Data and collections, Integration, and Utilities. Each plugin registers its own initialization and finalization tasks from the tester's `Lifecycle` folder, alongside the existing framework startup tasks. It starts in MultiView and uses `HighDpiMode.DpiUnaware` so Windows bitmap-scales its existing screen and dialog layouts. **Workspace > Screen hosting** explicitly declares **Settings** as single-instance; **New design** and ordinary component screens use the multi-instance default. Both screen modes are available, and the hidden Empty screen belongs to Workspace. These demos have independent notes, a counter, **Rename tab** actions, a lifecycle log, and a checkbox to exercise cancellation. **Plain screen (no bars)** exercises a screen with no menus or toolbar. See the [Tester instructions](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md). [SystemExpert](../../utils/SystemExpert/README.md) also uses MultiView; monitoring tools use the framework's default multi-instance policy unless they explicitly declare another lifetime.

`Directory.Build.props` sets `ForceDesignerDPIUnaware=true` for the Visual Studio WinForms designer across the repository. This designer setting is separate from runtime DPI awareness. The library leaves runtime DPI policy to the application; Tester explicitly selects `HighDpiMode.DpiUnaware` before creating controls to preserve Windows bitmap scaling.

## 📦 Dependencies

- **Sphere10.Framework**: Core framework
- **Sphere10.Framework.Data**: Database abstraction
- **System.Windows.Forms**: Windows Forms (.NET built-in)
- **System.Drawing**: Graphics support

## 📖 Related Projects

- [Sphere10.Framework.Windows.Forms.Sqlite](../Sphere10.Framework.Windows.Forms.Sqlite) - SQLite connection panel
- [Sphere10.Framework.Windows.Forms.MSSQL](../Sphere10.Framework.Windows.Forms.MSSQL) - SQL Server connection panel  
- [Sphere10.Framework.Windows.Forms.Firebird](../Sphere10.Framework.Windows.Forms.Firebird) - Firebird connection panel
- [Sphere10.Framework.Windows](../Sphere10.Framework.Windows) - Windows platform integration
- [Sphere10.Framework.Data](../Sphere10.Framework.Data) - Database abstraction layer

## ✅ Status & Compatibility

- **Maturity**: Production-tested for Windows desktop applications
- **.NET Target**: .NET 10.0 (Windows)
- **Platform**: Windows only (Windows Forms)

## ⚖️ License

Distributed under the **MIT License**.

See the LICENSE file for full details. More information: [MIT License](https://opensource.org/license/mit)

## 👤 Author

**Herman Schoenfeld** - Software Engineer

## Native helper migration

Reusable native interop now lives in `Sphere10.Framework.Windows`, accessed through `WinAPI` and `Tools.Windows.Win32`. Windows Forms retains control lifecycle, message dispatch, and wrappers such as `Tools.WinForms.CreateCursor`. Sound playback moved to `Sphere10.Framework.Windows.SoundPlayerEx`.

Code using the former low-level APIs should update these references:

- `Tools.WinForms.IconInfo`, `CreateIconIndirect`, and `GetIconInfo` moved to `WinAPI.USER32.ICONINFO`, `WinAPI.USER32.CreateIconIndirect`, and `WinAPI.USER32.GetIconInfo`.
- `DevAgeRichTextBox`'s native constants and protected `CHARFORMAT` moved to `WinAPI.RICHEDIT`; derived controls should pass `WinAPI.RICHEDIT.CHARFORMAT` to `SetCharFormatMessage`.
- Popup/combo-box native constants moved to `WinAPI.USER32`; word extraction helpers moved to `Tools.Windows.Win32`. Native sizing/tab structures now use `WinAPI.USER32.MINMAXINFO` and `WinAPI.COMCTL32.TCITEM`.

These low-level moves require recompilation and the listed source updates for consumers of the moved members. Rich-edit formatting, cursor hotspots, native struct sizes, and hook disposal have regression coverage in `WindowsInteropTests`. The cursor wrapper does not transfer native-handle ownership to `Cursor.Dispose`; direct native consumers should use `CreateCursorHandle` and release their handle explicitly.

Ordinary custom forms can override `OnFormClosing(FormClosingEventArgs)`. `LiteMainForm` seals that workflow to coordinate `IApplication.ExitAsync`; application forms can veto normal exit through `OnApplicationExiting(CancelEventArgs)` or `ApplicationExitingEvent`. Image attachment selection returns the selected file or clipboard source instead of always reporting `None`.

Legacy DES stream helpers retain their existing full-length input buffer and zero-filled tail at end of input; short reads are completed before encryption or decryption. The existing DES key, IV, and ciphertext format are preserved.


Application, block and screen command layers compose through `Tools.UI.MergeApplicationMenus` and `MergeApplicationToolBarItems`. Application commands survive contextual commands with the same caption; screen commands replace matching block commands. Native captions merge without mnemonic markers, including contributions to the existing File and Help headers. Exit and help commands survive screen changes, with Help last. Native presentation honors block/menu placement flags and preserves caller-owned menu and item references.

`Control.EnterRenderingScope()` defers layout and native redraw across the control tree. Nested scopes, added controls, newly created handles and newly shown controls share suppression. The last scope flushes parent/child layout and performs one redraw; disposed and intentionally hidden controls remain so. Dispose scopes on the creating UI thread. Screen-host transitions use this scope around the form and publish one completed selection and command update.

Restoring redraw preserves each control's own requested visibility, rather than its effective `Visible` value inherited from a hidden parent. The base `Control.Visible` designer descriptor supplies that state through `ShouldSerializeValue`, even when a derived control declares a different default. Native regression checks must inspect actual window visibility after hiding and reactivating a retained view: healthy managed bounds alone do not prove its dropdowns and search controls are painted.
