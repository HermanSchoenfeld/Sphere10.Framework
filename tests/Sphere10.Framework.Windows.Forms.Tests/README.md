# Sphere10.Framework.Windows.Forms.Tests

The focused `GlobalHotKeyReleaseTests` fixture verifies held-shortcut release,
rebinding and ordinary-command laziness. `ReadingGuideTests` verifies physical
bounds on each display, passive focus behaviour and clock lifetime. Both use
native Windows resources; run them separately from other desktop effect fixtures.

NUnit tests for the native WinForms application framework, screen hosting, controls, grids, charts and wizards. The project references [Sphere10.Framework.Windows.Forms](../../src/Sphere10.Framework.Windows.Forms/README.md), the DataVisualization chart library and the [WinForms tester](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md). Demo tests use the tester assembly and its resources directly. They do not compile private copies of its wizard or application configuration. The project also compiles the chart library's own test sources from `src/Sphere10.Framework.Windows.Forms.DataVisualization/Tests` as linked files.

## Run the tests

Run these commands from the repository root on Windows with the .NET 10 SDK. The project targets `net10.0-windows`, like the WinForms library it tests, and a machine with a .NET 11 SDK also builds and runs `net11.0-windows`. `dotnet test` runs every target; add `-f net10.0-windows` to run one.

```powershell
dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj -c Release -p:BuildRevision=0
```

Run the consumer configuration checks by themselves:

```powershell
dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~DemoPluginRegistrationTests"
```

Run the grid fixtures:

```powershell
dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~VisualGrid|FullyQualifiedName~SourceGrid"
```

Run the chart showcase fixtures separately:

```powershell
dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~ChartGallerySamplesTests|FullyQualifiedName~ChartGalleryScreenTests|FullyQualifiedName~ChartDemoTests|FullyQualifiedName~TelemetryChartDemoTests"
```

