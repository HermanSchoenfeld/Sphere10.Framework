<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

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
private async void EditButton_Click(object Sender, EventArgs Args) {
	var (Accepted, UserInput) = await EnterTextDialog.ShowAsync(this, "Edit name", "Name", "Current name");
	if (Accepted)
		await DialogEx.ShowAsync(this, SystemIconType.Information, "Saved", UserInput, "OK");
}
```

Use `ShowAsync` on `DialogEx`, `ExceptionDialog`, `EnterTextDialog`, `PasswordDialog`, `LogonDialog`, and `CrudDialog`; use `GenericEditorForm.ShowFormAsync` and `QuestionDialogSession.AskQuestionAsync`. Password input returns `(DialogResult Result, string Password)`; text input returns `(bool Accepted, string UserInput)`. These helpers dispose the dialogs they create after completion. When calling native `Form.ShowDialogAsync` on your own form, keep it in a `using` scope across the `await`.

Custom forms use .NET WinForms directly: `using var Dialog = new MyDialog(); var Result = await Dialog.ShowDialogAsync(this);`. The old generic modal extension and the fallback that posted synchronous `ShowDialog` calls have been removed. `InvokeAsyncEx` remains a UI dispatch convenience over native `Control.InvokeAsync`, propagating completion, exceptions, and cancellation; it does not implement a modal loop.

`IApplicationDialog` declares the native `ShowDialogAsync` overloads, implemented directly by the inherited `Form` methods. There is no legacy synchronous fallback. The caller retains ownership of an existing dialog. CRUD create, edit, and delete methods return `Task`; SourceGrid deletion uses `DeleteSelectedRowsAsync`. Custom `DialogEx` button behavior overrides `OnProcessButtonAsync`.

`Wizard.Start(owner)` also awaits native `Form.ShowDialogAsync`, keeping its owner disabled until completion or cancellation and disposing the wizard dialog afterward.

Wizard screens receive their `Wizard` and `Model` before `Initialize`, including screens added with `InjectScreen`. Navigation calls `Validate` before `OnNext`; commit accepted input in `OnNext` when invalid input must not change the model. Set `UpdateModelOnStateChanged = false` for this pattern. The default `OnPresent` calls `CopyModelToUI`, so input and summary screens can refresh their controls each time they are shown.

WinForms closing and validation events still require cancellation and handled flags to be set before the first `await`. Synchronous grid queries and model construction can only schedule asynchronous error reporting; they cannot await a modal result. `IWindowsFormsEditorService.ShowDialog`, common file/folder/color dialogs, and the synchronous `LiteMainForm.AskYN` contract retain synchronous modal calls. `AskYN` uses a native message box; blocking `ShowDialogAsync` with `.Result` or `.GetAwaiter().GetResult()` on the UI thread would deadlock.

Run the STA regression tests with `dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj` on Windows. They exercise actual forms with a message loop and close their dialogs automatically.

File > Exit and the close button use an asynchronous confirmation backed by native WinForms modality. The initial close is cancelled before awaiting confirmation, and an accepted request closes the form after screen and application veto checks. Framework shutdown occurs after `Application.Run` returns, allowing normal window disposal to finish first.

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
| `Wizard` | Multi-step wizard framework |
| `SourceGrid` | Advanced data grid |
| `LoadingCircle` | Loading animation |
| `ExplorerBar` | Explorer-style navigation bar |
| `AppointmentBook` | Appointment/calendar UI |
| `ApplicationBlock` | Modular application sections with menus |

## 🏗️ Application Blocks

Application blocks provide a modular way to organize application features with menu structures. The builder pattern provides a fluent API for construction:

```csharp
using Sphere10.Framework.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

// Register an application block using the builder pattern
serviceCollection.AddApplicationBlock(
    new ApplicationBlockBuilder()
        .WithName("My Block")
        .WithDefaultScreen<MainScreen>()
        .AddMenu(mb => mb
            .WithText("File")
            .AddActionItem("New", () => CreateNew())
            .AddActionItem("Open", () => OpenFile())
            .AddScreenItem<SettingsScreen>("Settings")
        )
        .AddMenu(mb => mb
            .WithText("Tools")
            .AddScreenItem<ToolA>("Tool A")
            .AddScreenItem<ToolB>("Tool B")
        )
        .Build()
);

