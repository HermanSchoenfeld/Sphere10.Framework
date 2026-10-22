# Reusable Windows Forms controls

Read this catalog before building a Windows desktop screen, selector, editor,
connection dialog or diagnostic tool. Find the existing component first, inspect
its current source and real consumer, and extend the owning component when a
capability is missing. Use the [framework reuse practice](framework-reuse.md).

This is the central selection catalog for the public visual families in
`Sphere10.Framework.Windows.Forms` and its database UI packages. The separate
`Sphere10.Framework.Windows` package supplies native services and facades rather
than WinForms controls. Most controls below use the root
`Sphere10.Framework.Windows.Forms` namespace; exceptions are identified below.
Source links identify the actual API, including older components whose names
promise more than their current implementation delivers.

For implementation rules, follow the [designer practice](winforms-designer.md),
[user UI settings](user-ui-settings.md), [synchronization](synchronization.md),
[logging](logging.md) and [Windows API facades](windows-interop.md). This catalog
selects components; those topics own their respective rule bodies. All UI access
belongs on the creating STA UI thread. The framework's worker-safe logger output
is a specifically documented exception, not permission to read controls from a
worker.

## Choose a component

| Requirement | Existing entry point |
|---|---|
| Choose a file, save destination or directory | `PathSelectorControl` |
| Choose optional file/application/screen/stdout/stderr logging, options and rolling output | `LogOutputSelectorControl` |
| Choose a database engine and edit its connection | `DatabaseConnectionPanel` or `DatabaseConnectionBar`, with provider modules registered |
| Edit a specific SQL Server, SQLite or Firebird connection | The matching concrete provider panel/bar |
| Display/search/page/edit entity records | `CrudGrid` with `IDataSource<T>` |
| Choose an existing related record | `CrudReferenceBinding<T>` and its `CrudReferencePicker` |
| Show a grid in a dropdown | `CrudComboBox` |
| Select an enum or combined flags | `EnumComboBox` or `FlagsCheckedListBox` |
| Choose a fixed toolbar value through a checked menu | `ToolStripChoiceSelector` |
| Show progress or scoped busy state | `ProgressBarEx` or `LoadingCircle` |
| Host plugin blocks and screens | `BlockMainForm`, `WinFormsApplicationScreen` and the application screen host |
| Present a question, text, password or authentication dialog | Existing async dialog helpers |
| Review generated CSV, JSON or text before selecting a save destination | `ReviewTextDialog` |
| Inspect or edit an in-memory byte buffer as hexadecimal and ASCII | `HexEditorControl` or `HexEditorDialog` |
| Display resource appointments | `AppointmentBook` and `IAppointmentBookDataSource` |
| Plot captured metric history with gaps, units and hover readings | `TelemetryChart` |
| Plot generic numeric series or category bars | `GraphChart`, `BarChart` |
| Use specialized chart types and the absorbed charting API | `Sphere10.Framework.Windows.Forms.DataVisualization.Chart` |

## File, image and logging selectors

| Component | Actual API and use |
|---|---|
| [PathSelectorControl](../../../src/Sphere10.Framework.Windows.Forms/Controls/PathSelectorControl.cs) | `Path`, `Mode`, `Editable`, `ForcePathExists`, `PlaceHolderText`, `OverwritePrompt` and parameterless `PathChanged`. [PathSelectionMode](../../../src/Sphere10.Framework.Windows.Forms/Controls/PathSelectionMode.cs) supplies `OpenFile`, `SaveFile`, `File` and `Folder`. Selection does not itself open an application resource. Disable `OverwritePrompt` for append-only use; the logger selector already does so. |
| [LogOutputSelectorControl](../../../src/Sphere10.Framework.Windows.Forms/Controls/LogOutputSelectorControl.cs) | Compact **Log Output** checkbox and **...** (Configure Log Output) button. First On opens the wizard; later On reuses accepted options. `ShowConfigurationAsync(owner)` shows details, Start/Stop and Change launching `ConfigureAsync(owner)`. `Configuration` is a defensive snapshot; `ApplicationLogger` and optional `ScreenHost` are borrowed. `Logger` is a `NoOpLogger` while off; `EnqueueInfo` provides bounded worker output, with `FlushLoggingAsync`, `StopLoggingAsync` and owner-thread `DisposeAsync` for draining. Screen output opens the host-owned `LogScreen` undocked. File output includes rolling/encoding options; `MaxFiles` includes the active file. `RestoreSettings()` restores optional Remember After Close preferences once logger/hooks are bound. |
| [ImageAttachmentSelectorControl](../../../src/Sphere10.Framework.Windows.Forms/Common/ImageAttachmentSelectorControl.cs) | Choose `None`, `Clipboard` or `File` through `AttachmentSource`; inspect `Filename`/`HasSelectedImage` and call `TryGetSelectedImage(out Image)`. A successful call returns a newly obtained image to be disposed by its consumer. File-backed images can retain a file lock until disposed. |
| [TextBoxWriter](../../../src/Sphere10.Framework.Windows.Forms/TextBox/TextBoxWriter.cs) and [TextBoxLogger](../../../src/Sphere10.Framework.Windows.Forms/TextBox/TextBoxLogger.cs) | Adapt an existing `TextBox` to a writer/logger. `TextBoxWriter` dispatches appends to the UI; `TextBoxLogger` composes timestamping and the existing asynchronous logger. Its returned log call does not imply persisted/drained output; use the [logging ownership guidance](logging.md). |

The log selector does no output/settings IO during construction, property
configuration, `Logger` access or design-mode construction. Explicit
`RestoreSettings()` can read preferences and reopen an enabled remembered output;
call it after the owning logger and consumer hooks are ready. Configure/start/stop it on its UI thread;
workers may retrieve `Logger` and call it. The property changes when output is
accepted or stopped, so a long-lived service should resolve it per event. File
resources are owned; application output and console writers are borrowed. The
selected session joins owned file writes with transitions and ignores calls after
it closes. Borrowed logger callbacks execute outside its scope so they may
dispatch to the UI without blocking UI state reads/Stop; their existing owner
controls completion. Do not separately dispose or globally register the session.

