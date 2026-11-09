# Sphere10.Framework.Windows.Forms.Tests

NUnit tests for the native WinForms application framework, screen hosting, controls, grids, charts and wizards. The project references [Sphere10.Framework.Windows.Forms](../../src/Sphere10.Framework.Windows.Forms/README.md), the DataVisualization chart library and the [WinForms tester](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md). Demo tests use the tester assembly and its resources directly. They do not compile private copies of its wizard or application configuration. The project also compiles the chart library's own test sources from `src/Sphere10.Framework.Windows.Forms.DataVisualization/Tests` as linked files.

## Run the tests

Run these commands from the repository root on Windows with the .NET 10 SDK. A machine with a .NET 11 SDK also builds and runs the `net11.0-windows` target.

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
- `MagnificationTests` is `[Explicit]` because it creates magnifier windows and changes the desktop colour effect. Stop other magnification applications first.
- The fixtures need no external services or demo database credentials.

## Contents

### Project root

The project keeps all of its own fixtures at the root. `NativeTestEnvironment` is an NUnit `[SetUpFixture]` that enables visual styles before any fixture creates native controls.

| Fixture | Purpose |
| --- | --- |
| [AboutBoxLifetimeTests](AboutBoxLifetimeTests.cs) | Repeated hosted About calls respect transient and singleton container ownership. |
| [ActionMenuOwnerTests](ActionMenuOwnerTests.cs) | Action menu items receive the executing main form, execute-on-load callbacks and contextual selection. |
| [ApplicationAggregateTests](ApplicationAggregateTests.cs) | One live shared application aggregate per native startup, block initialization, hidden unsaved screens and application command membership. |
| [ApplicationBannerResourceTests](ApplicationBannerResourceTests.cs) | Banner bitmaps are released on resize and disposal while borrowed icon providers survive. |
| [ApplicationBarSelectionTests](ApplicationBarSelectionTests.cs) | Navigation bar selection, programmatic and clicked selection, rejected clicks and inactive block registration. |
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
| [CrudDropdownSizingTests](CrudDropdownSizingTests.cs) | CRUD reference dropdown popup dimensions, stretch columns, automatic page size, search and paging. |
| [CrudReferencePickerTests](CrudReferencePickerTests.cs) | CRUD reference picker binding, self references, clearing, required bindings and data source reads. |
| [DefaultCrudEntityEditorTests](DefaultCrudEntityEditorTests.cs) | The default entity editor's nested properties, cancel, circular references and change baselines. |
| [DemoLogOutputLifetimeTests](DemoLogOutputLifetimeTests.cs) | Application finalization joins accepted log output after its demo is destroyed. |
| [DemoPluginRegistrationTests](DemoPluginRegistrationTests.cs) | The tester's demo plugins, blocks, screens, application tasks and launch profiles through the native host. |
| [DevAgeDrawingReuseTests](DevAgeDrawingReuseTests.cs) | Shared drawing calculations used by the imported shading, split button and task pane painting. |
| [DrawingCacheTests](DrawingCacheTests.cs) | Brush and pen cache keys, reuse limits and disposal of cached and overflow resources. |
| [DrawingExtractionTests](DrawingExtractionTests.cs) | Drawing geometry, contrast colours, alignment and rounded rectangles extracted from SourceGrid. |
| [FixedLengthFieldListTests](FixedLengthFieldListTests.cs) | The fixed-length field dictionary's typed views, field identity and declared ordering. |
| [FixedLengthRecordTests](FixedLengthRecordTests.cs) | Fixed-length records round-trip with padding and reject oversized fields. |
| [FormWindowSettingsTests](FormWindowSettingsTests.cs) | Saved window bounds across monitor moves, DPI changes, removed monitors, size limits and invalid values. |
| [GraphChartTests](GraphChartTests.cs) | `GraphChart` snapshot ownership, rejected input, signed series, gaps, horizontal bars and empty states. |
| [HotKeyEditorTests](HotKeyEditorTests.cs) | The shortcut editor round trip, accepted keys, descriptions and Windows-key shortcuts. |
| [ImportedUtilityReuseTests](ImportedUtilityReuseTests.cs) | Imported property resolution and record separator parsing through their consumer APIs. |
| [LoadingCircleGalleryTests](LoadingCircleGalleryTests.cs) | The loading indicator gallery's bindings, cancellable examples and screen lifetime. |
| [LoadingCircleTests](LoadingCircleTests.cs) | Loading circle styles, colours, geometry, transparent painting and restoration of borrowed controls. |
| [LogOutputDestinationWizardScreenTests](LogOutputDestinationWizardScreenTests.cs) | Log destination wizard explanations, scaling and selection. |
| [LogOutputNavigationTests](LogOutputNavigationTests.cs) | Docked log captures keep their originating menu and release text and owners on close. |
| [LogOutputPreferencesTests](LogOutputPreferencesTests.cs) | Log output preference application, accepted actions, stop boundaries, legacy reads and remembered output. |
| [LogOutputSelectorControlTests](LogOutputSelectorControlTests.cs) | The log output selector's inactive logger, sizing, append mode, encoding and level options. |
| [LogOutputTransitionTests](LogOutputTransitionTests.cs) | Log output changes join pending writes, owner disposal and failure isolation between activations. |
| [LoggingTestScreenTests](LoggingTestScreenTests.cs) | The tester's logging screen sends each level to file output and the text box logger preview. |
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
| [PropertyGridExTests](PropertyGridExTests.cs) | `PropertyGridEx` caption sizing, fitting, manual splitter positions and read-only selection. |
| [ReviewTextDialogTests](ReviewTextDialogTests.cs) | The text review dialog's preview, copy, save, encoding, replacement failure and cancellation. |
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
| [TelemetryChartDemoTests](TelemetryChartDemoTests.cs) | The telemetry dashboard's units, plot geometry, timestamps, bounded history, cadence and timer lifetime. |
| [TelemetryChartTests](TelemetryChartTests.cs) | `TelemetryChart` point context, snapshot ownership, area gaps, timestamp hover and unit formatting. |
| [TesterControlShowcaseTests](TesterControlShowcaseTests.cs) | The tester's control showcase workflows and resource boundaries. |
| [TesterDataShowcaseTests](TesterDataShowcaseTests.cs) | The tester's data showcase settings, observable collections and worker cancellation. |
| [TesterIntegrationShowcaseTests](TesterIntegrationShowcaseTests.cs) | The tester's integration workbenches without mail, audio, hooks or network traffic. |
| [TextLogTests](TextLogTests.cs) | The text log control's construction, text, selection and append behaviour. |
| [ToolStripChoiceSelectorTests](ToolStripChoiceSelectorTests.cs) | The tool strip choice selector's models, keyboard selection, checks and disposal. |
| [VisualGridAutoPageFillTests](VisualGridAutoPageFillTests.cs) | Automatic page fill with mixed row heights, fonts, resizing and DPI. |
| [VisualGridAutoPageSizeTests](VisualGridAutoPageSizeTests.cs) | Automatic page size mode, pager controls, owner values and the mode dropdown. |
| [VisualGridBackgroundPreparationTests](VisualGridBackgroundPreparationTests.cs) | Background row preparation off the owner thread, bounded allocation and stale result rejection. |
| [VisualGridColumnLayoutTests](VisualGridColumnLayoutTests.cs) | Column layout, scrollbars, header widths and image cells. |
| [VisualGridColumnsEditorTests](VisualGridColumnsEditorTests.cs) | The column collection editor publishes ordering and edits only on acceptance. |
| [VisualGridConfigurationContextMenuTests](VisualGridConfigurationContextMenuTests.cs) | Header configuration context menu attachment, targeting and ownership. |
| [VisualGridInteractionTests](VisualGridInteractionTests.cs) | Cell editing permissions, click selection and rapid row changes. |
| [VisualGridLiveDemoTests](VisualGridLiveDemoTests.cs) | The live grid demo's capture, timer publication, targeted refresh and disposal. |
| [VisualGridLiveInputTests](VisualGridLiveInputTests.cs) | Native clicks and context menus during suppressed and ordinary row preparation. |
| [VisualGridNativeInputRouteTests](VisualGridNativeInputRouteTests.cs) | Prepared refreshes keep the committed native input window visible to hit testing. |
| [VisualGridPageModeMenuTests](VisualGridPageModeMenuTests.cs) | Page mode menu selection of single, automatic and manual paging. |
| [VisualGridPagingTests](VisualGridPagingTests.cs) | Locate across pages, manual and automatic page sizes and scrollbar allowance. |
| [VisualGridPreferencesTests](VisualGridPreferencesTests.cs) | Screen-supplied grid settings validation, restoration and column width and order round trips. |
| [VisualGridPreparedRowInvalidationTests](VisualGridPreparedRowInvalidationTests.cs) | Refreshing an unselected prepared row invalidates its reused native cell. |
| [VisualGridQuerySummaryTests](VisualGridQuerySummaryTests.cs) | Filtered totals across pages and rejection of stale query summaries. |
| [VisualGridReadOnlyCellTests](VisualGridReadOnlyCellTests.cs) | Read-only text cells, type converter formatting and opt-in row height reuse. |
| [VisualGridRefreshMouseSequenceTests](VisualGridRefreshMouseSequenceTests.cs) | A mouse press spanning a snapshot replacement does not gate movement or release. |
| [VisualGridRefreshTests](VisualGridRefreshTests.cs) | Row styles during preparation, grid titles and header layout during refresh. |
| [WidgetStateEventTests](WidgetStateEventTests.cs) | Committed state events and refresh boundaries on adopted numeric, colour, text, hex and list controls. |
| [WinFormsCommandOwnerTests](WinFormsCommandOwnerTests.cs) | Block and plugin command owner activation, fixed membership and disposal. |
| [WinFormsDefaultInterfaceDispatchTests](WinFormsDefaultInterfaceDispatchTests.cs) | Native plugin and decorator overrides project through the shared interfaces. |
| [WinFormsWizardAdapterTests](WinFormsWizardAdapterTests.cs) | The native wizard adapter's dynamic successors, validation, shared metadata and terminal callbacks. |
| [WindowHighlightScopeTests](WindowHighlightScopeTests.cs) | The window picker and highlight frame, owner hiding, consumed clicks and owner-thread disposal. |
| [WindowsInteropTests](WindowsInteropTests.cs) | Native structure layouts, rich edit formatting, cursor hotspots and device hook cleanup. |
| [WindowsWindowLayeringTests](WindowsWindowLayeringTests.cs) | Passive overlay detection above reference windows. |
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
