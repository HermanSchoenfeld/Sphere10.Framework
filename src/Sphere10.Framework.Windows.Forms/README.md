<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 🖼️ Sphere10.Framework.Windows.Forms

**Windows Forms UI framework and component library** providing data binding controls, database connection panels, validation components, and presentation utilities for desktop applications.

Sphere10.Framework.Windows.Forms enables **rapid Windows desktop application development** with pre-built UI components for database connectivity, data display, user input validation, and common desktop patterns.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Windows.Forms
```

## Chart Library Dependency

The chart controls use
[Sphere10.Framework.Windows.Forms.DataVisualization](../Sphere10.Framework.Windows.Forms.DataVisualization/README.md),
which imports Microsoft's original Windows Forms charting source with the maintained
fork's runtime fixes. Its licenses and third-party notices are retained.
Source builds reference the project within Framework; NuGet builds use its matching
Framework package. A separate charting checkout is not required.

The library builds for .NET 10 and .NET 11 RC 2 with the .NET 11 SDK. Its .NET 10
target is used by Framework, WinForms Tester and System Expert. A .NET 10 SDK
consumer builds only that compatible target. Build and pack the absorbed project
within Framework to check both targets. Runtime code, enums, helpers and resources
are consolidated into one assembly; the legacy designer pipeline was removed.

## Local CHM help

`CHMHelpProvider` opens context help on the containing form's monitor, falling
back to `IUserInterfaceServices.PrimaryUIController` for non-control contexts.
It preserves the CHM's navigation and window styles, uses physical coordinates
under a temporary per-monitor DPI context, and moves an existing viewer when
the requesting form changes monitors. A viewer already on that monitor keeps
its current placement; a moved maximized viewer remains maximized.

## Numeric charts

`GraphChart` is a designer-editable numeric plotting control backed by
`Sphere10.Framework.Windows.Forms.DataVisualization`. `Style` selects line, filled area, vertical columns
or horizontal bars. `BarChart` provides the column-chart defaults, category
labels and margins. The **Line And Bar Charts** screen in WinForms Tester demonstrates
multiple series, missing observations, styles, axes and live snapshot updates.

Supply captured data with `SetSeries(GraphChartSeries[])` on the owning UI
thread. `GraphChartSeries` copies its `GraphChartPoint[]`; its `Points` and the
control's `Series` properties return detached arrays. X coordinates must be
finite; missing or nonfinite Y observations remain gaps. Line and area segments
stop at gaps, while bar charts retain missing category positions. Signed numeric
values are supported. Neither control captures data, schedules refreshes, reads
Windows counters or retains an unbounded history.

Configure `MinimumX`, `MaximumX`, `MinimumY` and `MaximumY` for explicit ranges;
null restores automatic scaling. `XAxisTitle`, `YAxisTitle`, `XValueFormat` and
`YValueFormat` control labels and units. `XLabelFormatter` and `YLabelFormatter`
allow runtime label formatting. Set `ShowLegend`, `ShowAxes`, `ShowVerticalGrid`,
`GridColor` and `PlotBackColor` as appropriate. A series can override the control's
style, and a point's optional `Label` supplies its category caption. `NativeChart`
exposes the owned engine for advanced configuration; the control owns disposal.
`PlotBounds` reports the actual layout, and `TryGetPointAt` returns the nearest
captured category or numeric X coordinate, including a missing observation.
Empty captures display a centered **NO DATA** message, including captures with
only unavailable values and after `Clear`. Finite zero values display normally.
`NoDataText` changes the message; an empty string hides it. `TelemetryChart` and
`BarChart` share the same empty-state presentation.

```csharp
var chart = new BarChart { YAxisTitle = "Temperature (°C)", MinimumY = 0 };
chart.SetSeries(new[] {
	new GraphChartSeries("CPU Cores", new[] {
		new GraphChartPoint(0, 42, "Core 0"),
		new GraphChartPoint(1, 44, "Core 1")
	}, Color.Firebrick)
});
```

Repeated snapshots reuse the native chart control and retained series, batch
collection invalidations, and remove superseded series. Resizing only renders
captured values. Both source and package builds use
`Sphere10.Framework.Windows.Forms.DataVisualization`.

## Telemetry history charts

`TelemetryChart` adapts captured time/value samples to the same `GraphChart`
renderer, retaining the existing telemetry API and designer layout.
Call `SetSamples(TelemetryChartPoint[])` on its UI thread; input membership is
copied and the returned `Points` array is detached. The chart performs no sampling,
background scheduling or native inventory. The consumer owns capture cadence,
history length and statistics. All System Expert CPU, memory, GPU, network,
overview, process-inspection and temperature charts use this control.

`Unit` selects percent, bytes, bytes/second or Celsius scales. Celsius supports
real subzero readings and extends beyond 100 °C when necessary. Invalid and
missing observations break both line and filled-area segments; they do not become
zero readings. Select `PlotStyle = TelemetryChartStyle.Area` for a filled history,
and configure `LineColor`, `FillColor`, `PlotBackColor`, `GridColor` and
`ShowVerticalGrid` to match its host. `Compact` hides scale labels and headers;
`ShowHeader` independently hides the title/current-value header.

Hover displays the nearest captured value and local capture time, using elapsed
time rather than sample index. `TryGetSampleAt` exposes the same lookup in control
coordinates. Data normalization and scale labels are prepared when samples
change; native resizing and paint only project that prepared history. The
control owns its tooltip and disposes it with the surface.

```csharp
var chart = new TelemetryChart {
	Unit = TelemetryChartUnit.Celsius,
	PlotStyle = TelemetryChartStyle.Area,
	LineColor = Color.Firebrick,
	ShowVerticalGrid = true
};
chart.SetSamples(new[] {
	new TelemetryChartPoint(DateTime.UtcNow.AddSeconds(-1), 42),
	new TelemetryChartPoint(DateTime.UtcNow, 43)
});
```

## Scoped window outlines

`WindowHighlightScope` owns a hollow, nonactivating, click-through frame over caller-supplied **physical desktop pixel bounds**. Supply its color and physical-pixel thickness; update `Bounds` to follow a target, then dispose the scope to remove every overlay pixel. Create, update and dispose on the same STA UI thread. The frame uses a layered input-transparent window, leaving the target's device context and foreground activation untouched. The application owns target identity checks, timeout and selection policy; `WindowsWindow.GetPhysicalBounds` supplies verified geometry independently of the querying thread's DPI awareness. Outer bounds include invisible resize borders, as with GetWindowRect. No monitoring or timeout starts in the generic scope.

The frame's HWND is created and updated under a scoped per-monitor V2 awareness
context. Native geometry and placement therefore use the same physical coordinate
space even when a worker and UI thread have different awareness or monitors use
different scales. The scope restores the thread's prior context after every
operation; it does not change the application's overall DPI mode. DPI-change
suggestions do not rescale an already supplied physical rectangle. Never multiply
desktop coordinates by a global DPI ratio: monitor origins can be negative and
scaling can differ between displays.

Keep this frame's managed `Form.TopMost` false. In [.NET 10's Form implementation](https://github.com/dotnet/winforms/blob/v10.0.0/src/System.Windows.Forms/System/Windows/Forms/Form.cs#L2061-L2068), `SetVisibleCore` explicitly focuses managed topmost forms even when `ShowWithoutActivation` is true; its [TopMost setter](https://github.com/dotnet/winforms/blob/v10.0.0/src/System.Windows.Forms/System/Windows/Forms/Form.cs#L1901-L1914) also changes z-order without `SWP_NOACTIVATE`. `WindowHighlightForm` instead specifies native `WS_EX_TOPMOST` at creation alongside `WS_EX_NOACTIVATE`, retains `ShowWithoutActivation`, and rejects keyboard selection. Native tests verify the actual topmost style and zero activation/focus events/messages across first show, resize and reshow. Do not replace that native creation style with the managed property or restore a previously foreground window after an accidental activation.

## Async modal dialogs (.NET 10)

Await modal operations from UI event handlers or methods returning `Task`:

```csharp
private async void EditButton_Click(object sender, EventArgs args) {
	var (accepted, userInput) = await EnterTextDialog.ShowAsync(this, "Edit name", "Name", "Current name");
	if (accepted)
		await DialogEx.ShowAsync(this, SystemIconType.Information, "Saved", userInput, "OK");
}
```

Use `ShowAsync` on `DialogEx`, `ExceptionDialog`, `EnterTextDialog`, `ReviewTextDialog`, `HexEditorDialog`, `PasswordDialog`, `LogonDialog`, and `CrudDialog`; use `GenericEditorForm.ShowFormAsync` and `QuestionDialogSession.AskQuestionAsync`. Password input returns `(DialogResult Result, string Password)`; text input returns `(bool Accepted, string UserInput)`. These helpers dispose the dialogs they create after completion. When calling native `Form.ShowDialogAsync` on your own form, keep it in a `using` scope across the `await`.

`DialogEx` fits its message when shown on the owner's monitor. Short messages retain the designer's small size; longer confirmations grow the form and text viewport, widening when that reduces wrapping. Growth is bounded by the monitor working area and a 900 × 780 logical-pixel window limit, or a smaller caller-supplied `MaximumSize`. Text scrolls vertically once that limit is reached, with command buttons kept outside the scroll area. The existing 5,000-character text limit remains; use `ReviewTextDialog` for full reports.

Custom forms use .NET WinForms directly: `using var dialog = new MyDialog(); var result = await dialog.ShowDialogAsync(this);`. `InvokeAsyncEx` remains a UI dispatch convenience over native `Control.InvokeAsync`, propagating completion, exceptions, and cancellation; it does not implement a modal loop.

`IApplicationDialog` declares the native `ShowDialogAsync` overloads, implemented directly by the inherited `Form` methods. There is no legacy synchronous fallback. The caller retains ownership of an existing dialog. CRUD create, edit, and delete methods return `Task`; SourceGrid deletion uses `DeleteSelectedRowsAsync`. Custom `DialogEx` button behavior overrides `OnProcessButtonAsync`.

`await wizard.Start(owner)` on `IWinFormsWizard<TModel>` also awaits native `Form.ShowDialogAsync`, keeping its owner disabled until completion or cancellation and disposing the wizard dialog afterward.

Wizard screens receive their `Wizard` and `Model` before `Initialize`, including screens added with `InjectScreen`. Navigation calls `Validate` before `OnNext`; commit accepted input in `OnNext` when invalid input must not change the model. Set `UpdateModelOnStateChanged = false` for this pattern. The default `OnPresent` calls `CopyModelToUI`, so input and summary screens can refresh their controls each time they are shown.

Override a branching screen's `Traits` with `WizardStepTraits.AssumeHasNext` when `OnNext` injects its successor. The shared orchestrator displays Next immediately, even for a lone registered screen. If the callback does not supply a real successor, navigation reports an error and keeps the current screen rather than completing. Final screens retain `WizardStepTraits.None`.

WinForms closing and validation events still require cancellation and handled flags to be set before the first `await`. Synchronous grid queries and model construction can only schedule asynchronous error reporting; they cannot await a modal result. `IWindowsFormsEditorService.ShowDialog`, common file/folder/color dialogs, and the synchronous `LiteMainForm.AskYN` contract retain synchronous modal calls. `AskYN` uses a native message box; blocking `ShowDialogAsync` with `.Result` or `.GetAwaiter().GetResult()` on the UI thread would deadlock.

Run the STA regression tests with `dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj` on Windows. They exercise actual forms with a message loop and close their dialogs automatically.

Call `await application.ExitAsync()` to request shutdown and receive whether the main window actually closed. File > Exit and the close button delegate to that same application workflow. It owns one asynchronous native confirmation, the retained screens' close guards, the active screen's block and the selected block's `CanExitAsync(application)` permissions, and existing application-exit observers. Each distinct block is checked once. Repeated requests share the pending decision. A native `FormClosing` observer can still decline normal exit, and another request can retry. Window close continues to hide or minimize forms configured for those actions; explicit application exit closes the window. `application.Exit()` provides the synchronous counterpart, using the same pending decision and native message pumping on the UI thread. `IsExiting` identifies a pending request; `HasExited` identifies completed closure.

`await application.ExitAsync(force: true)` and `application.Exit(force: true)` skip confirmation and permission vetoes, including native `FormClosing` vetoes, while completing normal WinForms closure. Force also upgrades a pending normal request and dismisses its default confirmation dialog. Cancellation during confirmation dismisses that dialog and leaves the application available for another request. Once shutdown is committed, finalizers and resource cleanup finish even if a caller stops waiting. Exit dispatches onto the UI thread even when requested from a worker. Session-ending messages use forced graceful closure. A standalone main form retains its existing closing behavior and confirmation override. Once exit is approved, application and framework shutdown tasks complete before the main window closes; service-provider disposal follows after `Application.Run` returns.

## Review text before saving

`ReviewTextDialog` shows the complete generated text in a read-only, scrollable preview, with **Copy** for the full text and **Save** for choosing a destination. It opens without selecting the preview. Use it for CSV, JSON and other text exports after generating the content with the framework's existing serializers/writers:

```csharp
var savedPath = await ReviewTextDialog.ShowAsync(
	this, "Review CSV Export", "Inspect the export before saving a copy.", csv,
	"export.csv", "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
	cancellationToken: cancellationToken, logger: application.Logger);