// Or create a reusable builder method
public class MyApplicationBlock {
    public static ApplicationBlock Build() {
        return new ApplicationBlockBuilder()
            .WithName("My Application")
            .WithDefaultScreen<DashboardScreen>()
            .AddMenu(mb => mb
                .WithText("Main Menu")
                .AddScreenItem<Screen1>("Option 1")
                .AddScreenItem<Screen2>("Option 2")
                .AddActionItem("Custom Action", async () => {
                    // Custom action logic
                    await DoSomethingAsync();
                })
            )
            .Build();
    }
}

// Register in ModuleConfiguration
serviceCollection.AddApplicationBlock(MyApplicationBlock.Build());
```

The builder pattern supports:
- **Screen items**: Navigate to specific forms/screens
- **Action items**: Execute custom code when clicked
- **Menu organization**: Group related features logically
- **Fluent API**: Chain methods for clean, readable code
- **Type-safe**: Compile-time checking with generics

### Remembering the main window

Enable automatic per-user window settings at application startup:

```csharp
Sphere10Framework.Instance
	.BuildWinFormsApplication()
	.UseMainForm<BlockMainForm>()
	.UseMainFormSettings()
	.StartWinFormsApplication();
```

`UseMainFormSettings()` restores the main window's normal bounds, monitor and maximized state, captures its placement on accepted closure, and writes it once through a framework shutdown task before settings providers are disposed. Moving, resizing, and dragging the menu divider do not write settings. Cancelled exit requests do not queue a save. A minimized window reopens in its last normal or maximized state. Missing monitors and changed working areas or DPI are handled by fitting the window onto an available display. Without saved settings, the configured initial size is used.

`BlockMainForm` also saves the left navigation pane's width in the same `FormWindowSettings`, including its last expanded width while collapsed or temporarily hidden. The restored width scales with DPI and is clamped to the current navigation limits. With no saved width, the wider default applies.

`FormWindowSettings` uses the existing `UserSettings` provider and `SettingsObject.Save()` contract. The default settings ID is `"MainForm"`; supply `SettingsID` to distinguish multiple main-window configurations. `UseMainFormSettings(false)` disables automatic persistence. Optional preference read/write failures are logged and do not prevent opening or closing the application.

Other forms can keep `Tools.WinForms.AutoPersistWindowSettings(Form, SettingsID)` in a `using` scope for the window's lifetime to save once on accepted closure, or use `CaptureWindowSettings` and `RestoreWindowSettings` with their own `FormWindowSettings` instance. See the repository [user-settings skill](../../.github/skills/user-settings/SKILL.md) for settings classes, defaults, lifecycle and storage conventions.

### Multiple screens and detachable tabs

`MainForm.ScreenMode` defaults to `ScreenMode.SingleView`, which shows one screen without a tab bar. Set it to `ScreenMode.MultiView` to keep multiple screens open in titled tabs. `BlockMainForm` inherits this property and hosts the tabs in its main content panel.

```csharp
Sphere10Framework.Instance
	.BuildWinFormsApplication()
	.UseMainForm<BlockMainForm>(Form => Form.ScreenMode = ScreenMode.MultiView)
	// Register the application's modules here.
	.StartWinFormsApplication();

var Block = new ApplicationBlockBuilder()
	.WithName("Workspace")
	.WithDefaultScreen<DashboardScreen>(title: "Dashboard")
	.AddMenu(Menu => Menu.WithText("Views")
		.AddScreenItem<DashboardScreen>("Dashboard")
		.AddScreenItem<EditorScreen>("New editor", title: "Editor")
		.ConfigureItem(Item => Item.AsScreenItem()
			.WithText("New report")
			.WithScreen<ReportScreen>()
			.AsMultiInstance()
			.WithTitle("Report")))
	.Build();

public class DashboardScreen : ApplicationScreen {
	public DashboardScreen() => ActivationMode = ScreenActivationMode.SingleInstance;
}