Set `SPHERE10_CHART_VALIDATION_DIRECTORY` to an output directory before running these fixtures to retain their rendered PNGs. The tests use the real tester screens and chart renderer with simulated datasets. They do not collect hardware or network telemetry. See the [chart showcase walkthrough](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md#chart-showcases) for the user-facing scenarios and the full chart-family catalog. `SPHERE10_WINFORMS_SCREENSHOTS` and `SPHERE10_LOG_OUTPUT_SCREENSHOTS` similarly retain screenshots from the fixtures that read them, and the `UiCaptureDirectory` test parameter does the same for the tab and toolbar layout fixtures.

## Requirements

- Windows with an interactive desktop session. Native UI fixtures run on STA threads and are `[NonParallelizable]` when they share framework startup or native window state.
- `VisualGridSelectionPaintingTests`, `WidgetStateEventTests` and `WindowHighlightScopeTests` send physical input or move the cursor. Run them on an idle desktop without other native UI fixtures or user input.
- `ChartExampleCompilationTests` is `[Explicit]` and needs an installed .NET SDK to compile the exported C# example.
- `MagnificationTests`, `DisplayColorEffectRendererTests` and `DisplayColorEffectExclusionTests` are `[Explicit]` because they create magnifier windows and change the desktop colour effect. Stop other magnification applications first.
- The fixtures need no external services or demo database credentials.

## Contents

### Project root

The project keeps all of its own fixtures at the root. `NativeTestEnvironment` is an NUnit `[SetUpFixture]` that enables visual styles before any fixture creates native controls.

| Fixture | Purpose |
| --- | --- |
| [AttachmentControlTests](AttachmentControlTests.cs) | Copied attachment contents, filename normalization, clear and empty-file semantics. |
| [AboutBoxLifetimeTests](AboutBoxLifetimeTests.cs) | Repeated hosted About calls respect transient and singleton container ownership. |
| [ActionMenuOwnerTests](ActionMenuOwnerTests.cs) | Action menu items receive the executing main form, execute-on-load callbacks and contextual selection. |
| [ApplicationAggregateTests](ApplicationAggregateTests.cs) | One live shared application aggregate per native startup, block initialization, hidden unsaved screens and application command membership. |
| [ApplicationBannerResourceTests](ApplicationBannerResourceTests.cs) | Banner bitmaps are released on resize and disposal while borrowed icon providers survive. |
| [ApplicationBarThemeTests](ApplicationBarThemeTests.cs) | Block selector shapes (full-width buttons, inset tags) and colour styles (expando header, Soft Office) resolve from the explorer bar theme, its explicit overrides and the bar and main-form overrides, including Classic and the main form's navigation theme; the settings round-trip through theme XML and the buttons are accessible push buttons. |
| [ApplicationBarSelectionTests](ApplicationBarSelectionTests.cs) | Navigation bar selection, programmatic and clicked selection, rejected clicks and inactive block registration. |
| [ApplicationBlockAvailabilityTests](ApplicationBlockAvailabilityTests.cs) | Native block availability without a form: `AvailableWhen` builds an unevaluated `ActionApplicationBlock`, an ordinary block is always offered, availability is fixed once the retained block is built, and `CheckAvailability` guards its provider and the delegate before the block takes its menus. |
| [ApplicationControlServicesTests](ApplicationControlServicesTests.cs) | Controls resolve application services lazily, across startup, design surfaces, shutdown and restart. |
| [ApplicationExitTests](ApplicationExitTests.cs) | Application-owned exit confirmation, vetoes and retries, awaited finalization with a live disabled window, forced close and worker or UI-thread dispatch. |
| [ApplicationGraphTests](ApplicationGraphTests.cs) | Typed parent bridges, native menu and toolbar references, definition replacement and owner cleanup on host disposal. |
| [ApplicationHostRenderingTests](ApplicationHostRenderingTests.cs) | Native tab selection, undocking and redocking, keyboard selection and close glyphs with one update per transition. |
| [ApplicationIconProviderTests](ApplicationIconProviderTests.cs) | Default and explicit application icon providers and their precedence. |
| [ApplicationLifecycleTests](ApplicationLifecycleTests.cs) | Keyed plugin tasks, process tasks, startup failure cleanup, UI-thread continuations and shutdown tasks before window closure. |
| [ApplicationPluginTests](ApplicationPluginTests.cs) | Loaded plugin receipts, block contributions, service-only plugins, direct blocks and live membership. |
| [ApplicationPresentationAdapterTests](ApplicationPresentationAdapterTests.cs) | Native menus, blocks and models project through the shared presentation contracts and keep their ownership. |
| [ApplicationScreenDockingTests](ApplicationScreenDockingTests.cs) | Dock targets for detached screen windows, header geometry and per-monitor DPI. |
| [ApplicationScreenFormTests](ApplicationScreenFormTests.cs) | Detached screen window caption, resizing, toolbar collapse, handle recreation and maximize behaviour. |
| [ApplicationScreenHostTests](ApplicationScreenHostTests.cs) | Screen host disposal guards, single and multi-instance screens and snapshot arrays. |
| [ApplicationScreenTabTests](ApplicationScreenTabTests.cs) | Tab drag reordering, Escape cancellation, capture loss and undocking from the tab bar. |
| [ApplicationSettingsEditorSelectionTests](ApplicationSettingsEditorSelectionTests.cs) | Automatic and explicit editors for screen-owned settings. |
| [ApplicationSettingsNativeTests](ApplicationSettingsNativeTests.cs) | Native settings identity, persistence, the Options dialog and model-refresh lifecycle. |
| [ApplicationSettingsResetTests](ApplicationSettingsResetTests.cs) | Options reset scopes, policies, confirmation and owner persistence. |
| [ApplicationSettingsStartupTests](ApplicationSettingsStartupTests.cs) | Settings restoration before native screen activation and the existing user settings provider. |
| [ApplicationSettingsStoreTests](ApplicationSettingsStoreTests.cs) | Typed provider records for native settings, encryption, transient state and failed or cancelled saves. |
| [ApplicationStartupScreenTests](ApplicationStartupScreenTests.cs) | Saved startup screen choices, stable keys, legacy choices and fallbacks. |
| [ApplicationToolbarLayoutTests](ApplicationToolbarLayoutTests.cs) | Toolbar row height, visibility changes, hosted controls and font changes keep content in place. |
| [AsyncModalTests](AsyncModalTests.cs) | Asynchronous modal and wizard cleanup on the owning UI dispatcher, and global shortcut cleanup. |
| [BuilderConvenienceTests](BuilderConvenienceTests.cs) | Inline builder overloads against native ownership and deferred activation. |
| [BuiltinApplicationCommandTests](BuiltinApplicationCommandTests.cs) | Designer-added menu and toolbar commands through the application command contract. |
| [CalendarRecurrenceEditorTests](CalendarRecurrenceEditorTests.cs) | The recurrence editor reads back the rule it shows, enables only the choices its frequency uses, keeps another frequency's choices out of the rule, raises its change event and explains an unusable rule. |
| [CalendarViewDataSourceTests](CalendarViewDataSourceTests.cs) | The visual calendar's day, week and month views over a resource schedule and over `CalendarViewDataSource`: a column per day or per calendar, overlaps side by side, an appointment's own column in events, reschedule requests with real new times that change nothing, the asynchronous refresh that lays a period out on a worker and shows only the latest request, and the tester screen's two schedules. |
| [CHMHelpProviderTests](CHMHelpProviderTests.cs) | CHM context help owner forms, default topics and viewer placement across monitors. |
| [ChartAssemblyConsolidationTests](ChartAssemblyConsolidationTests.cs) | Chart enums, designer metadata and XML serialization after the chart assembly consolidation. |
| [ChartDemoTests](ChartDemoTests.cs) | The four-chart Graph and Bar dashboard across scenarios and sizes, gaps versus zeros, presentation tools, capture commands and timer lifetime. |
| [ChartExampleCompilationTests](ChartExampleCompilationTests.cs) | Builds the exported C# chart factory with the installed SDK and checks data, thresholds and annotations (explicit). |
| [ChartGallerySamplesTests](ChartGallerySamplesTests.cs) | The chart gallery covers every native chart type, renders real images and supplies the correct multivalue shapes. |
| [ChartGalleryScreenTests](ChartGalleryScreenTests.cs) | The gallery screen's previews, filtering, paging, explorer options, PNG export and configuration recreation. |
| [ChartLogOutputTests](ChartLogOutputTests.cs) | Chart observations written to log output, timestamps, revisions and history release. |
| [ClipboardRuntimeTests](ClipboardRuntimeTests.cs) | Clipboard serialization dependencies load and Unicode text round-trips. |
| [ColorMatricesTests](ColorMatricesTests.cs) | Colour matrix identity, multiplication, contrast, interpolation, array conversion and image application. |
| [ComboBoxSelectionAppearanceTests](ComboBoxSelectionAppearanceTests.cs) | A focused toolbar combo box keeps a grey selection and native keyboard selection. |
| [ControlCatalogTests](ControlCatalogTests.cs) | Every exported control and form has one named catalogue example, and sample images are released. |
| [ControlRenderingScopeTests](ControlRenderingScopeTests.cs) | Redraw suppression scopes, nested scopes, reparenting, deferred layout and failure recovery. |
| [ControlStateEventProviderManagerTests](ControlStateEventProviderManagerTests.cs) | State event provider registration, lazy resolution, inheritance and construction failures. |
| [ControlStateEventProviderTests](ControlStateEventProviderTests.cs) | State event providers for common inputs, containers, combo boxes and split panels attach and detach correctly. |
| [VisualGridDropDownSizingTests](VisualGridDropDownSizingTests.cs) | CRUD reference dropdown popup dimensions, stretch columns, automatic page size, search and paging. |
| [VisualGridReferencePickerTests](VisualGridReferencePickerTests.cs) | CRUD reference picker binding, self references, clearing, required bindings and data source reads. |
| [VisualEntityEditorTests](VisualEntityEditorTests.cs) | The default entity editor's nested properties, cancel, circular references and change baselines. |
| [DemoLogOutputLifetimeTests](DemoLogOutputLifetimeTests.cs) | Application finalization joins accepted log output after its demo is destroyed. |
| [DemoPluginRegistrationTests](DemoPluginRegistrationTests.cs) | The tester's demo plugins, blocks, screens, application tasks and launch profiles through the native host. |
| [DesignerCompatibilityTests](DesignerCompatibilityTests.cs) | Forms and controls load as the root of a WinForms design surface with no framework running, and their designer files keep the generated shape. The covered types include `HexEditorControl`, the visual calendar controls, `CalendarRecurrenceEditor` and the tester's `VisualCalendarScreen`. |
| [DevAgeDrawingReuseTests](DevAgeDrawingReuseTests.cs) | Shared drawing calculations used by the imported shading, split button and task pane painting. |
| [DisplayFrameClockTests](DisplayFrameClockTests.cs) | The display frame clock's thread, rate, throttling, slow handlers and restart. |
| [DisplayColorEffectRendererTests](DisplayColorEffectRendererTests.cs) | Request ownership, capture membership, elapsed-time transitions and notification ordering through the native renderer (explicit). |
| [DisplayColorEffectExclusionTests](DisplayColorEffectExclusionTests.cs) | Window/app exclusions, opaque overlap, movement, visibility, mixed-DPI pixels, paused lifetime tracking, capture only while an excluded window is on screen, Store-app rules matched through the app inside its frame (opens Calculator), and restored compositor rendering (explicit). |
| [DrawingCacheTests](DrawingCacheTests.cs) | Brush and pen cache keys, reuse limits and disposal of cached and overflow resources. |
| [DrawingExtractionTests](DrawingExtractionTests.cs) | Drawing geometry, contrast colours, alignment and rounded rectangles extracted from SourceGrid. |
| [FixedLengthFieldListTests](FixedLengthFieldListTests.cs) | The fixed-length field dictionary's typed views, field identity and declared ordering. |
| [FixedLengthRecordTests](FixedLengthRecordTests.cs) | Fixed-length records round-trip with padding and reject oversized fields. |
| [FormWindowSettingsTests](FormWindowSettingsTests.cs) | Saved window bounds across monitor moves, DPI changes, removed monitors, size limits and invalid values. |
| [GlobalHotKeySetTests](GlobalHotKeySetTests.cs) | Global hot key sets register, re-register, release and report failures. |
| [GlobalHotKeyReleaseTests](GlobalHotKeyReleaseTests.cs) | Held-shortcut release, rebinding, callback thread and lazy tracking for ordinary commands. |
| [ReadingGuideTests](ReadingGuideTests.cs) | Physical bounds across displays, passive focus behaviour and visible/fixed clock lifetime (explicit). |
| [GraphChartTests](GraphChartTests.cs) | `GraphChart` snapshot ownership, rejected input, signed series, gaps, horizontal bars and empty states. |
| [HexEditorControlTests](HexEditorControlTests.cs) | Edits, save, find, document ownership, hit testing, the bytes-per-row list and dragging the line between the panes in the real hex editor control, view and form on an STA message loop. |
| [HexEditorDataInspectorTests](HexEditorDataInspectorTests.cs) | Data inspector integer, floating-point and character decoding in both byte orders. |
| [HexEditorDocumentTests](HexEditorDocumentTests.cs) | The hex document's bounded block cache, pending edits, structural changes, save and revert. |
| [HexEditorSearchTests](HexEditorSearchTests.cs) | Forward and backward streaming search across chunk and cache boundaries, pending edits and cancellation. |
| [HexEditorViewportTests](HexEditorViewportTests.cs) | Caret movement, selection, go-to, scrolling, overwrite typing and copy on the view model. |
| [HotKeyCommandEditorTests](HotKeyCommandEditorTests.cs) | The command shortcut editor's assignments, conflicts and grouping. |
| [HotKeyEditorTests](HotKeyEditorTests.cs) | The shortcut editor round trip, accepted keys, descriptions and Windows-key shortcuts. |
| [ImageComboBoxTests](ImageComboBoxTests.cs) | Image combo choices, commits, metrics, painting, DPI and the underlying custom combo drop-down. |
| [ImportedUtilityReuseTests](ImportedUtilityReuseTests.cs) | Imported property resolution and record separator parsing through their consumer APIs. |
| [LoadingCircleGalleryTests](LoadingCircleGalleryTests.cs) | The loading indicator gallery's bindings, cancellable examples and screen lifetime. |
| [LoadingCircleTests](LoadingCircleTests.cs) | Loading circle styles, colours, geometry, transparent painting and restoration of borrowed controls. |
| [LogOutputDestinationWizardScreenTests](LogOutputDestinationWizardScreenTests.cs) | Log destination wizard explanations, scaling and selection. |
| [LogOutputNavigationTests](LogOutputNavigationTests.cs) | Docked log captures keep their originating menu and release text and owners on close. |
| [LogOutputPreferencesTests](LogOutputPreferencesTests.cs) | Log output preference application, accepted actions, stop boundaries, legacy reads and remembered output. |
| [LogOutputSelectorControlTests](LogOutputSelectorControlTests.cs) | The log output selector's inactive logger, sizing, append mode, encoding and level options. |
| [LogOutputTransitionTests](LogOutputTransitionTests.cs) | Log output changes join pending writes, owner disposal and failure isolation between activations. |
| [LoggingTestScreenTests](LoggingTestScreenTests.cs) | The tester's logging screen sends each level to file output and the text box logger preview. |
| [MachineIdentifierRegistrationTests](MachineIdentifierRegistrationTests.cs) | The module registers one `WindowsMachineIdentifierGenerator` with the default sources as `IMachineIdentifierGenerator`, and a host's own registration, such as `BIOSMachineIdentifier`, wins. |
| [MagnificationTests](MagnificationTests.cs) | Fullscreen colour effects, magnifier windows and excluded overlays (explicit). |
| [MainFormExitTests](MainFormExitTests.cs) | Main form exit confirmation, vetoes, hide-on-close and window placement saving before closure. |
| [MergeableToolStripTests](MergeableToolStripTests.cs) | Reversible native tool strip merging by caption and explicit merge actions. |
| [MigratedNativeEditorTests](MigratedNativeEditorTests.cs) | Rich text conversion and the text editor's placeholder and validation after extraction from the grid bundle. |
| [MiscCompressionComparisonTests](MiscCompressionComparisonTests.cs) | The compression comparison utility's run, cancel and screen disposal. |
| [MonitorLayoutControlTests](MonitorLayoutControlTests.cs) | Monitor layout tiles, hit testing, painting and hosted per-tile or shared controls. |
| [NativeApplicationMenuBuilderPatternTests](NativeApplicationMenuBuilderPatternTests.cs) | Native menu builder screen and action shortcuts keep their metadata and validate registrations. |
| [NativeScreenCommandProviderTests](NativeScreenCommandProviderTests.cs) | Screen, plugin and block command projections into native strips for docked and detached windows. |
| [NativeTestEnvironment](NativeTestEnvironment.cs) | NUnit setup fixture that enables visual styles once for the whole run (helper, not a fixture). |
| [NavigationPaneTests](NavigationPaneTests.cs) | Navigation pane width tracking, restoration while hidden, DPI changes and maximum width. |
| [OnScreenNotificationFormTests](OnScreenNotificationFormTests.cs) | Passive on-screen notifications show near the screen edge and dispose after dismissal. |
| [PathSelectorControlTests](PathSelectorControlTests.cs) | A typed path is checked for existence on a worker when the box loses focus: the focus change is never held up, `PathChanged` follows only for a path that exists, a missing one sets `IsPathMissing`, and a path set from code discards a check still running. |
| [ProductUpdateDialogLayoutTests](ProductUpdateDialogLayoutTests.cs) | The update notice's content and layout prepared without showing it: a release-server update shows its release notes and download link, and an update without them, as the Windows Store's, gives them no space. |
| [ProductUpdateDialogTests](ProductUpdateDialogTests.cs) | The update notice on a real modal dialog: Cancel and Escape stop an installation and keep the notice open, closing cancels it, a decline in the channel's prompt keeps the notice open without an error, and an installation the updater took over closes the notice with its result (explicit, with a substituted updater). |
| [ProductUpdateInstallerTests](ProductUpdateInstallerTests.cs) | The update installer refuses a file name that is not a plain `.exe` or `.msi` name before downloading, holds the verified file against writing, replacing and deleting until the installer has been started, builds the direct and `msiexec` start commands, and removes a refused, failed, cancelled, unstarted or unlaunchable download. The transfer and the process start are substituted, so no BITS job, window or process is created. |
| [ProductUpdatePresenterTests](ProductUpdatePresenterTests.cs) | An update whose installer started requests exit and one the channel installed itself (the Windows Store's, without release notes) does not, a declined or failed notice leaves the application running, one notice at a time, the checker's announcement is posted to the user interface without being awaited, and an update that was refused or whose notice failed is offered again by the next check. No window is created. |
| [PropertyGridExTests](PropertyGridExTests.cs) | `PropertyGridEx` caption sizing, fitting, manual splitter positions and read-only selection. |
| [ReviewTextDialogTests](ReviewTextDialogTests.cs) | The text review dialog's preview, copy, save, encoding, replacement failure and cancellation. |
| [RoundedRectanglePathTests](RoundedRectanglePathTests.cs) | Rounded rectangle paths round only the chosen corners and limit large radii. |
| [ScreenActivationBuilderTests](ScreenActivationBuilderTests.cs) | Screen registrations own their metadata, startup activators, same-type registrations and cancellation. |
| [ScreenPolicyTests](ScreenPolicyTests.cs) | Shared default ordering, permanent singleton screens, legacy start flags and guarded mode changes. |
| [ScreenSettingsOwnershipTests](ScreenSettingsOwnershipTests.cs) | Native screens declare their own nested settings and keep existing provider record identities. |
| [ScreenWindowMenuTests](ScreenWindowMenuTests.cs) | The Screens menu tracks instances and detached windows, Close All and Close All Others. |
| [SearchTextBoxTests](SearchTextBoxTests.cs) | The search text box's clear action, layout, handle recreation and grid filter integration. |
| [SharedFluentBuilderIntegrationTests](SharedFluentBuilderIntegrationTests.cs) | Native plugins with shared nested configuration, cached block builds and type defaults. |
| [SharedScreenHostIntegrationTests](SharedScreenHostIntegrationTests.cs) | Shared and native screen host identity, block browsing, definition activation and owner validation. |
| [SourceGridControllerDispatchTests](SourceGridControllerDispatchTests.cs) | SourceGrid controller dispatch while callbacks change controller membership. |
| [SourceGridExporterTests](SourceGridExporterTests.cs) | SourceGrid CSV export escaping, framing, cell formatting and empty rows. |
| [SourceGridGalleryTests](SourceGridGalleryTests.cs) | The SourceGrid gallery's bindings, editors, exports, editing contract and sorting. |
| [SourceGridHtmlExporterTests](SourceGridHtmlExporterTests.cs) | SourceGrid HTML export of images, spans, styles, escaping and stream ownership. |
| [SourceGridModernizationTests](SourceGridModernizationTests.cs) | Typed SourceGrid collections keep their ordering, binding and ownership contracts. |
| [SourceGridResourceOwnershipTests](SourceGridResourceOwnershipTests.cs) | Print documents, cell editors and list editors own or borrow their resources correctly. |
| [SpecializedControlStateEventProviderTests](SpecializedControlStateEventProviderTests.cs) | State event providers for grids, toolbar choices, hosted inputs and nested menus. |
| [StartStopButtonTests](StartStopButtonTests.cs) | The start/stop tool strip button and plain button: one caption and glyph per state, and the action each click requests. |
| [TelemetryChartDemoTests](TelemetryChartDemoTests.cs) | The telemetry dashboard's units, plot geometry, timestamps, bounded history, cadence and timer lifetime. |
| [TelemetryChartTests](TelemetryChartTests.cs) | `TelemetryChart` point context, snapshot ownership, area gaps, timestamp hover and unit formatting. |
| [TesterControlShowcaseTests](TesterControlShowcaseTests.cs) | The tester's control showcase workflows and resource boundaries. |
| [TesterDataShowcaseTests](TesterDataShowcaseTests.cs) | The tester's data showcase settings, observable collections and worker cancellation. |
| [TesterIntegrationShowcaseTests](TesterIntegrationShowcaseTests.cs) | The tester's integration workbenches without mail, audio, hooks or network traffic. |
| [TextLogTests](TextLogTests.cs) | The text log control's construction, text, selection and append behaviour. |
| [ToggleSwitchTests](ToggleSwitchTests.cs) | The toggle switch's check behaviour, preferred size, DPI scaling and painting. |
| [ToolStripChoiceSelectorTests](ToolStripChoiceSelectorTests.cs) | The tool strip choice selector's models, keyboard selection, checks and disposal. |
| [ToolStripCommandMenuTests](ToolStripCommandMenuTests.cs) | The command menu's mirrored entries, separators, submenus, clicks through to the owning command and its empty entry. |
| [ToolStripItemExtensionsTests](ToolStripItemExtensionsTests.cs) | Tool strip image replacement disposal and icon rendering size. |
| [VisualCalendarAppearanceTests](VisualCalendarAppearanceTests.cs) | The tester's visual calendar screen rendered over a fixed schedule: font-based rows, the planning grid appointment style, unchanged appointment colours and the look each `VisualCalendarStyle` derives from the appointment colour. |
| [VisualGridArrayControllerTests](VisualGridArrayControllerTests.cs) | `VisualGrid` array controller binding, visible entity capture and selection across reloads. |
| [VisualGridAutoPageFillTests](VisualGridAutoPageFillTests.cs) | Automatic page fill with mixed row heights, fonts, resizing and DPI. |
| [VisualGridAutoPageSizeTests](VisualGridAutoPageSizeTests.cs) | Automatic page size mode, pager controls, owner values and the mode dropdown. |
| [VisualGridBackgroundPreparationTests](VisualGridBackgroundPreparationTests.cs) | Background row preparation off the owner thread, bounded allocation and stale result rejection. |
| [VisualGridColumnLayoutTests](VisualGridColumnLayoutTests.cs) | Column layout, scrollbars, header widths and image cells. |
| [VisualGridColumnsEditorTests](VisualGridColumnsEditorTests.cs) | The column collection editor publishes ordering and edits only on acceptance. |
| [VisualGridConfigurationContextMenuTests](VisualGridConfigurationContextMenuTests.cs) | Header configuration context menu attachment, targeting and ownership. |
| [VisualGridDataSourceControllerTests](VisualGridDataSourceControllerTests.cs) | Public grid data-source publication identity, stale snapshots, filtered export and ranged reads. |
| [VisualGridInteractionTests](VisualGridInteractionTests.cs) | Cell editing permissions, click selection and rapid row changes. |
| [VisualGridLiveDemoTests](VisualGridLiveDemoTests.cs) | The live grid demo's capture, timer publication, targeted refresh and disposal. |
| [VisualGridLiveInputTests](VisualGridLiveInputTests.cs) | Native clicks and context menus during suppressed and ordinary row preparation. |
| [VisualGridNativeInputRouteTests](VisualGridNativeInputRouteTests.cs) | Prepared refreshes keep the committed native input window visible to hit testing. |
| [VisualGridNotificationTests](VisualGridNotificationTests.cs) | Source change notifications refresh the grid silently, wait for drafts and detach on rebinding. |
| [VisualGridManualLoadingTests](VisualGridManualLoadingTests.cs) | The loading message an owner shows, replaces and hides over a grid, with and without disabling input. |
| [VisualGridPageModeMenuTests](VisualGridPageModeMenuTests.cs) | Page mode menu selection of single, automatic and manual paging. |
| [VisualGridPagingTests](VisualGridPagingTests.cs) | Locate across pages, manual and automatic page sizes and scrollbar allowance. |
| [VisualGridPreferencesTests](VisualGridPreferencesTests.cs) | Screen-supplied grid settings validation, restoration and column width and order round trips. |
| [VisualGridPreparedRowInvalidationTests](VisualGridPreparedRowInvalidationTests.cs) | Refreshing an unselected prepared row invalidates its reused native cell. |
| [VisualGridQuerySummaryTests](VisualGridQuerySummaryTests.cs) | Filtered totals across pages and rejection of stale query summaries. |
| [VisualGridReadOnlyCellTests](VisualGridReadOnlyCellTests.cs) | Read-only text cells, type converter formatting and opt-in row height reuse. |
| [VisualGridRefreshMouseSequenceTests](VisualGridRefreshMouseSequenceTests.cs) | A mouse press spanning a snapshot replacement does not gate movement or release. |
| [VisualGridRefreshTests](VisualGridRefreshTests.cs) | Row styles during preparation, grid titles and header layout during refresh. |
| [VisualGridRowDetailsTests](VisualGridRowDetailsTests.cs) | Hierarchy-source discovery, future graph nodes, custom panels, recursive tables, shared columns, selection retention, cancellation and owned-view teardown. |
| [VisualGridSelectionPaintingTests](VisualGridSelectionPaintingTests.cs) | Selection painting through prepared refreshes, including a physical mouse selection case. |
| [WidgetStateEventTests](WidgetStateEventTests.cs) | Committed state events and refresh boundaries on adopted numeric, colour, text, hex and list controls. |
| [WinFormsCommandOwnerTests](WinFormsCommandOwnerTests.cs) | Block and plugin command owner activation, fixed membership and disposal. |
| [WinFormsDefaultInterfaceDispatchTests](WinFormsDefaultInterfaceDispatchTests.cs) | Native plugin and decorator overrides project through the shared interfaces. |
| [WinFormsWizardAdapterTests](WinFormsWizardAdapterTests.cs) | The native wizard adapter's dynamic successors, validation, shared metadata and terminal callbacks. |
| [WindowHighlightScopeTests](WindowHighlightScopeTests.cs) | The window picker and highlight frame, owner hiding, consumed clicks and owner-thread disposal. |
| [WindowsWindowApplicationTests](WindowsWindowApplicationTests.cs) | The application a window shows: an ordinary window's own executable, and no answer for a window that has closed. |
| [WindowsTitleBarReaderTests](WindowsTitleBarReaderTests.cs) | A window's title-bar content read from the controls its accessibility tree reports, and from its own pixels when it reports nothing. |
| [WindowTitleBarButtonsTests](WindowTitleBarButtonsTests.cs) | A title-bar button's placement beside the caption buttons, moving left of title-bar content and staying put without room, its menu opening beneath it on every display, following its window, click reporting, restart and the own-window default. |
| [WindowsFormsToolTests](WindowsFormsToolTests.cs) | WinForms tool discovery and factories for charts, grids, inputs, pickers and review dialogs. |
| [WindowsInteropTests](WindowsInteropTests.cs) | Native structure layouts, rich edit formatting, cursor hotspots and device hook cleanup. |
| [WindowsWindowLayeringTests](WindowsWindowLayeringTests.cs) | Passive overlay detection above reference windows. |
| [WindowsReleaseServerProductUpdaterTests](WindowsReleaseServerProductUpdaterTests.cs) | The release server's Windows updater downloads, verifies and starts the installer with the update's arguments and owner window and requires exit, starts nothing after a failed download, maps BITS states to installation stages, and is registered by `UseProductUpdates` unless the host chose an updater. The transfer and the process start are substituted. |
| [WizardDemoTests](WizardDemoTests.cs) | The demo wizard's validation, model updates, back navigation and completion. |
| [XmlDigitalSignatureTests](XmlDigitalSignatureTests.cs) | XML digital signature signing, verification and lookup. |

### DataVisualization (linked sources)

The project compiles these files from the chart library's [Tests](../../src/Sphere10.Framework.Windows.Forms.DataVisualization/Tests) folder. They appear under `DataVisualization` in the project.

| Fixture | Purpose |
| --- | --- |
| [DataVisualizationCollectionTests](../../src/Sphere10.Framework.Windows.Forms.DataVisualization/Tests/DataVisualizationCollectionTests.cs) | Named chart element collection insert, remove, clear and rename. |
| [DataVisualizationKeywordTests](../../src/Sphere10.Framework.Windows.Forms.DataVisualization/Tests/DataVisualizationKeywordTests.cs) | Chart keyword replacement. |
| [DataVisualizationRenderingTests](../../src/Sphere10.Framework.Windows.Forms.DataVisualization/Tests/DataVisualizationRenderingTests.cs) | Renders a chart series to PNG. |

## Application model under test

The running model is `IApplication` → blocks → menus and screens. `LoadedPlugins` records successful startup contributors. Navigation uses `ScreenHost.ActiveBlock` and actual command, definition and screen references. Application lifecycle tasks are keyed by plugin name and run through `InitializeAsync` and `FinalizeAsync`. Native close requests go through `ExitAsync`, so shutdown tasks finish under the splash before window closure. Shared tests cover the plugin builder's name-before-registration guards and the directional sync and async base contracts in [Sphere10.Framework.Application.Tests](../Sphere10.Framework.Application.Tests/README.md).