```

The helper returns the saved absolute path, or `null` when the user closes the review. Token cancellation disposes the helper-owned dialog, awaits native modal cleanup to restore the owner, then throws `OperationCanceledException`. It awaits native `ShowDialogAsync(owner)`; the filename picker is the existing synchronous Windows common dialog. Construction and preview setup perform no file IO and require no application services, including on the designer surface.

For an existing dialog instance, configure `PreviewText`, `Instructions`, `DefaultFileName`, `Filter`, `OutputEncoding` and optional borrowed `Logger` on the creating UI thread. `SaveAsync(path, cancellationToken)` saves without opening a picker. The default encoding is UTF-8 without a BOM. A complete temporary copy in the destination directory is written before replacing the destination; save failure or cancellation preserves its original content and cleans up the dialog's temporary copy. Saving prevents simultaneous saves and normal closure. Disposing the dialog cancels an outstanding save. The dialog reports save failures through `Logger`, leaving its options and disposal with the caller.

## Byte and hexadecimal editing

`HexEditorControl` is a designer-editable byte editor with offset, sixteen hexadecimal byte columns and an ASCII preview. Its virtual grid displays at most 4 KiB (256 rows) at once; jump by hexadecimal offset or use the page controls. Cell edits require exactly two hex digits. **Insert**, **Delete**, **Paste Hex**, **Copy Hex**, **Find Next** and **Revert** work on the selected contiguous byte range; Ctrl+C, Ctrl+V, Delete and Ctrl+A provide keyboard equivalents. The last blank byte cell can append a byte. Find accepts whitespace-separated hexadecimal pairs and wraps after the end.

`Bytes` copies both incoming and outgoing arrays. Assignment establishes a clean baseline; `HasChanges` compares with that baseline and `RevertChanges()` restores it. Parameterless `BytesChanged` fires only when buffer contents change, including assignment of different contents. Use a loading guard and treat this as a dirty notification; read `Bytes` at an accepted edit or conversion boundary rather than copying the full buffer per cell edit. `ReadOnly` disables mutations while retaining selection, search and copy. Programmatic consumers can call `SelectRange`, `SetByte`, `InsertBytes`, `DeleteBytes`, `ReplaceBytes`, `PasteHex` and `FindBytes`; all control access belongs on the creating UI thread. `CommitEdit()` completes a pending native edit and returns false for invalid input.

`HexEditorDialog` hosts the same control over its own byte copy. `await HexEditorDialog.ShowAsync(owner, title, bytes, readOnly: false, cancellationToken: cancellationToken)` returns a fresh array on **OK**, or null on **Cancel**; it never changes the input array. Token cancellation joins modal cleanup before throwing. The control/dialog edit only memory: the application decides when and where accepted bytes are persisted.

## Application lifecycle and splash

Declare `WithName(...)` before calling `RegisterInitializer` or `RegisterFinalizer`. Both type and factory overloads throw `InvalidOperationException` immediately when the plugin has no name.

`IApplication` exposes `Initialize()` / `InitializeAsync(...)`, `Finalize()` / `FinalizeAsync(...)`, `Exit(...)` / `ExitAsync(...)`, `Logger`, `Disposables`, lifecycle events and `Progress`. Initializing an application runs its tasks once; concurrent calls await the same operation. Merely building a plugin, resolving the application or reading its properties does not run its tasks. Finalization waits for attempted initialization, runs shutdown tasks once and releases application-owned resources; disposing the application also completes that cleanup. Finalization alone does not close the main window: request closure through `ExitAsync`. Task collections remain protected implementation details of `ApplicationBase`.

`WinFormsApplicationPluginBuilder` inherits `RegisterInitializer<T>()` and `RegisterFinalizer<T>()` from the shared plugin builder. Their factory overloads accept `Func<IServiceProvider, T>`. These helpers register transient lifecycle tasks keyed by the built plugin's `Name`; duplicate names are rejected at load, and custom plugin names must remain stable. Different plugins can register the same task type without sharing its instance. The application resolves and caches tasks for its successful `LoadedPlugins` through its own provider. Host registrations using `services.AddInitializer<T>(typeof(IApplication))` and `services.AddFinalizer<T>(typeof(IApplication))` remain supported for application-owned tasks. The combined inventory starts with host-owned registrations in registration order, followed by plugins in `LoadedPlugins` order and each plugin's tasks in declaration order. Initializer priorities apply across this inventory within the existing sequential and parallelizable phases; finalizers execute in combined inventory order. Existing unkeyed framework tasks retain their process startup and shutdown behavior. Use `ConfigureServices` for other plugin dependencies.

Derive synchronous tasks from `SyncApplicationInitializerBase` or `SyncApplicationFinalizerBase` and override only their synchronous method; the base supplies the worker-task async bridge. Derive asynchronous tasks from `AsyncApplicationInitializerBase` or `AsyncApplicationFinalizerBase` and override only their asynchronous method; the base supplies the synchronous bridge. The common `ApplicationInitializerBase` and `ApplicationFinalizerBase` leave both methods abstract. Tasks requiring native UI affinity can implement both contracts there and dispatch to their owning UI thread.

`WinFormsApplication` derives from `AsyncApplicationBase`: its asynchronous readiness hook installs blocks on the owning UI thread before awaiting application tasks. Worker lifecycle requests start through `InvokeAsyncEx`; synchronous native lifecycle and disposal calls pump UI messages while waiting for the same asynchronous operation. This preserves UI continuations without sending block registration to a worker. Pending lifecycle work still follows the shared coalescing, cancellation and disposal guards.

`StartWinFormsApplication` starts the framework with the native message loop running. Module initialization finishes before it constructs the application root; global initializers then run with that exact `IApplication.Logger`, including an override of the registered process logger. Constructing the root does not install blocks or create screens. `Framework.Initialized` still runs before block installation and `application.InitializeAsync()`, which finish before showing the main window. A vetoed block installation can retry without running application tasks. Approved exit awaits application finalization and then runs framework finalizers while the main window remains open with interaction disabled. The window closes after these tasks; the runner waits for the root's exit result before ending the message loop and disposing the service provider. Cleanup still proceeds when a finalizer fails. `Disposables` holds resources owned by the application, independently of the borrowed form and screen host.

Enable the startup and shutdown splash with `.UseSplashScreen()` or `.UseSplashScreen(image)` on the `BuildWinFormsApplication()` chain. It retains its framework events and global information logger, subscribes to process task progress even with a custom application logger, and subscribes to application lifecycle and progress before initialization. After exit confirmation and all closing permissions succeed, a fresh splash appears immediately at full opacity and displays application and framework shutdown tasks while the main window remains behind it. Closing the main window is the last step after those tasks. The splash then fades out during resource cleanup. A failed initialization reuses its still-visible splash for cleanup. Updates from worker tasks queue onto the UI thread. Startup retains its fade-in; shutdown does not fade in. The overload `.UseSplashScreen(image, configure)` applies the same `Action<SplashScreen>` to each startup and shutdown instance. `ProgressOverImage` places the real lifecycle text over the artwork; `ProgressBounds`, `ProgressBackColor`, `ProgressTitleColor` and `ProgressMessageColor` customize that area. An optional `ProgressTextFormatter` can turn task identifiers into display captions while preserving meaningful messages and errors. Existing overloads retain the separate default footer. Supplied artwork remains caller-owned; the splash disposes its resized copy. Application tasks touching native controls must run on the UI thread; reserve parallelizable tasks for work that does not access controls directly.

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

For a configured screen that should start in a modeless detached window, use `await application.ScreenHost.ShowScreenUndockedAsync(screen)` through the shared `IApplicationScreenHost`. The native host also retains `ShowScreenUndocked(screen)` for synchronous native callers. In **MultiView**, a new normal nonpermanent instance opens centered over its main frame without inserting or selecting a main-frame tab, changing the active block, or asking the current main screen to hide. The host retains the exact instance; it remains available in **Screens** and can be docked through the ordinary guarded controls. Showing an existing detached instance activates its window. Showing an existing docked instance this way uses the usual undocking checks. A false result leaves an unretained supplied instance with its caller. Ordinary `ShowScreenAsync(screen)` keeps the normal docked launch behavior.

The generic `IApplicationScreenHost<TBlock, TScreen>` provides typed blocks, screens and lifecycle operations once for every platform. `IWinFormsApplicationScreenHost` adds native docking and existing synchronous presentation APIs. A reference to a screen identifies the same object through neutral, generic and native interfaces. Blocks also use reference identity; distinct native block definitions can have matching names.

The native adapter composes the common logical host and performs native presentation on the UI thread. Its binding dictionary contains native presentation state, including tabs, detached windows and tab image keys. A screen exposes its public native `Host` and owning `Block` through both native and shared contracts; `ApplicationBlock` remains an alias of `Block`. These references are available during activation and destruction hooks, remain attached while a singleton is hidden or a window is undocked, and clear when the instance leaves the host. A `WinFormsApplicationScreen` can also access the owning shared host through protected `ApplicationScreenHost` and close itself with `await ApplicationScreenHost.CloseScreenAsync(this)`.

`OpenScreenAsync(block, definition)` accepts a registered block and an exact definition reference from its `ScreenDefinitions`. Blocks own immutable definitions independently of menus, so a screen can open without a navigation item. A menu item references `ScreenDefinition`, and the block's `DefaultScreenDefinition` references the desired startup definition. Use `RegisterBlockAsync` and `UnregisterBlockAsync` for application membership; guarded removal closes its native controls and updates the same host registry.

Menu items expose their declaring menu through `Parent`, and menus expose their block through `Parent`. Adding and removing items and menus through their containers maintains these references. A screen definition's `Activators` array is a snapshot of the commands attached to menus that reference it; replacing a command's definition or removing the command updates that reverse membership. Application-level menus have no block parent. Rendering merged menus preserves each command's original parent. Screen commands, including toolbar commands, require their declaring menu's block through `item.Parent.Parent`. Ordinary application actions can run without a block or menu parent.

A registered block's `Parent` references the running application. That link follows live host membership and clears when the block is removed or the application adapter is disposed. `Source` records the plugin that successfully contributed the captured block; standalone blocks have no source plugin. Removing and registering a contributed block again preserves its source, independently of its current application membership. Native and shared interfaces expose the same references.

Execute the actual menu item supplied by the registered block, and pass the actual block, definition or screen to host operations. Ownership comes from these references; command dispatch does not search blocks for matching captions or IDs. The typed WinForms contracts specialize `Parent`, `Source`, `Host` and `Block`, with inline bridges to the shared interfaces under `#region Base Interface Bridges`.