`LoggingStarting(configuration)` runs before file IO and lets a consumer reject the selection by
throwing a configuration exception. `LoggingStateChanged` and
`LoggingFailed(Exception)` report transitions and failures. A write failure closes
the sink and exposes the error; visual notification is marshalled to the UI thread
or retained until its handle is created. Recovery before display reconciles the
current logging state rather than repainting an obsolete failure. Programmatic
start/write failures propagate to the caller. Wizard cancellation preserves the
current output; accepted configuration remains available through the **...** button.
Use [logging preferences](user-ui-settings.md#reusable-logging-preferences) for
stable IDs, accepted save boundaries, enabled-choice restoration and optional errors.

Consumers that support changing event output can resolve `() => control.Logger`
for each accepted event. A directory-monitor application must reject output
inside its monitored tree to avoid logging its own writes; use `LoggingStarting`
before IO. SystemExpert demonstrates this in its application-owned watcher; the
general selector does not impose a watcher-specific file policy.

The selector's concrete designer-editable workflow surfaces are
[LogOutputConfigurationDialog](../../../src/Sphere10.Framework.Windows.Forms/Controls/Logging/LogOutputConfigurationDialog.cs),
[LogOutputDestinationWizardScreen](../../../src/Sphere10.Framework.Windows.Forms/Controls/Logging/LogOutputDestinationWizardScreen.cs),
[LogOutputFileWizardScreen](../../../src/Sphere10.Framework.Windows.Forms/Controls/Logging/LogOutputFileWizardScreen.cs),
[LogOutputRollingWizardScreen](../../../src/Sphere10.Framework.Windows.Forms/Controls/Logging/LogOutputRollingWizardScreen.cs),
[LogOutputFormattingWizardScreen](../../../src/Sphere10.Framework.Windows.Forms/Controls/Logging/LogOutputFormattingWizardScreen.cs)
and [LogOutputReviewWizardScreen](../../../src/Sphere10.Framework.Windows.Forms/Controls/Logging/LogOutputReviewWizardScreen.cs),
using the concrete [LogOutputWizardScreen](../../../src/Sphere10.Framework.Windows.Forms/Controls/Logging/LogOutputWizardScreen.cs)
bridge to the typed wizard API. Applications normally use the selector, not a
second local copy of these steps. File/rolling steps are injected only for File;
changing destination on Back replaces that branch without duplicate steps.

## Database connection editors

### Shared provider contract and engine selection

[IDatabaseConnectionProvider](../../../src/Sphere10.Framework.Windows.Forms/Database/IDatabaseConnectionProvider.cs)
defines `ConnectionString`, `DatabaseName`, `GetDAC()` and `Task<Result>
TestConnection()`. These are connection-setting editors, not database explorers,
data grids or transaction owners. `GetDAC()` constructs a provider DAC without
opening a connection. `TestConnection()` opens and disposes a temporary DAC scope
on a worker and returns result errors for connection failures. String parsing and
artificial-key loading can throw before that connection-error handler is entered.
Read settings/call the control on the UI thread, await the test, and inspect
`Result.IsSuccess`.

| Component | Selection and responsibility |
|---|---|
| [DatabaseConnectionPanel](../../../src/Sphere10.Framework.Windows.Forms/Database/DatabaseConnectionPanel.cs) | Full engine selector plus the selected provider panel. `SelectedDBMSType`, `IgnoreDBMS`, `ConnectionString`, `DatabaseName` and read-only `Database : DBReference`. Raises `DBMSTypeChanged(panel, dbmsType)`. |
| [DatabaseConnectionBar](../../../src/Sphere10.Framework.Windows.Forms/Database/DatabaseConnectionBar.cs) | Compact engine selector plus selected provider bar. Same common settings; `Database : DBReference` is assignable, choosing the engine before copying its string. The bar has no public `DBMSTypeChanged` event. |
| [ConnectionPanelBase](../../../src/Sphere10.Framework.Windows.Forms/Database/ConnectionPanelBase.cs) | Concrete designer surface and provider extension hooks. Default connection members throw `NotImplementedException`; use a real provider control for a usable editor. |
| [ConnectionBarBase](../../../src/Sphere10.Framework.Windows.Forms/Database/ConnectionBarBase.cs) | Equivalent compact extension surface. Also supplies `ArtificialKeysFile` and `SelectArtificialKeysFile()`. `GetDAC()` loads the selected artificial-key XML into the returned DAC. |

The generic selectors use the running framework's named provider services. Add
the matching UI provider `ModuleConfiguration` through the existing startup
builder's `UseModule<TModule>()` **before** constructing them. Referencing a package
alone does not register these services. They select only registered engines;
`IgnoreDBMS` can hide further choices. Set `SelectedDBMSType` before assigning
`ConnectionString`. A concrete provider control can instead be constructed
directly without named-service lookup.

The controls inherit `UserControlEx.StateChanged`, which reports edited UI state,
not connectivity. Panels/bars do not create a database merely because the user
selected a new filename. Application code owns its subsequent DAC scopes,
connections and transactions; see [persistence](persistence-and-transactions.md).

### Concrete provider packages

| Package and controls | Fields, registration and important limits |
|---|---|
| [Windows.Forms.Sqlite](../../../src/Sphere10.Framework.Windows.Forms.Sqlite/README.md): [SqliteConnectionPanel](../../../src/Sphere10.Framework.Windows.Forms.Sqlite/SqliteConnectionPanel.cs), [SqliteConnectionBar](../../../src/Sphere10.Framework.Windows.Forms.Sqlite/SqliteConnectionBar.cs) | Namespace `Sphere10.Framework.Windows.Forms.Sqlite`. Filename, password, journal mode and sync mode. `Filename`, `Password` and `HasPassword` are read-only; set through `ConnectionString`/UI, and use `Mode` to choose the file-selection behavior. Module registers `DBMSType.Sqlite`. Generated strings always set `FailIfMissing = true`; create databases separately. Native `System.Data.SQLite` runtime assets are required. |
| [Windows.Forms.MSSQL](../../../src/Sphere10.Framework.Windows.Forms.MSSQL/README.md): [MSSQLConnectionPanel](../../../src/Sphere10.Framework.Windows.Forms.MSSQL/MSSQLConnectionPanel.cs), [MSSQLConnectionBar](../../../src/Sphere10.Framework.Windows.Forms.MSSQL/MSSQLConnectionBar.cs) | Namespace `Sphere10.Framework.Windows.Forms.MSSQL`. `Server`, `Database`, `Username`, `Password` and string `Port`; DAC is `MSSQLDAC`. Module registers `DBMSType.SQLServer`. Strings always emit `IntegratedSecurity = false`; these controls do not offer a Windows authentication toggle or server/database discovery. |
| [Windows.Forms.Firebird](../../../src/Sphere10.Framework.Windows.Forms.Firebird/README.md): [FirebirdConnectionPanel](../../../src/Sphere10.Framework.Windows.Forms.Firebird/FirebirdConnectionPanel.cs), [FirebirdConnectionBar](../../../src/Sphere10.Framework.Windows.Forms.Firebird/FirebirdConnectionBar.cs) | Namespace `Sphere10.Framework.Windows.Forms.Firebird`. Server, database, username, password and port. Module registers `DBMSType.Firebird`. Generated strings use `FbServerType.Default`. |
| Same Firebird package: [FirebirdEmbeddedConnectionPanel](../../../src/Sphere10.Framework.Windows.Forms.Firebird/FirebirdEmbeddedConnectionPanel.cs), [FirebirdEmbeddedConnectionBar](../../../src/Sphere10.Framework.Windows.Forms.Firebird/FirebirdEmbeddedConnectionBar.cs) | Filename, username and password UI; public read-only `Filename`, `Password`, `HasPassword` plus `Mode`. No public embedded `Username` property. Module registers `DBMSType.FirebirdFile`. Generated strings use `FbServerType.Embedded`; matching native client/runtime setup and database creation remain external responsibilities. |
| [Windows.Forms.MySQL](../../../src/Sphere10.Framework.Windows.Forms.MySQL/README.md): [MySQLConnectionPanel](../../../src/Sphere10.Framework.Windows.Forms.MySQL/MySQLConnectionPanel.cs), [MySQLConnectionBar](../../../src/Sphere10.Framework.Windows.Forms.MySQL/MySQLConnectionBar.cs) | Namespace `Sphere10.Framework.Windows.Forms.MySQL`. Server, database, username, password and port, backed by `MySqlConnector` and `MySQLDAC`. [Module](../../../src/Sphere10.Framework.Windows.Forms.MySQL/ModuleConfiguration.cs) registers `DBMSType.MySQL`. Blank/default port uses 3306; generated strings contain only the visible fields. |
| [Windows.Forms.PostgreSQL](../../../src/Sphere10.Framework.Windows.Forms.PostgreSQL/README.md): [PostgreSQLConnectionPanel](../../../src/Sphere10.Framework.Windows.Forms.PostgreSQL/PostgreSQLConnectionPanel.cs), [PostgreSQLConnectionBar](../../../src/Sphere10.Framework.Windows.Forms.PostgreSQL/PostgreSQLConnectionBar.cs) | Namespace `Sphere10.Framework.Windows.Forms.PostgreSQL`. Server/host, database, username, password and port, backed by `Npgsql` and `PostgreSQLDAC`. [Module](../../../src/Sphere10.Framework.Windows.Forms.PostgreSQL/ModuleConfiguration.cs) registers `DBMSType.PostgreSQL`. Blank/default port uses 5432. |
| [Windows.Forms.Oracle](../../../src/Sphere10.Framework.Windows.Forms.Oracle/README.md): [OracleConnectionPanel](../../../src/Sphere10.Framework.Windows.Forms.Oracle/OracleConnectionPanel.cs), [OracleConnectionBar](../../../src/Sphere10.Framework.Windows.Forms.Oracle/OracleConnectionBar.cs) | Namespace `Sphere10.Framework.Windows.Forms.Oracle`. Server/descriptor, service (`Database`), username, password and port, backed by Oracle's managed client and `OracleDAC`. [Module](../../../src/Sphere10.Framework.Windows.Forms.Oracle/ModuleConfiguration.cs) registers `DBMSType.Oracle`. Host/service mode uses port 1521 by default. Without a service, Server is the complete DataSource. Loading a connection string copies DataSource to Server and clears service/port instead of parsing its descriptor, so `DatabaseName` can then be empty. |

Provider editors rebuild strings from their visible fields. They do **not** round
trip every provider option, including arbitrary pooling, read-only, encryption,
character-set, role or client-library settings. Apply required additional options
to the edited string through that provider's connection-string builder before
constructing the DAC. Invalid port/settings text can throw during reconstruction.
Avoid logging strings containing credentials. Selecting a password field does not
prove a given SQLite native build supports encryption.

The actual multi-provider consumers are the tester's
[ConnectionPanelTestScreen](../../../utils/Sphere10.Framework.Utils.WinFormsTester/Screens/ConnectionPanelTestScreen.cs),
[ConnectionBarTestScreen](../../../utils/Sphere10.Framework.Utils.WinFormsTester/Screens/ConnectionBarTestScreen.cs)
and [startup module registration](../../../utils/Sphere10.Framework.Utils.WinFormsTester/Program.cs).
Use their awaited connection tests and scoped busy indicator; keep new static
dialog layouts in a concrete designer-editable form, as the designer practice
requires.

## CRUD grids, record selection and entity editing

| Component | Use and API |
|---|---|
| [CrudGrid](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudGrid.cs) | `GridBindings` of `CrudGridColumn<T>`, `SetDataSource<T>(IDataSource<T>, capabilities)`, `RefreshGrid()`, record create/edit/delete operations, selected entity, search/sort/paging and optional inline edits. These task-returning methods retain their established names without `Async` suffixes. Source capabilities and supplied permission masks are intersected at binding. |
| [CrudGridColumn<T>](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudGridColumn.cs) | Actual typed value getter/setter, display type, header, sort name, property path, per-cell edit/availability, command actions and explicit `ExpandsToFit`. `CrudCellDisplayType.Image` uses a borrowed read-only image; the owning cache/consumer must keep it alive while displayed. |
| [CrudComboBox](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudComboBox.cs) | Namespace `Sphere10.Framework.Windows.Forms.Crud`. A `CustomComboBox` with a full CrudGrid dropdown; await `SetCrudParameters<T>(...)`, configure display delegate, `MaximumDropDownSize`, selected entity and auto-hide behavior. Supply an already constrained source capability mask. |
| [CrudReferenceBinding<T>](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudReferenceBinding.cs) | Describe an eligible related-record source, picker columns, shallow display text, null policy and fixed popup bounds. Share the binding with inline columns and the property-grid editor through property paths. |
| [CrudReferencePicker](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudReferencePicker.cs) and [CrudReferenceEditor](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudReferenceEditor.cs) | Read/search/page existing records while preserving selected object identity. Load asynchronously; accept/cancel/clear selection according to the binding. These are reference editors, not ownership-graph editors. |
| [DefaultCrudEntityEditor](../../../src/Sphere10.Framework.Windows.Forms/Crud/DefaultCrudEntityEditor.cs) | Property-grid editing, session-local descriptors, related-record bindings, owned-object expansion, changed values and undo/accept. Implements `ICrudEntityEditor<object>`. |
| [CrudEntityEditorDialog](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudEntityEditorDialog.cs) | Owns editor presentation, save/delete/validation/cancel decisions for the supplied data source and entity. |
| [CrudDialog](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudDialog.cs) | `ShowAsync<T>(...)` for a complete CRUD grid dialog, with optional custom entity editor. |
| [CrudGridSnapshotController<T>](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudGridSnapshotController.cs), [ICrudGridSnapshotController](../../../src/Sphere10.Framework.Windows.Forms/Crud/ICrudGridSnapshotController.cs) | Typed nonvisual collection/binding for a concrete designer surface. Caller-supplied async collector, identity and optional value formatter; typed `Rows`, `Count`, `SetDefaultSort`, `RefreshAsync` and `CaptureSnapshot`. Serializes refreshes and preserves selection across replacement instances. Use an existing core `SnapshotDataSource<T>` through its constructor overload when appropriate; export policy stays with the caller. |
| [CrudGridSettings](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudGridSettings.cs) and [grid settings integration](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudGrid.cs) | Reuse `PersistSettings`/`SettingsKey` for accepted manual paging, automatic sizing choice and sorting preferences. See the settings practice for lifecycle and IDs. |

Use the [WinForms project guide](../../../src/Sphere10.Framework.Windows.Forms/README.md)
for capability masks, complete-row automatic page sizing, refresh completion,
reference identity, editor commit/undo and fixed dropdown sizing. Use an existing sync/async
`IDataSource<T>` base according to the source's execution direction; see
[data-source practice](data-sources.md). Diagnostic/read-only tables still use
CrudGrid with restricted capabilities. `VisibleEntities` represents the current
page, not the full collection.

For grid-driven screens, follow the [row-action toolbar/context-menu rule](winforms-designer.md#row-actions-on-grid-screens).
Expose custom row tools in the screen toolbar as well as by right-click, sharing
their handlers and selection-dependent availability. Standard CRUD actions may
remain in the grid/context menu according to the screen's workflow.

The [CrudTestScreen](../../../utils/Sphere10.Framework.Utils.WinFormsTester/Screens/CrudTestScreen.cs)
demonstrates the complete editing path. Core's
[SnapshotDataSource<T>](../../../src/Sphere10.Framework/DataSource/SnapshotDataSource.cs)
supplies platform-neutral snapshot queries, and Windows'
[FileIconCache](../../../src/Sphere10.Framework.Windows/Shell/FileIconCache.cs)
supplies owned cached icons with borrowed image results. Keep nonvisual typed behavior
behind a concrete nongeneric designer surface.

## Text, numeric, enum and list input

| Component | Use and API |
|---|---|
| [TextBoxEx](../../../src/Sphere10.Framework.Windows.Forms/TextBox/TextBoxEx.cs), [ComboBoxEx](../../../src/Sphere10.Framework.Windows.Forms/ComboBoxes/ComboBoxEx.cs), [PlaceHolderTextExtender](../../../src/Sphere10.Framework.Windows.Forms/Components/PlaceHolderExtender.cs) | Existing placeholder/cue text support through `PlaceHolderText` and the extender. |
| [SearchTextBox](../../../src/Sphere10.Framework.Windows.Forms/TextBox/SearchTextBox.cs) | TextBoxEx with a search icon and search placeholder. It supplies presentation; implement the source's filtering through its actual search contract. |
| [ButtonTextBox](../../../src/Sphere10.Framework.Windows.Forms/TextBox/ButtonTextBox.cs) | Text input with an embedded `Button`, exposed `ButtonClick` and text-margin adjustment. |
| [IntBox](../../../src/Sphere10.Framework.Windows.Forms/TextBox/IntBox.cs), [DecimalBox](../../../src/Sphere10.Framework.Windows.Forms/TextBox/DecimalBox.cs), [MoneyBox](../../../src/Sphere10.Framework.Windows.Forms/TextBox/MoneyBox.cs) | Nullable typed `Value`, `ValueChanged`, validity, format and empty text via `NumericBoxBase<T>`. This is an older abstract/generic visual ancestry; inspect designer compatibility before choosing it for a new designer surface. |
| [EnumComboBox](../../../src/Sphere10.Framework.Windows.Forms/ComboBoxes/EnumComboBox.cs) | `EnumType`, `SelectedEnum`, `Filter`, `IgnoreEnums`, `AllowEmptyOption` and `EmptyOptionText`. Displays framework enum descriptions rather than requiring a manually duplicated label list. |
| [CheckedListBoxEx](../../../src/Sphere10.Framework.Windows.Forms/CheckedListBoxes/CheckedListBoxEx.cs) | Check-on-click, `CheckItems` helpers and `ItemChecked`, raised after the checked state changes through UI dispatch. |
| [FlagsCheckedListBox](../../../src/Sphere10.Framework.Windows.Forms/CheckedListBoxes/FlagsCheckedListBox.cs) | `EnumType` must carry `[Flags]`; `SelectedEnum` combines selected values, with compound parent/child flag updates and `HasSelectedValue`. It converts numeric values to signed `Int64`; verify unsigned high-bit enum cases before adopting them. |
| [CustomComboBox](../../../src/Sphere10.Framework.Windows.Forms/ComboBoxes/CustomComboBox.cs), [PopupControl and PopupDropDown](../../../src/Sphere10.Framework.Windows.Forms/ComboBoxes/PopupControl.cs) | Host an existing dropdown `Control`, with resize/size policies and selected-value events. PopupDropDown supplies the ToolStripDropDown visual surface. Prefer the ready CrudComboBox/reference picker for record selection. |
| [ListMerger](../../../src/Sphere10.Framework.Windows.Forms/ListMerger/ListMerger.cs) | Two named list sides, `LeftItems`/`RightItems` arrays, display/value members and `MergeMode`; reports `ItemSelected`/`ItemsMoved`. Reuse for moving/copying membership between two lists. |
| [CheckedGroupBox](../../../src/Sphere10.Framework.Windows.Forms/Controls/CheckedGroupBox.cs), [RadioGroupBox](../../../src/Sphere10.Framework.Windows.Forms/Controls/RadioGroupBox.cs), [RadioButtonPanel](../../../src/Sphere10.Framework.Windows.Forms/Controls/RadioButtonPanel.cs) | Grouped optional/alternative settings. Checked/radio group captions select the group and can disable children; the panel coordinates radio buttons and radio group boxes. |

## Progress, validation, images and visual layout

| Component | Use and API |
|---|---|
| [LoadingCircle](../../../src/Sphere10.Framework.Windows.Forms/LoadingCircle/LoadingCircle.cs) | Busy spinner presets and appearance; `StartAnimating`, `StopAnimating`, `BeginAnimationScope()` and static `EnterAnimationScope(...)`, optionally disabling a container except explicit exclusions. Scope disposal restores activity. |
| [LoadingCircleToolStripMenuItem](../../../src/Sphere10.Framework.Windows.Forms/LoadingCircle/LoadingCircleToolStripMenuItem.cs) | Existing ToolStripControlHost for the spinner, exposing `LoadingCircleControl`. |
| [ProgressBarEx](../../../src/Sphere10.Framework.Windows.Forms/Controls/ProgressBarEx.cs) | Custom-painted progress with `DisplayStyle` and `CustomText`. Its current Percentage mode prints `Value + '%'`, not a normalized `(Value-Minimum)/(Maximum-Minimum)` calculation; use a 0–100 value or supply text. |
| [ValidationIndicator](../../../src/Sphere10.Framework.Windows.Forms/Controls/ValidationIndicator.cs) | Validation-state icon, `PerformValidation`, `ValidationStateChanged`, tooltip settings and `RunValidation()`. The public trigger is `async void`; it is not an awaitable validation-completion contract. |
| [PictureBoxEx](../../../src/Sphere10.Framework.Windows.Forms/Controls/PictureBoxEx.cs) | Scaled `SystemIconType` images, transparent rendering and resize/DPI updates. |
| [GradientPanel](../../../src/Sphere10.Framework.Windows.Forms/Panels/GradientPanel.cs), [TransparentPanel](../../../src/Sphere10.Framework.Windows.Forms/Panels/TransparentPanel.cs), [RoundedPanel](../../../src/Sphere10.Framework.Windows.Forms/ExplorerBar/RoundedPanel.cs) | Existing gradient, custom background/transparent and rounded visual panel families. |
| [GraphChart](../../../src/Sphere10.Framework.Windows.Forms/Charts/GraphChart.cs), [BarChart](../../../src/Sphere10.Framework.Windows.Forms/Charts/BarChart.cs) | Designer-editable DataVisualization controls for owned numeric multi-series snapshots, line/area/column/horizontal bars, category labels, signed ranges, gaps, legends, axis formats and nearest-point lookup. No timer or data collection starts in construction. Snapshot publication reuses controls and native series; disposal owns the engine. WinForms Tester's Line And Bar Charts screen and System Expert consume these controls. See [numeric charts](../../../src/Sphere10.Framework.Windows.Forms/README.md#numeric-charts). |
| [TelemetryChart](../../../src/Sphere10.Framework.Windows.Forms/Charts/TelemetryChart.cs) | Captured-history adapter over `GraphChart` with copied `TelemetryChartPoint[]`, percent/byte/rate/Celsius scales, missing-value gaps and nearest-timestamp hover. `SetSamples` accepts captured data; the control has no sampler, timer or application service dependency. All System Expert history views, including the embedded Core Temperatures history, consume this control. See the [chart guide](../../../src/Sphere10.Framework.Windows.Forms/README.md#telemetry-history-charts) for presentation options and UI-thread ownership. |
| [PanelEx](../../../src/Sphere10.Framework.Windows.Forms/Panels/PanelEx.cs) | UserControlEx surface with no extra domain behavior. |
| [ExpandingCircle](../../../src/Sphere10.Framework.Windows.Forms/Controls/ExpandingCircle.cs), [PerPixelAlphaForm](../../../src/Sphere10.Framework.Windows.Forms/Forms/PerPixelAlphaForm.cs) | Specialized overlay animation and alpha-layered bitmap forms; use only when that presentation is required. |
| [ToolStripChoiceSelector](../../../src/Sphere10.Framework.Windows.Forms/Controls/ToolStripChoiceSelector.cs) | `ToolStripDropDownButton` with checked menu choices. Use `SetItems(IEnumerable<object>)`, `SelectedItem`, `SelectedIndex` and `SelectedIndexChanged`; `Items` returns copied array membership retaining original object identity. `ItemTextProvider` formats captions and `EmptyText` labels no selection. Native click/keyboard paths share selection, and disposal releases menu references while borrowing the data objects. Use for fixed toolbar choices; retain editable path fields and complex CRUD pickers for their separate contracts. |
| [SplitButton](../../../src/Sphere10.Framework.Windows.Forms/Buttons/SplitButton.cs) | Primary button plus `SplitMenuStrip`, with `ShowSplit`. |
| [PadLockButton](../../../src/Sphere10.Framework.Windows.Forms/Buttons/PadLockButton.cs) | `LockState`, `ToggleLockState` and `PadLockStateChanged` for a visual locked/unlocked choice. The icon alone does not enforce a permission. |
| [SquareButton](../../../src/Sphere10.Framework.Windows.Forms/Buttons/SquareButton.cs), [BitmapButton](../../../src/Sphere10.Framework.Windows.Forms/Buttons/BitmapButton.cs) | Gradient/pressed navigation button and bitmap state-strip button respectively. BitmapButton expects tiled up/down/focused/hover/disabled images. |
| [SidebarToggleButton](../../../src/Sphere10.Framework.Windows.Forms/Buttons/SidebarToggleButton.cs) | Native navigation-pane toggle used by BlockMainForm. Reuse the existing host integration. |
| [TaskPane](../../../src/Sphere10.Framework.Windows.Forms/ExplorerBar/TaskPane.cs), [Expando](../../../src/Sphere10.Framework.Windows.Forms/ExplorerBar/Expando.cs), [TaskItem](../../../src/Sphere10.Framework.Windows.Forms/ExplorerBar/TaskItem.cs) | Existing explorer-style collapsible navigation groups and commands. The application block infrastructure composes them already. |
| [MergeableMenuStrip](../../../src/Sphere10.Framework.Windows.Forms/MergeableStrips/MergeableMenuStrip.cs), [MergeableToolStrip](../../../src/Sphere10.Framework.Windows.Forms/MergeableStrips/MergeableToolStrip.cs), [MergeableStatusStrip](../../../src/Sphere10.Framework.Windows.Forms/MergeableStrips/MergeableStatusStrip.cs) | Existing visual-inheritance strip helpers, with `InheritedToolStrip`; [ToolStripVisualInheritanceFixer](../../../src/Sphere10.Framework.Windows.Forms/MergeableStrips/ToolStripVisualInheritanceFixer.cs) is their merge component. New screen commands should use the actual screen-owned strip and existing host composition. |

The tester has [LoadingCircleTestScreen](../../../utils/Sphere10.Framework.Utils.WinFormsTester/Screens/LoadingCircleTestScreen.cs)
and [FlagsCheckedBoxListScreen](../../../utils/Sphere10.Framework.Utils.WinFormsTester/Screens/FlagsCheckedBoxListScreen.cs).
Visual-state/layout updates should use [rendering scopes](../../../src/Sphere10.Framework.Windows.Forms/Common/ControlRenderingScope.cs)
through `Control.EnterRenderingScope()` when needed; follow the native lifecycle
and visibility contract in the [WinForms README](../../../src/Sphere10.Framework.Windows.Forms/README.md#native-helper-migration).

## Forms, dialogs and application hosting

| Family | Existing entry points |
|---|---|
| Reusable updating surfaces | [UserControlEx](../../../src/Sphere10.Framework.Windows.Forms/Controls/UserControlEx.cs) and [FormEx](../../../src/Sphere10.Framework.Windows.Forms/Forms/FormEx.cs): `StateChanged`, model/UI hooks and `EnterUpdateScope(...)`. [ApplicationControl](../../../src/Sphere10.Framework.Windows.Forms/Application/Controls/ApplicationControl.cs) and [ApplicationForm](../../../src/Sphere10.Framework.Windows.Forms/Application/Forms/ApplicationForm.cs) add application services; ordinary reusable controls should not introduce a DI dependency solely for ancestry. |
| Main application/workspace | [LiteMainForm](../../../src/Sphere10.Framework.Windows.Forms/Application/Forms/LiteMainForm.cs), [MainForm](../../../src/Sphere10.Framework.Windows.Forms/Application/Forms/MainForm.cs), [BlockMainForm](../../../src/Sphere10.Framework.Windows.Forms/Application/Forms/BlockMainForm.cs), [WinFormsApplicationScreen](../../../src/Sphere10.Framework.Windows.Forms/Application/Forms/WinFormsApplicationScreen.cs), [WinFormsApplicationScreenHost](../../../src/Sphere10.Framework.Windows.Forms/Application/Controls/WinFormsApplicationScreenHost.cs), its abstract [WinFormsApplicationScreenHostBase](../../../src/Sphere10.Framework.Windows.Forms/Application/Controls/WinFormsApplicationScreenHostBase.cs), [WinFormsApplicationScreenTabControl](../../../src/Sphere10.Framework.Windows.Forms/Application/Controls/WinFormsApplicationScreenTabControl.cs) and [WinFormsApplicationScreenForm](../../../src/Sphere10.Framework.Windows.Forms/Application/Forms/WinFormsApplicationScreenForm.cs). These own existing selection, retention, close guards, screen toolbars/menus and docking. Use the concrete host; the abstract base is an implementation foundation. |
| Application presentation | [ApplicationBar](../../../src/Sphere10.Framework.Windows.Forms/Application/Controls/ApplicationBar.cs) contributes block/navigation items; [ApplicationBanner](../../../src/Sphere10.Framework.Windows.Forms/Application/Controls/ApplicationBanner.cs) is the stock designer surface for title/version/company metadata. [ApplicationBannerHost](../../../src/Sphere10.Framework.Windows.Forms/Application/Controls/ApplicationBannerHost.cs) opts in to a registered transient banner per host while retaining its stock designer preview. See [banner extension and ownership](../../../src/Sphere10.Framework.Windows.Forms/README.md#application-banners). |
| Questions and errors | [DialogEx](../../../src/Sphere10.Framework.Windows.Forms/Dialogs/DialogEx.cs), [QuestionDialog](../../../src/Sphere10.Framework.Windows.Forms/Dialogs/QuestionDialog.cs), [QuestionDialogSession](../../../src/Sphere10.Framework.Windows.Forms/Dialogs/QuestionDialogSession.cs) and [ExceptionDialog](../../../src/Sphere10.Framework.Windows.Forms/Dialogs/ExceptionDialog.cs). Use their existing async helpers and explicit native owner. |
| Text and credentials | [EnterTextDialog](../../../src/Sphere10.Framework.Windows.Forms/Dialogs/EnterTextDialog.cs) returns acceptance/input; [PasswordDialog](../../../src/Sphere10.Framework.Windows.Forms/Dialogs/PasswordDialog.cs) supplies policy validation; [LogonDialog](../../../src/Sphere10.Framework.Windows.Forms/Dialogs/LogonDialog.cs) accepts an asynchronous authentication delegate. |
| Text export review | [ReviewTextDialog](../../../src/Sphere10.Framework.Windows.Forms/Dialogs/ReviewTextDialog.cs) previews complete read-only text with Copy and Save, initially unselected. `ShowAsync(owner, title, instructions, text, defaultFileName, filter, encoding, cancellationToken, logger)` returns the saved path or null on user closure; token cancellation throws. Direct `SaveAsync(path, cancellationToken)` uses the captured preview/encoding and replaces a destination only after a complete temporary copy succeeds. The dialog owns its picker/temporary file; the optional error logger is borrowed. Use the [README example](../../../src/Sphere10.Framework.Windows.Forms/README.md#review-text-before-saving) for CSV/JSON exports. |
| Binary editing | [HexEditorControl](../../../src/Sphere10.Framework.Windows.Forms/Controls/HexEditorControl.cs) edits owned bytes with a bounded virtual hex/ASCII viewport; [HexEditorDialog](../../../src/Sphere10.Framework.Windows.Forms/Dialogs/HexEditorDialog.cs) returns a fresh accepted array or null on cancellation. Read the [byte editing API](../../../src/Sphere10.Framework.Windows.Forms/README.md#byte-and-hexadecimal-editing) before binding it to a draft; buffer assignment resets its baseline, and persistence remains with the consumer. |
| Generic object/text editors | [GenericEditorForm](../../../src/Sphere10.Framework.Windows.Forms/Forms/GenericEditorForm.cs) and its `ShowFormAsync(object, readOnly)` helper; [TextEditorForm](../../../src/Sphere10.Framework.Windows.Forms/Forms/TextEditorForm.cs) for an existing text-editor surface. Prefer the CRUD editor when persistence/undo/reference semantics are needed. |
| Lifecycle splash | [SplashScreen](../../../src/Sphere10.Framework.Windows.Forms/Dialogs/SplashScreen.cs) attaches to application/framework progress. Configure it through the existing application startup builder; preserve approved shutdown ordering from the lifecycle guide. |
| Wizards | [WinFormsWizardBuilder<T>](../../../src/Sphere10.Framework.Windows.Forms/Wizard/WinFormsWizardBuilder.cs), [WinFormsWizardScreen<T>](../../../src/Sphere10.Framework.Windows.Forms/Wizard/WinFormsWizardScreen.cs), [DefaultWinFormsWizardScreen](../../../src/Sphere10.Framework.Windows.Forms/Wizard/DefaultWinFormsWizardScreen.cs) and [WinFormsWizardDialog<T>](../../../src/Sphere10.Framework.Windows.Forms/Wizard/WinFormsWizardDialog.cs), driven by shared wizard metadata and orchestrator. DefaultWinFormsWizardScreen specializes dictionary-backed steps. Preserve the family's existing generic API and follow the designer practice when providing new concrete step surfaces. |
| Product feedback/about | [ProductAboutBox](../../../src/Sphere10.Framework.Windows.Forms/Application/Product/ProductAboutBox.cs), [ProductProductReportBugDialog](../../../src/Sphere10.Framework.Windows.Forms/Application/Product/ProductProductReportBugDialog.cs), [ProductProductRequestFeatureDialog](../../../src/Sphere10.Framework.Windows.Forms/Application/Product/ProductProductRequestFeatureDialog.cs), and [ProductProductSendCommentsDialog](../../../src/Sphere10.Framework.Windows.Forms/Application/Product/ProductProductSendCommentsDialog.cs). These use configured product/application services. |
| Product contact UI | [BasicContactDetailsControl](../../../src/Sphere10.Framework.Windows.Forms/Application/Product/Controls/BasicContactDetailsControl.cs) and [WhoAreYouControl](../../../src/Sphere10.Framework.Windows.Forms/Application/Product/Controls/WhoAreYouControl.cs) supply anonymous/email contact details and user category for the generic feedback dialogs. |

The [application ownership practice](application-ownership.md) and
[native lifecycle guide](../../../src/Sphere10.Framework.Windows.Forms/README.md#application-lifecycle-and-splash)
own block/plugin/screen relationships, exit coordination and disposal. Reuse
those hosts rather than creating another navigation shell. Main-window settings
and controls' accepted preferences use the settings practice above.

For an application-wide keyboard shortcut, use
[GlobalHotKey](../../../src/Sphere10.Framework.Windows.Forms/Components/GlobalHotKey.cs)
and `IGlobalHotKey` on the owning UI thread. It receives Windows hotkey messages
without a keyboard hook, supports modifier combinations and disabling, suppresses
auto-repeat, and preserves an existing registration if a replacement conflicts.
Keep product preferences and status notifications in the application. See the
[component guide](../../../src/Sphere10.Framework.Windows.Forms/README.md#global-keyboard-shortcuts)
for registration and cleanup.

## Appointment/resource scheduling

[AppointmentBook](../../../src/Sphere10.Framework.Windows.Forms/AppointmentBook/AppointmentBook.cs)
(namespace `Sphere10.Framework.Windows.Forms.AppointmentBook`) displays resource
columns and time-based appointments through
[IAppointmentBookDataSource](../../../src/Sphere10.Framework.Windows.Forms/AppointmentBook/DataSources/IAppointmentBookDataSource.cs).
It exposes the selected appointment, period selection with `SetTimePeriod(...)`,
and selection, double-click, resize, drag/drop events. Its view-model converter is
an existing nonvisual extension point.
[BaseAppointmentBook](../../../src/Sphere10.Framework.Windows.Forms/AppointmentBook/BaseAppointmentBook.cs)
supplies the existing concrete inherited grid surface and view-model binding.

[UnallocatedBook](../../../src/Sphere10.Framework.Windows.Forms/AppointmentBook/UnallocatedBook.cs)
shows unallocated appointments using its corresponding view-model converter and
changes selection/drag/time availability behavior. Use the tester's
[AppointmentBookScreen](../../../utils/Sphere10.Framework.Utils.WinFormsTester/Screens/AppointmentBookScreen.cs)
for the paired resource/unallocated demonstration. This scheduling UI is distinct
from the framework's background job scheduler.

## Bundled grid and editor infrastructure

SourceGrid/DevAge visual components are included under the WinForms assembly's
`SourceGrid` namespaces. Prefer the framework's CrudGrid for application record
tables. Lower-level components remain appropriate when implementing a custom
cell presentation or a specialized existing grid, not to duplicate CRUD paging,
editing or reference selection.

| Family | Existing source and purpose |
|---|---|
| Cell and virtual grids | [Grid](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/SourceGrid/Grids/Grid.cs), [GridVirtual](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/SourceGrid/Grids/GridVirtual.cs), cell models/views/controllers/editors and selections. |
| Data/array/list/planning variants | [DataGrid](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/SourceGrid.Extensions/DataGrid.cs), [ArrayGrid](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/SourceGrid.Extensions/ArrayGrid.cs), [ListEditor](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/SourceGrid.Extensions/ListEditor.cs), [PlanningGrid](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/SourceGrid.Extensions/PlanningGrid.cs) and [PingGrid](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/SourceGrid.Extensions/PingGrids/PingGrid.cs). Inspect each binding contract; they are not `IDataSource<T>` adapters. |
| Validated input editors | [DevAgeTextBox](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/DevAgeTextBox.cs), [DevAgeMaskedTextBox](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/DevAgeMaskedTextBox.cs), [DevAgeComboBox](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/DevAgeComboBox.cs), [DevAgeNumericUpDown](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/DevAgeNumericUpDown.cs), [DevAgeTextBoxButton](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/DevAgeTextBoxButton.cs) and [TextBoxUITypeEditor](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/TextBoxUITypeEditor.cs). Namespace `Sphere10.Framework.Windows.Forms.SourceGrid.DevAgeControls`. |
| Rich text, colors and images | [DevAgeRichTextBox](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/DevAgeRichTextBox.cs), [ColorPicker](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/ColorPicker.cs) with `SelectedColor`, and [ImageNavigator](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/ImageNavigator.cs) with image array/current index/next/previous. |
| Supporting visual surfaces | [ButtonMultiSelection](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/ButtonMultiSelection.cs), [DropDownButton](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/DropDownButton.cs), [DropDown](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/DropDown.cs), [HeaderGroupBox](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/HeaderGroupBox.cs), [LinkLabel](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/LinkLabel.cs), [Line](../../../src/Sphere10.Framework.Windows.Forms/SourceGrid/DevAge.Windows.Forms/Windows/Forms/Line.cs), and bundled error forms. Retain the existing source notices. |

## Older surfaces and verified limits

The catalog is not a claim that every historical component has passed the current
designer/lifecycle tests. When touching an older component, preserve its working
consumer and address the required behavior in that owning layer rather than
copying a defect into a new component.

| Existing surface | Current source limitation |
|---|---|
| [TextLog](../../../src/Sphere10.Framework.Windows.Forms/TextBox/TextLog.cs) | Namespace `Sphere10.Framework.Windows.Forms.TextBoxes`. `AppendText()` is empty and selection properties throw `NotImplementedException`; it is not a usable large-log viewer. Use the existing TextBox writer/logger or a tested event grid for present behavior. |
| [PropertyGridEx](../../../src/Sphere10.Framework.Windows.Forms/Controls/PropertyGridEx.cs) | Stores `Readonly` without enforcing it and clears property selections in its selection override. Do not use that flag as a verified read-only guarantee. The default CRUD editor and explicit model descriptors own their actual editing policy. |
| [ServiceStatusControl](../../../src/Sphere10.Framework.Windows.Forms/Controls/ServiceStatusControl.cs) | `ServiceName`, `UseNssm`, `Status` and explicit `Start()` schedule 2-second status refreshes, removed when the parent form is disposed. Native Start/Stop branches currently invert the service-controller lookup condition; reuse requires fixing/testing that path. Nssm operations use the existing sentry. Do not start a monitoring job from a designer constructor. |
| [ExecuteScriptForm](../../../src/Sphere10.Framework.Windows.Forms/Forms/ExecuteScriptForm.cs) | Existing DAC/SQL script display, copy and explicit worker execution, but only a constructor taking `IDAC` and `ISQLBuilder`; it currently lacks the public parameterless design-time contract. |
| [BlockConfigurationScreen](../../../src/Sphere10.Framework.Windows.Forms/Application/Forms/BlockConfigurationScreen.cs), [ServerPostBackForm](../../../src/Sphere10.Framework.Windows.Forms/Application/Forms/ServerPostBackForm.cs) | Historical visual surfaces; their names do not establish a complete modern configuration workflow. Inspect their actual implementation/consumer before adopting. |
| Numeric editors and generic wizard visuals | Preserve the existing public family API, but do not assume a generic or abstract visual inheritance chain can be instantiated by Visual Studio's designer. Follow the concrete-surface/controller split when extending design-editable UI. |

## Catalog maintenance

When adding, renaming or completing a reusable Windows UI component, update its
category here with its actual source, principal API, ownership/activation boundary,
and a real consumer. Update its project README for detailed usage and link the
matching topic rather than duplicating rules. Record verified historical limits
explicitly; remove a limit only after the corresponding source behavior and
meaningful tests have changed. Keep this topic registered in the pattern task
index and both solutions' Developer Documentation folders.