public class EditorScreen : ApplicationScreen {
	public EditorScreen() => ActivationMode = ScreenActivationMode.MultiInstance;
}
```

Screen builders declare the type's instance policy with `.AsSingleInstance()` or `.AsMultiInstance()`. `SingleInstance` (the default) allows one instance of that type per host across all blocks and menu entries, including detached windows. `MultiInstance` allows a new independent instance on each menu activation. The host validates all explicit declarations for a block before opening its screens; an unspecified menu entry inherits any declaration for the same type. Conflicting declarations or changes to a resolved type policy are rejected, even after its last instance closes. Without a builder declaration, the screen's constructor supplies `ActivationMode` through its protected setter. The property cannot change while hosted. The former `KeepAlive` and `AlwaysCreate` names are now `SingleInstance` and `MultiInstance`, respectively.

`ApplicationScreen.Title` uses the control's `Text` property. Menu navigation supplies the menu text as the initial title unless a specific builder title is provided. Screens can subsequently change `Title`; the tab and detached window update immediately. A block's default screen opens during registration if there is no active screen after its `ExecuteOnLoad` items have run.

Tab widths grow with their titles up to `ApplicationScreenTabControl.MaximumTabWidth`, which defaults to 260 logical pixels. Longer titles display an ellipsis, with the full title available on hover. Tab spacing, close buttons, docking markers, and the width limit scale for the current monitor's DPI.

Selecting a tab restores the previous screen's toolbar items and removes its screen menu, then merges the selected screen's `ToolBar` and registered `MenuItems` into the main form. The same item instances and event handlers are retained. When detached, `ApplicationScreenForm` displays the registered `MenuItems` directly as top-level menus, including File when supplied, regardless of `ShowInApplicationMenuStrip`. It retains the original `ToolBar` control, layout, and handlers instead of copying its items into a replacement strip. The selected docked screen continues to supply the main window's menu and toolbar. Redocking selects the returned tab and merges its items back into the main form.

Detached windows use a compact caption with accessible **Re-dock**, **Minimize**, **Maximize/Restore**, and **Close** icon buttons. The title area supports native dragging and double-click maximize/restore, and the border remains resizable. Close and re-dock preserve the screen's cancellation checks. Only menus and toolbars supplied by the screen occupy space; a content-only screen starts immediately below the caption, without empty bars or a detached tab row.

- Close a tab with its close button, middle-click, or its context menu.
- Undock using the tab context menu, or drag the selected tab away from the tab headers and release.
- Drag tabs along the tab bar to see them move immediately; Escape restores the original position.
- In a detached window, use the caption's **Re-dock** icon, or bring the window's title bar close to the main tab strip. Only the header band accepts drag docking; the screen content panel does not. A highlighted tab-sized **Release to dock** preview and text hint show the proposed position and screen title without changing tab widths, order, or selection. The highlight uses the ExplorerBar palette and follows the visible navigation pane's blue in `BlockMainForm`. Leaving the target or cancelling the drag clears the preview.

The left navigation pane starts at 320 logical pixels wide and scales with the form. Drag its divider to choose a different width. `MaximumNavigationPaneWidth` defaults to 480 logical pixels at 96 DPI; the effective limit also reserves 320 logical pixels for the screen when space permits. Limits scale with DPI and clamp oversized saved widths. Minimized or temporarily undersized layouts defer divider updates until both panels fit; the remembered width remains available for restoration and settings capture. Collapse and restore the entire left navigation pane with the **sidebar icon** in the main toolbar, or **Ctrl+Alt+M**. The icon remains available across screen switches and supplies Show/Hide sidebar tooltips. It does not reserve a separate header or collapsed gutter. `BlockMainForm.NavigationPaneCollapsed` exposes the preference. The pane remembers its width and collapsed state across screen switches. Filled screens temporarily hide navigation without changing that preference.

`MainForm.ScreenHost` exposes `ActivateScreen`, `ShowScreen`, `CloseScreen`, `CloseScreens`, `CanCloseScreens`, `UndockScreen`, `DockScreen`, `OpenScreens` and `Screens`. `ActiveScreen` identifies the selected docked screen; detached windows retain independent chrome. `Screens` also includes cached single-instance screens hidden in SingleView. `ActivateScreen` and menu navigation reuse an existing singleton. Direct `ShowScreen` calls reject a second singleton instance; the caller still owns a rejected instance. A screen belongs to one host at a time.

SingleView retains hidden single-instance screens and disposes multi-instance screens when navigating away, preserving the previous behavior. Explicit close always destroys the instance; opening it again constructs a fresh screen. Switching from MultiView to SingleView closes other open screens and retains the selected tab. Use `ScreenHost.TrySetScreenMode` to handle cancellation without an exception; assigning `ScreenMode` throws if a screen vetoes the change.

The existing `OnHide(ref bool CancelHide)` and `ScreenHidden` event can cancel navigation, close, undock, redock, block removal, and changes to SingleView. Batch close and mode changes check all affected screens before removing any. `ScreenDisplayedFirstTime` runs once per instance; `ScreenDisplayed` runs on each subsequent selection or hosting transition. Closing or disposing the host also disposes hidden and detached instances, with `ScreenDestroyed` raised once per instance.

The reusable host follows `IApplicationScreenHost` → `ApplicationScreenHostBase` → `ApplicationScreenHost`, with `ApplicationScreenHostDecorator` for extensions. `ApplicationScreenTabControl` supplies the tab interactions and `ApplicationScreenForm` hosts detached screens.

The **WinForms Tester** starts in MultiView and uses `HighDpiMode.DpiUnaware` so Windows bitmap-scales its existing screen and dialog layouts. Its **Screen hosting** menu uses `.AsSingleInstance()` for **Settings** and `.AsMultiInstance()` for **New design**, and offers both screen modes. These demos have independent notes, a counter, **Rename tab** actions, a lifecycle log, and a checkbox to exercise cancellation. **Plain screen (no bars)** exercises a screen with no menus or toolbar. See the [Tester instructions](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md). [SystemExpert](../../utils/SystemExpert/README.md) also uses MultiView, with a single instance of each monitoring tool.

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

Distributed under the **MIT NON-AI License**.

See the LICENSE file for full details. More information: [Sphere10 NON-AI-MIT License](https://sphere10.com/legal/NON-AI-MIT)

## 👤 Author

**Herman Schoenfeld** - Software Engineer

## Native helper migration

Reusable native interop now lives in `Sphere10.Framework.Windows`, accessed through `WinAPI` and `Tools.Windows.Win32`. Windows Forms retains control lifecycle, message dispatch, and wrappers such as `Tools.WinForms.CreateCursor`. Sound playback moved to `Sphere10.Framework.Windows.SoundPlayerEx`.

Code using the former low-level APIs should update these references:

- `Tools.WinForms.IconInfo`, `CreateIconIndirect`, and `GetIconInfo` moved to `WinAPI.USER32.ICONINFO`, `WinAPI.USER32.CreateIconIndirect`, and `WinAPI.USER32.GetIconInfo`.
- `DevAgeRichTextBox`'s native constants and protected `CHARFORMAT` moved to `WinAPI.RICHEDIT`; derived controls should pass `WinAPI.RICHEDIT.CHARFORMAT` to `SetCharFormatMessage`.
- Popup/combo-box native constants moved to `WinAPI.USER32`; word extraction helpers moved to `Tools.Windows.Win32`. Native sizing/tab structures now use `WinAPI.USER32.MINMAXINFO` and `WinAPI.COMCTL32.TCITEM`.

These low-level moves require recompilation and the listed source updates for consumers of the moved members. Rich-edit formatting, cursor hotspots, native struct sizes, and hook disposal have regression coverage in `WindowsInteropTests`. The cursor wrapper does not transfer native-handle ownership to `Cursor.Dispose`; direct native consumers should use `CreateCursorHandle` and release their handle explicitly.

Custom closing behavior should override `OnFormClosing(FormClosingEventArgs)`. The framework forms use this supported lifecycle hook. Image attachment selection now returns the selected file or clipboard source instead of always reporting `None`.

Legacy DES stream helpers retain their existing full-length input buffer and zero-filled tail at end of input; short reads are completed before encryption or decryption. The existing DES key, IV, and ciphertext format are preserved.