## Native screen construction

Native screen tabs prefer `WinFormsApplicationScreen.ScreenImage`, including for
screens launched directly without a menu activator. Set this designer-editable,
borrowed image before the screen is first hosted and keep it alive while hosted;
neither screen nor host disposes the supplied image. If it is null, the host uses
a registered screen activator image, including another definition for the same
screen type. The host owns its image-list entry, keeps each tab's image key across
docking and releases that entry when its screen is removed. Shared definitions
remain platform-neutral.

The default `WinFormsApplicationScreenHost.CreateScreen` selects compatible
`(block, owner)` or `(block)` constructors, then a parameterless constructor.
It does not automatically resolve arbitrary DI constructors. When a screen
needs application-registered dependencies, adapt that construction boundary or
override the host factory; verify the real menu activation path uses the intended
service instances. Do not assume a manually constructed DI screen proves hosted
activation uses DI.

Keep explicit dependency constructors for standalone use and a parameterless
designer constructor for hosted construction. Initialize the static layout, then
return in design mode before resolving providers or binding services. Runtime
screens can use the application's configured provider or an overridden host
factory for shared collectors. Hosted screens use `Block.Parent.Logger` for
application diagnostics. Reusable typed Windows resource APIs live in
[Sphere10.Framework.Windows](../Sphere10.Framework.Windows/README.md#windows-resource-abstractions),
independently of an application checkout. Collectors, sampling policy and display
models live in the application and compose these APIs.

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

`PropertyGridEx.AutoSizeLabelColumn` opts into measuring the visible property
captions rather than using the native default label/value split. Labels adapt to
the current font, DPI, object and viewport, bounded by `MaximumLabelColumnWidth`
(320 logical pixels) and `MaximumLabelColumnRatio` (0.4). Values receive the
remaining width. A native splitter drag or keyboard adjustment takes precedence
until `ResetLabelColumnWidth()` is called. The same control, selected object,
property selection and native view remain in place.

The optional splitter adapter targets .NET 10's internal `PropertyGridView`
`LabelWidth` and `MoveSplitterTo` members. Native minimum/maximum splitter limits
still apply; for example, very wide views reserve at least roughly one ninth for
labels. If a future runtime removes those members, automatic sizing disables
itself and the normal property grid remains available. `Readonly` is retained as
a legacy marker; use property descriptor `ReadOnly` metadata to govern editing.
It no longer clears native selection, an unsupported operation on .NET 10.

**Data Binding**: Enhanced data binding with validation and change notification.

**Wizard Framework**: Multi-step wizard UI pattern implementation.

**Source Grid**: Advanced data grid component for tabular data display.

## 🔧 Core Components

### CrudGrid

See [grids and screen hosts on both platforms](../../docs/guidelines/patterns/grids-and-screen-hosts.md)
for the Blazor equivalents, shared contracts and platform differences.

Set `GridTitle` to a meaningful content name in the designer or constructor.
It updates the caption synchronously on the creating UI thread without requiring
an existing window. When a live handle requires cross-thread dispatch, the
setter queues the caption update and tolerates native-handle teardown.
A nonempty title keeps the header visible when the grid is disabled or the source
offers no create, delete or search controls. Changing the title updates header
visibility without explicitly reading or rebinding the source. Normal Auto paging
may refresh if that layout change alters the viewport's page capacity. An untitled
header collapses only when none of those controls is supported.

Use `CrudCellDisplayType.Image` with an `Image`-typed column and its value getter
for an image cell, such as an executable icon. These cells are read-only and do
not instantiate the text editor. The grid borrows the supplied image; its data
source/screen retains ownership and must keep it alive through presentation.
Extract/load expensive images on a worker and use an owned bounded cache rather
than querying the filesystem during cell painting. The reusable Windows
[FileIconCache](../Sphere10.Framework.Windows/Shell/FileIconCache.cs)
returns borrowed images; keep its lifetime longer than the cells displaying them.

Image columns use `ImageAlignment` from Core's `Sphere10.Framework.ResizeAlignment`,
defaulting to `CenterCenter` at the original image size. Set `ImageResizeMethod`
to `ResizeMethod.AspectFit`, `AspectFitPadded`, `AspectFill` or `Stretch`
to scale within the cell content area. Null uses native-size placement with
SourceGrid's existing clamping when the cell is smaller than the image. Fit leaves the
remaining cell background visible; alignment also controls padded placement and
fill cropping. Ordinary and worker-prepared cells share one view per image column,
retain prepared slots on refresh and keep source image ownership with the caller.
Explicit scaling uses the existing `Image.Resize` API and disposes its temporary
bitmap after painting; it does not create an owned image per cell.

Natural header sizing reserves the actual sort glyph extent before the first
sort, so a later ascending or descending indicator cannot cover the measured
caption. Sorting keeps the established widths; explicit user widths still take
precedence. SourceGrid's `SortIndicator.ReserveSpaceWhenUnsorted` defaults to true.

`CrudGrid` hides the create and delete buttons when their respective capabilities are unavailable. Delete is also hidden unless a visible row is selected. Toolbar and paging controls size to their contents, including scaled fonts and DPI.

Set `AllowCellEditing = true` to edit columns with `CanEditCell = true` and a `SetPropertyValue` binding; `CanUpdate` must also be enabled. Changing the editing setting updates existing cells immediately. `LeftClickToDeselect` toggles a row on successive single clicks, including Ctrl-clicks. When both settings are enabled, double-click an editable cell or press **F2** to edit while keeping its row selected; checkbox values also change only through that explicit edit gesture. These selection modes also support `SelectOnMouseUp`.

Read-only text cells retain type conversion through SourceGrid's nonvisual
`EditorBase`, without constructing a WinForms editor control or attaching a
write-through controller for every displayed value. Enabling effective editing
later creates the normal editor for eligible columns without rereading the source;
revoking it cancels drafts and detaches persistence callbacks. Empty cells require
no editor. This keeps complete read-only inventories lighter during refresh.

For frequently refreshed read-only tables, `AutoSizeRowsOnRefresh = false` can
retain measured heights of ordinary single-line text/image rows. Initial binding,
new rows, changed image height, multiline/wrapped content and custom/editable cells
still measure. Source, bindings, column widths, fonts and DPI changes invalidate
the retained geometry. The default is true, preserving normal per-refresh sizing.

`PrepareReadOnlyCellsInBackground = true` opts a worker-safe read-only Text/Image
table into background source reads, result enumeration, `CellHasValue`/value
delegates and display conversion. The source, row objects, delegates, converters
and borrowed images must support that execution context; the default remains
false for UI-affine consumers. Keep column configuration stable during a refresh,
or replace `GridBindings` to supersede an in-flight result. Editable columns and
other display types keep their existing owner-thread path. Prepared value records
contain the original typed values and cached display strings, so sizing and
painting do not reevaluate model delegates or converters. Only plain compatible
prepared cells reuse their complete native cell/model/editor graph, replacing
snapshot values without edit callbacks. Row and reverse-selection lookups are
also prepared on the worker. First/new row slots create their detached native
cell graphs there too, before attachment on the owner thread. Native controls, editing, selection and
layout remain on the owner thread. Superseded/disposed preparations are discarded
before publishing a page. Targeted row refresh follows the same preparation
contract. Image ownership stays with the caller, including while preparation is
in flight. An explicit programmatic cell-value change outside the data source
uses the normal converter until the next prepared refresh.

Prepared grids also divide native first sizing and later row remeasurement into
short owner-thread slices. A WinForms timer admits each subsequent slice, giving
pending input and painting a real message-loop opportunity. Each slice completes
its redraw/layout scopes before yielding; graphics and editor controls stay on
the owner thread. SourceGrid's measurement order, intrinsic widths/heights,
visibility and auto-size flags remain in use. Query, permission, disposal and
font/DPI/geometry changes are checked between slices, and explicit column widths
chosen during sizing survive completion. The initial loading surface stays in
place until the accepted page has finished sizing. Other grids retain their
existing synchronous sizing behavior.

When a resize changes native sizing geometry during a prepared binding, sizing
retries over the already committed cells. It does not reread, reconvert or rebind
the same snapshot. Actual query or binding changes still supersede the result.

`GridTitleAlignment` uses `System.Drawing.ContentAlignment` and defaults to
`MiddleCenter` for compatibility; choose `MiddleLeft` for a left-aligned table title.
The header collapses hidden Create/Delete button slots, so a read-only title
starts at the normal header inset rather than reserving space for unavailable actions.

The footer keeps **Total** on the left and the page-mode dropdown, **Page Size** and navigator aligned on the right. Narrow hosts wrap complete groups without clipping their controls. `PageMode` selects a `CrudGridPageMode`:

| Mode | Behavior |
|---|---|
| **Paged** | Manual **Page Size** label/editor and page navigator. Sizes range from **1 to 9999**, in steps of one. |
| **Auto Paged** (`Auto`) | Page navigator remains available; the manual label/editor are hidden. The actual ordered row heights, header and scrollbar-adjusted viewport determine how many complete rows fit, with a minimum of one. Resize, font and DPI changes recalculate capacity. Taller individual rows do not inflate the budget for every ordinary row. |
| **Single Paged** (`Single`) | All matching rows in one vertically scrollable page. The manual size and page navigator are hidden; the source receives page zero and `int.MaxValue` page length. Search and sorting still apply. |

Switching modes starts at page zero and retains the remembered manual size for returning to **Paged**. `AutoPageSize` remains a compatibility alias: true selects **Auto Paged**, false selects **Paged**. The enum values and stored preferences remain unchanged. Sources without `CanPage` retain their existing complete-result behavior and hide the paging footer. Navigation and sorting are remembered for **Paged** and **Auto Paged**; **Single Paged** always uses page zero.

Auto paging settles both larger and smaller capacities after native measurement.
Measured viewport/header/row dimensions already use the window's device pixels;
only an unmeasured fallback is DPI-scaled. A bounded fitting result prevents
repeated probes when a taller next row cannot fit. Short final pages retain
their remaining rows without an attempt to fill from nonexistent data.

For rapid live updates, set `SuppressLoadingIndicator = true`. Refresh begins immediately, without the normal stampede delay, loading animation or temporary grid disabling. It retains the logical busy state, joins an active refresh task and discards superseded query results. The default remains false for ordinary data loading. The WinForms tester's CRUD screen exposes the option for its grid and dropdown.

While a suppressed refresh awaits data, selection, deselection, paging and row
context menus continue against the last committed page. A right click first
selects its actual row. Internal native publication gates selection callbacks
until the replacement page and its lookups are consistent; the newest user
selection is then retained where possible. This does not permit inline editing
or CRUD mutations during a refresh, and replacing the source or columns blocks
interaction with the superseded view. Ordinary loading and initial binding keep
their normal input gate.

At runtime a newly created grid shows its loading surface immediately. First binding and later ordinary refreshes use the same full-grid overlay, with painted spokes scaled to the current viewport and resized with its host. Its search, CRUD buttons, table and footer remain covered until the first completed `RefreshGrid()` after `SetDataSource(...)`; a slow capability read or initial collection cannot expose designer placeholders. Nested loading operations retain the overlay until the last operation completes. `SuppressLoadingIndicator` suppresses later routine updates, while the first binding retains this loading surface. The Visual Studio designer still displays the editable normal layout.

After a selected-row operation, await `RefreshEntityAsync(entity)` or `RefreshSelectedEntityAsync()` to reread only that visible row through the source's `RefreshRangeAsync`. The source can replace the array entry with a fresh instance, or null when it disappeared. The grid retains page/sort settings, column widths and unaffected rows, and transfers selection to the replacement. A false result means the row is no longer visible or a source/query change superseded it. This requires a source that implements entity refresh; the standard read-only `SnapshotDataSource<T>` deliberately does not. A caller-owned snapshot source can supply that behavior without adding application collection policy to the grid. Targeted and full-page refreshes share scoped asynchronous admission.

Click a sortable column header to cycle **Ascending → Descending → None**, then ascending again. **None** clears the header arrow and requests the source's natural order with `SortDirection.None`; changing to another column starts ascending. Sorting preserves the current page and established column widths, and an explicitly cleared sort persists across sessions. `SnapshotDataSource<T>` honors explicit **None** even when a default sort is configured; the initial null column with **Ascending** still requests that configured default.

`await grid.ReadFilteredEntitiesAsync(cancellationToken)` captures the current
source, search and sort on the owning UI thread, then reads and materializes all
matching rows on a worker, across pages. It does not rebind, disable the grid,
show an indicator or change page/selection. The returned `Array` retains the
source's declared item type, including an empty result, so exporters can retain
typed CSV headers. It queries the bound source rather than calling a separate
collector; snapshot sources therefore reuse their published inventory.

Columns take their natural widths from the first nonempty result for a binding set, then keep those widths across paging, searching and refresh. Longer values on a later page do not resize the table. Explicit user resizing takes precedence and survives subsequent reads. Columns marked `ExpandsToFit` share spare viewport width when the viewport changes; an explicitly resized column keeps its chosen width. Assigning a new binding set starts a fresh natural-width calculation. Right mouse-down selects the clicked data row before a context menu opens in either left-click selection mode, without starting an editor or toggling checkbox values; headers and empty space do not open an entity menu.

`PersistSettings` defaults to true. Each logical grid uses a typed `CrudGridSettings` object through `UserSettings` to remember its page mode, manual size, page number and source sort name/direction across sessions. Older settings without `PageMode` migrate through their retained `AutoPageSize` flag. The default settings ID uses its concrete screen/control type and named child path, so docking and caption changes do not change it. Set `SettingsKey` for an explicit stable identity when names are dynamic or ambiguous; set `PersistSettings = false` for transient grids. Reference pickers already opt out. Settings restore after bindings and the provider are available, validate obsolete values, and preserve the consumer's configured defaults on first use. Accepted preference changes save; viewport resizing and source refreshes do not write settings. See [per-user UI settings](../../docs/guidelines/patterns/user-ui-settings.md).

### Snapshot collection and binding

For read-only changing collections, use the nonvisual
[CrudGridSnapshotController<T>](Crud/CrudGridSnapshotController.cs) behind a
designer-owned grid. It accepts an async collector, stable row identity and
optional property-value formatter; an overload accepts an existing core
`SnapshotDataSource<T>`. It binds once, serializes refreshes, queries the grid's
current page and maps the latest selected identity to its replacement in the same
native publication as the new cells, before geometry work can yield. Snapshot
publication invalidates an older in-flight grid query so stale cells cannot be
committed against a newer selected model. Collection and worker preparation leave
the previous committed page available for input.
Typed `Rows`, `Count`, `SetDefaultSort`, `RefreshAsync` and `CaptureSnapshot`
support concrete nongeneric screen surfaces. CSV/JSON export belongs to the caller.
Create/configure/refresh it on the grid's UI thread with its dispatcher context;
the collector determines its own worker execution. Late results are ignored
after grid disposal. See [controller tests](../../tests/Sphere10.Framework.Windows.Forms.Tests/CrudGridSnapshotControllerTests.cs).

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

Set `ExpandsToFit = true` on the column that should occupy spare width, such as the Name column in a reference picker. Other columns keep their initial measured widths. Multiple configured columns share extra space; none stretch implicitly when no column is configured. Resizing redistributes spare width from remembered minimum widths. Explicitly resized columns retain the chosen width and leave automatic stretching. Pages, search and refresh do not remeasure columns from their new values. Existing columns wider than the viewport use horizontal scrolling. Automatic page sizing continues to fit complete rows in the available grid height.

`CrudGrid.ReferenceBindings` and `DefaultCrudEntityEditor.ReferenceBindings` also accept bindings keyed by a property path, such as `"Manager"` or `"Address.Contact"`. The grid supplies its own data source automatically for properties whose type matches the edited entity type and which have no custom editor or converter. Explicit bindings take precedence. This avoids expanding recursive entity relationships while ordinary nested values such as Address remain editable.

`CrudComboBox` uses the same column configuration. Its `MaximumDropDownSize` and optional `SetCrudParameters(size: ...)` configure the popup size, capped to the available screen space. Use `ExpandsToFit` in its grid bindings to choose which columns fill spare width.

### Default entity editor

The default property-grid editor remains enabled for read-only inspection, so users can select, expand and copy values. Its session-local descriptors mark all fields read-only and suppress mutating custom/collection editors when create/update capability is absent; model metadata is not changed globally. Writable editing sessions retain their existing nested/custom-editor behavior.

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

### LogOutputSelectorControl

`LogOutputSelectorControl` is a public, designer-editable control in `Sphere10.Framework.Windows.Forms`. It presents a compact **Log Output** checkbox and **...** (Configure Log Output) button, and can be used without depending on SystemExpert.

Checking it for the first time opens the WinForms wizard: choose **File**, **Application**, **Standard Output** or **Standard Error**, then configure the applicable destination, levels, timestamping and **Remember After Close** option. File output supports append/rolling files, encoding, retained file count and maximum encoded bytes. The checkbox turns on only after Finish validates and opens the selected output. After acceptance, unchecking stops output and checking again reuses those options. **...** (Configure Log Output) opens `LogOutputConfigurationDialog`, showing the current details with a **Change** button to launch the wizard. Cancelling a change preserves the current output; a failed Finish attempts to recover the old output when reopening it is possible.

```csharp
using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;

// Supply the owning application's borrowed logger for the Application option.
logOutputSelector.ApplicationLogger = application.Logger;
// Bind LoggingStarting/LoggingFailed before restoring a possibly enabled output.
logOutputSelector.RestoreSettings();
await logOutputSelector.ConfigureAsync(this);

// Resolve the current logger for each write; inactive output is a NoOpLogger.
logOutputSelector.Logger.Info("Monitoring started"); // Also safe from workers.

logOutputSelector.StopLogging();
```

`Logger` returns a `NoOpLogger` while off and changes to the accepted session when on. Retrieve it per write or supply a callback to a long-lived service; a logger captured while off remains inactive. Closed sessions safely ignore late calls. File writers/sinks are owned and writes join before disposal. Application loggers and console writers are borrowed and retain their existing owners/options; their callbacks run outside the session scope and their owner controls in-flight completion. Reading the property, construction and design-mode loading perform no file IO.

`Configuration` is a defensive `LogOutputConfiguration` snapshot. Configure/start/stop on the creating UI thread. `StartLogging()` accepts its already configured values for programmatic consumers; `ConfigureAsync(owner)` supplies the wizard and `ShowConfigurationAsync(owner)` supplies the details dialog. `LoggingStarting(configuration)` can veto before IO, including during restoration. `LoggingStateChanged`, `LoggingFailed`, `HasConfiguration`, `IsLogging` and `LastError` expose state and failures. Do not separately dispose the selected session or register it as another global owner.

With **Remember After Close**, `LogOutputSelectorSettings` persists the accepted configuration and enabled choice through `UserSettings`. Call `RestoreSettings()` explicitly after the application logger and consumer hooks are ready. Construction/handle creation never read preferences or open output. The method returns false while a stable owner/provider is unavailable and can then be retried; completed restoration is idempotent. `SettingsKey` can supply an explicit stable ID; otherwise the owning screen/control type and named child path identify this selector independently of captions or docking. Set the key before restoring or accepting output. Restoration and unchanged disposal do not write preferences, and disposal closes a remembered active sink without changing its saved enabled choice. Turning Remember off deletes only that selector's stored preferences, including its cached record. Optional preference failures appear in `LastSettingsError`; output remains usable and failed accepted saves retry on disposal. See [per-user UI settings](../../docs/guidelines/patterns/user-ui-settings.md#reusable-logging-preferences).

SystemExpert's watcher passes `eventLoggerProvider: () => logOutputSelector.Logger`, so output changes take effect without restarting monitoring. Every accepted event is independent of display filters, pause and retention; file paths inside the watched tree are rejected before IO. WinFormsTester's **Logging** screen demonstrates the selector alongside a TextBoxLogger preview. See the [reusable controls catalog](../../docs/guidelines/patterns/winforms-controls.md).

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

### Application banners

[ApplicationBanner](Application/Controls/ApplicationBanner.cs) is the concrete
designer surface for product title, version and company branding. Extend it with
a product-owned subclass and designer pair, then register that subclass as the
transient `ApplicationBanner` service. Framework registers the stock transient
only when no application registration exists.

Place [ApplicationBannerHost](Application/Controls/ApplicationBannerHost.cs) in a
form's designer to opt in to that registration. It retains a static stock preview
on the design surface and resolves `Banner` on runtime load or first access.
Each host requires its own transient instance and owns that child through its
control collection, disposing it with the host.

The banner owns its rendered icon bitmaps, releases replaced images during
resizing and disposes its final image with the control. The icon supplied by
`IApplicationIconProvider` remains borrowed from that provider.

`UseIcon(icon)` selects that application's borrowed icon; retain it for the
runner's complete lifetime. `AddMainForm` preserves an explicitly registered
`IApplicationIconProvider`. Its default provider uses the configured icon, then
the main form's `ApplicationIcon` contract. The native runner applies the
resolved icon to the main window before initialization, and the standard About
dialog uses the same provider for its title bar and banner. Consumers must not
dispose the shared icon or allocate undisposed per-read copies. The banner uses
Drawing's aspect-fit padded resize, centered in both dimensions, and About uses
the full product version consistently. `ShowAboutBox` resolves its dialog from a
per-call DI scope retained through awaited modal completion. Closing disposes a
transient or scoped dialog and its rendered banner images; a custom singleton
remains owned by the application container and can be shown again.

## 🛠️ Tools.WinForms Namespace

```csharp
using Tools;

// Create custom cursor from bitmap
Cursor cursor = WinForms.CreateCursor(bitmap, hotspotX, hotspotY);

// Load cursor from raw bytes
Cursor rawCursor = WinForms.LoadRawCursor(cursorBytes);
```

## 📋 Available Components

Use the central [reusable WinForms controls catalog](../../docs/guidelines/patterns/winforms-controls.md)
for the full categorized selection guide, including provider-specific database
editors, registration requirements, ownership and current limitations.

### Controls
| Control | Description |
|---------|-------------|
| `CheckedGroupBox` | Group box with checkbox header |
| `ApplicationBanner` | Designer-editable product title, version and company branding |
| `ApplicationBannerHost` | Stock designer preview with a registered transient banner at runtime |
| `PathSelectorControl` | File/folder path selection |
| `LogOutputSelectorControl` | Optional wizard-configured file/application/stdout/stderr output, configuration details dialog and remembered preferences |
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

Other forms can keep `Tools.WinForms.AutoPersistWindowSettings(Form, SettingsID)` in a `using` scope for the window's lifetime to save once on accepted closure, or use `CaptureWindowSettings` and `RestoreWindowSettings` with their own `FormWindowSettings` instance. See the [user UI settings practice](../../docs/guidelines/patterns/user-ui-settings.md) for settings classes, defaults, lifecycle and storage conventions.

### Shared presentation contracts

The portable contracts and implementation state live in `Sphere10.Framework.Application.UI`. Native adapters use explicit `WinForms` names, so both namespaces can be imported together without aliases. `ScreenActivationMode` comes directly from the shared namespace; there is no separate native enum. WinForms `IWinFormsApplicationBlock`, `IWinFormsApplicationMenu`, `IWinFormsApplicationMenuItem` and `IWinFormsScreenMenuItem` extend their shared counterparts with typed collections and native presentation options. Custom blocks supply `ScreenDefinitions` and `DefaultScreenDefinition`; custom screen items supply a canonical `ScreenDefinition` reference. Shared metadata has no string IDs. `WinFormsApplicationBlock` and `WinFormsApplicationMenu` derive from abstract `ApplicationBlockBase` and `ApplicationMenuBase<TItem>`. `WinFormsApplicationMenuItemBase` and `WinFormsLinkMenuItemBase` are abstract native intermediates above the concrete screen and action items. Native APIs retain images, menu parents, array views and disposal ownership. The fluent native builders inherit common metadata, screen policies and child-builder composition from typed shared bases, then create WinForms products on the first `Build()`. Plugin `ConfigureBlock`, block `ConfigureMenu`, menu `ConfigureItem`, their corresponding `Add` methods and common setters live in `Sphere10.Framework.Application.UI`; native adapters retain images, menu/explorer/toolbar flags and control factories. Subsequent builds return the same product and incorporate additional canonical definitions. Later name, position, default and menu configuration updates the existing native product, preserving one owner for menus, items and images.

`IWinFormsApplicationScreen` extends the shared `IApplicationScreen` contract with synchronous native hooks. `WinFormsApplicationScreen` maps those hooks to its existing display and cancelable hide events on the calling UI thread. Shared `CanDeactivateAsync` invokes the native hide guard; `OnDeactivatedAsync` does not emit the hide notification again. Cancellation is checked before invoking a native hook. The native presentation hooks remain synchronous; the host also exposes the shared asynchronous operations for controls, tabs, docking and screen disposal.

Screen definitions carry activation modes for both UI hosts. The host resolves declarations and captures a constructor's mode in a definition when no mode was declared, then validates subsequent instances against that metadata. The native adapter uses the shared `ScreenActivationMode` enum directly; an unspecified menu mode still leaves the initial choice to the screen constructor. Help metadata remains available through the shared screen contract.

### Running application and shared wizards

`IWinFormsApplication : Sphere10.Framework.Application.UI.IApplication` exposes the running `BlockMainForm` through live blocks, loaded startup plugins, its screen host, retained unsaved state and change notifications. The host owns active block and screen selection. For `BlockMainForm` and its subclasses, `AddMainForm` registers the native and shared interfaces to the same adapter; framework startup initializes its configured blocks. The adapter borrows the form and screen host rather than creating a second owner. `SetMenus` and `SetToolBarItems` accept native command definitions and update the actual application menu and toolbar using the form's existing bindings and execution path.

`WinFormsWizardBuilder<TModel>`, `WinFormsActionWizard<TModel>`, `IWinFormsWizard<TModel>`, `WinFormsWizardScreen<TModel>` and `WinFormsWizardDialog<TModel>` replace the unprefixed native wizard names. Navigation, step membership, callbacks and completion policy use the shared `Application.UI` wizard workflow; native adapters retain control initialization, validation, presentation, sizing and disposal. Dynamically removed screens remain owned by their wizard and are disposed with it. `WizardResult`, `WizardStepUpdateType` and `ScreenMode` now come directly from the shared UI namespace.

### Multiple screens and detachable tabs

The [cross-platform host guide](../../docs/guidelines/patterns/grids-and-screen-hosts.md#application-screen-hosts)
maps these operations to Blazor, including startup preferences, retained sessions,
close guards and the native detached-window boundary.

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

Tab widths grow with their titles up to `WinFormsApplicationScreenTabControl.MaximumTabWidth`, which defaults to 320 logical pixels. Native caption measurements reserve room for the owner-drawn icon, its spacing and the close action, including when the image list retains its original pixel size on a higher-DPI monitor. Longer titles display an ellipsis, with the full title available on hover. Tab spacing, close buttons, docking markers, and the width limit scale for the current monitor's DPI.

Selecting a tab restores the previous screen's toolbar items and removes its screen menu, then merges the selected screen's `ToolBar` and registered `MenuItems` into the main form. The same item instances and event handlers are retained. When detached, `WinFormsApplicationScreenForm` displays the registered `MenuItems` directly as top-level menus, including File when supplied, regardless of `ShowInApplicationMenuStrip`. It retains the original `ToolBar` control, layout, and handlers instead of copying its items into a replacement strip. The selected docked screen continues to supply the main window's menu and toolbar. Redocking selects the returned tab and merges its items back into the main form.

Detached windows use a compact caption with accessible **Re-dock**, **Minimize**, **Maximize/Restore**, and **Close** icon buttons. The title area supports native dragging and double-click maximize/restore, and the border remains resizable. Close and re-dock preserve the screen's cancellation checks. Only menus and toolbars supplied by the screen occupy space; a content-only screen starts immediately below the caption, without empty bars or a detached tab row.

- Close a tab with its close button, middle-click, or its context menu.
- Undock using the tab context menu, or drag the selected tab away from the tab headers and release.
- Drag tabs along the tab bar to see them move immediately; Escape restores the original position.
- In a detached window, use the caption's **Re-dock** icon, or bring the window's title bar close to the main tab strip. Only the header band accepts drag docking; the screen content panel does not. A highlighted tab-sized **Release to dock** preview and text hint show the proposed position and screen title without changing tab widths, order, or selection. The highlight uses the ExplorerBar palette and follows the visible navigation pane's blue in `BlockMainForm`. Leaving the target or cancelling the drag clears the preview.

The left navigation pane starts at 320 logical pixels wide and scales with the form. Drag its divider to choose a different width. `MaximumNavigationPaneWidth` defaults to 480 logical pixels at 96 DPI; the effective limit also reserves 320 logical pixels for the screen when space permits. Limits scale with DPI and clamp oversized saved widths. Minimized or temporarily undersized layouts defer divider updates until both panels fit; the remembered width remains available for restoration and settings capture. Collapse and restore the entire left navigation pane with the **sidebar icon** in the main toolbar, or **Ctrl+Alt+M**. The icon remains available across screen switches and supplies Show/Hide sidebar tooltips. It does not reserve a separate header or collapsed gutter. `BlockMainForm.NavigationPaneCollapsed` exposes the preference. The pane remembers its width and collapsed state across screen switches. Filled screens temporarily hide navigation without changing that preference.

Menu rows wrap captions to the available navigation width. Narrowing the pane increases row and group heights; widening it removes unnecessary wrapped lines. The navigation container keeps its menu viewport sized to the pane, including applications with one block and no block-selector buttons. Following groups move below their contents, with vertical scrolling when necessary.

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

The tab context menu's **Make Startup Screen** saves an
`ApplicationStartupScreenSettings` preference through the application's
`UserSettings`. The host loads it asynchronously before block registration and
prefers a matching registered screen over the configured default, while still
creating permanent screens. Identity uses plugin/block names, full screen type
and simple assembly name; captions, assembly versions and live session handles
are not persisted. Missing, invalid or non-restorable definitions fall back to
the configured default without overwriting the preference. Override
`WinFormsApplicationScreen.CanRestoreAtStartup` to false for screens that require
session arguments; their context action remains visible with an explanation.

`BlockMainForm.RegisterBlock` initializes new permanent screens without replacing an ordinary active screen. If an active Empty screen refuses the transition, the new block remains unregistered and undisposed so the caller can retry after clearing the guard. For a standalone host, `InitializeScreens(blocks)` returns whether initialization was accepted. `UnregisterScreenTypes(block)` removes its definitions and owned screens after all close guards agree. The form's existing `UnregisterBlock` uses that path, allowing administrative removal of permanent screens and disposal of hidden Empty instances. `CanCloseScreens(Screens)` remains the application-exit guard: permanent lifetime does not prevent shutdown.

The existing `OnHide(ref bool CancelHide)` and `ScreenHidden` event can cancel navigation, close, undock, redock, block removal, and changes to SingleView. Batch close and mode changes check all affected screens before removing any. `ScreenDisplayedFirstTime` runs once per instance; `ScreenDisplayed` runs on each subsequent selection or hosting transition. Closing or disposing the host also disposes hidden and detached instances, with `ScreenDestroyed` raised once per instance.

The reusable host follows `IWinFormsApplicationScreenHost` → `WinFormsApplicationScreenHostBase` → `WinFormsApplicationScreenHost`, with `WinFormsApplicationScreenHostDecorator` for extensions. `WinFormsApplicationScreenTabControl` supplies the tab interactions and `WinFormsApplicationScreenForm` hosts detached screens.

`MainForm` and `BlockMainForm` provide a **Screens** menu listing the exact open instances across blocks, including detached windows. Duplicate titles receive an occurrence number; detached entries are marked **Undocked**. Selecting an entry navigates to that instance without opening another or docking its window. The menu and tab context menus expose **Close All** and **Close All Others**. `CloseAllScreensAsync` and `CloseAllOtherScreensAsync(keepScreen)` close docked and detached instances through the existing atomic guard checks; permanent screens remain open. Hidden retained singletons are not open menu entries, and the ordinary exit guard still includes them.

The **WinForms Tester** loads two plugins through `AddWinFormsApplicationPlugin`: `WinFormsDemoPlugin.Configure` contributes Workspace and Notes, while `WinFormsComponentDemoPlugin.Configure` contributes Controls, Data and collections, Integration, and Utilities. Each plugin registers its own initialization and finalization tasks from the tester's `Lifecycle` folder, alongside the existing framework startup tasks. It starts in MultiView and uses `HighDpiMode.DpiUnaware` so Windows bitmap-scales its existing screen and dialog layouts. **Workspace > Screen hosting** explicitly declares **Settings** as single-instance; **New design** and ordinary component screens use the multi-instance default. Both screen modes are available, and the hidden Empty screen belongs to Workspace. These demos have independent notes, a counter, **Rename tab** actions, a lifecycle log, and a checkbox to exercise cancellation. **Plain screen (no bars)** exercises a screen with no menus or toolbar. See the [Tester instructions](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md). SystemExpert is an independent diagnostic application consuming these hosts and the Windows diagnostics library; the framework checkout does not require its source or tests.

`Directory.Build.props` sets `ForceDesignerDPIUnaware=true` for the Visual Studio WinForms designer across the repository. This designer setting is separate from runtime DPI awareness. The library leaves runtime DPI policy to the application; Tester explicitly selects `HighDpiMode.DpiUnaware` before creating controls to preserve Windows bitmap scaling.

## Global keyboard shortcuts

`GlobalHotKey` implements `IGlobalHotKey` using a private message-only window and
the Windows hotkey API, independently of low-level keyboard hooks. Create,
register, unregister and dispose it on the same UI thread. Construction performs
no registration; `TryRegister(Keys.Control | Keys.Alt | Keys.A, out error)` begins
receiving `Pressed` events through the native message loop. `Keys.None` disables
the shortcut. Windows auto-repeat is suppressed, so holding the key does not
toggle repeatedly.

An unavailable shortcut returns an error while retaining its previous active
registration. Modifier-only keys, F12 and unsupported combinations are rejected.
Disposal unregisters the shortcut and destroys the owned window. The consuming
application owns its activation policy, settings, user feedback and subscription
lifetime; keep registration alive when temporarily disabling the operation it
controls. AutoMouse uses this component for its configurable toggle shortcut.

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

`Control.EnterRenderingScope()` defers layout and native redraw across the control tree. Nested scopes, added controls, newly created handles and newly shown controls share suppression. The last scope flushes parent/child layout and performs one redraw; disposed and intentionally hidden controls remain so. Controls moved into another window receive their final full-tree invalidation at their current surviving root, including when the original root was disposed. A subtree still held by another rendering scope waits for that owner to release it. `EnterRenderingScope(repaintImmediately: false)` restores layout and native visibility synchronously but queues painting for the next normal native paint cycle, appropriate inside native input/selection callbacks. The existing no-argument overload retains immediate painting. Dispose scopes on the creating UI thread; never keep one across an await or BeginInvoke. Screen-host transitions use this scope around the form and publish one completed selection and command update.

Native redraw suppression temporarily removes `WS_VISIBLE`; old pixels can remain while Windows routes mouse input to another window. An enabled control and a responsive managed message loop do not establish that its native input target remains available. Live CrudGrid publication and incremental native sizing therefore use SourceGrid's existing double buffering and managed layout suspension, without `WM_SETREDRAW`. A single-row publication explicitly invalidates the changed view, including when no row is selected. Native input regressions sample cached HWND visibility and hit-test routing from a worker during publication, rather than calling control handlers directly or observing only UI timer ticks.

After native resizing and redraw restoration, the scope compares a top-aligned `TabControl`'s native `TCM_ADJUSTRECT` display area with WinForms' cached display rectangle. During toolbar restoration and undocking, managed `OnResize` can run before the native `WM_SIZE` recalculates wrapped header rows; the retained page then covers those headers. A mismatch reapplies the selected page's existing nonvirtual image metadata, whose parent update clears that cache. Its image-key/index mode, captions, custom text setters, native handles, selected page and selection events remain unchanged. This synchronization finishes before the existing final invalidation; it does not resize or recreate controls or select another page.

SourceGrid controller dispatch uses a cached membership snapshot. Controllers may add or remove controllers while handling a mouse, keyboard, focus or editing callback; the current event retains its admitted order and nested/subsequent events use the updated membership. A new snapshot is allocated only when membership changes.

Restoring redraw preserves each control's own requested visibility, rather than its effective `Visible` value inherited from a hidden parent. The base `Control.Visible` designer descriptor supplies that state through `ShouldSerializeValue`, even when a derived control declares a different default. Native regression checks must inspect actual window visibility after hiding and reactivating a retained view: healthy managed bounds alone do not prove its dropdowns and search controls are painted.
