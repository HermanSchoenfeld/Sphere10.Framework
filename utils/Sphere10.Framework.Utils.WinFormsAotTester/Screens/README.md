# Screens

The tester's demonstration screens, with the sample entities, data sources, helper controls and streams they use.

Each demonstration is an `ApplicationScreen`, except the few forms and user controls noted in the table. `WinFormsDemoPlugin` registers the Workspace and Notes screens, which exercise screen hosting, activation modes, empty and permanent screens and the navigation samples. `WinFormsComponentDemoPlugin` registers the Controls, Data And Collections, Integration and Utilities blocks. Both plugins are in the project folder. A screen that remembers preferences declares a nested `SettingsObject` subclass and overrides `ScreenSettings` to return it. Those nested settings types, enums and records are not listed below. Screens that write chart or log output register it with `DemoLogOutputLifetime` from [Lifecycle](../Lifecycle/README.md), the wizard pages are in [Wizard](../Wizard/README.md), and the images and sounds come from [Resources](../Resources/README.md).

The sample types here are test data rather than reusable components. `Employee`, `Address` and `TestCrudDataSource` feed the Visual Grid and CRUD examples, `MyAppointmentBookDataSource` feeds the appointment book, and `SyntheticByteStream` gives the hex editor a 512 MiB document without allocating it. `ComponentRegistryScreen.cs` and `TestLauncherScreen.cs` are fully commented out and declare no types. `FakeTransparentForm` and `DataSource1<TItem>` are not referenced by any screen.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Address`](Address.cs) | class | Street, city, post code and state values of the sample `Employee`. |
| [`ApplicationServicesTestScreen`](ApplicationServicesTestScreen.cs) | class | Lists the owning application, its logger and each block's screen registrations, and writes a message through the application logger. |
| [`AppointmentBookScreen`](AppointmentBookScreen.cs) | class | Appointment book with an unallocated-work tray beside it over `MyAppointmentBookDataSource`, using a pastel category palette and logging selection, drag and resize events. |
| [`BlockSelectorStylesScreen`](BlockSelectorStylesScreen.cs) | class | Switches the window's block selector shape and colour style live and shows every combination side by side on sample task panes. |
| [`BloomFilterAnalysisScreen`](BloomFilterAnalysisScreen.cs) | class | Compares `BloomFilterMath` length and error estimates for an item count, error rate and round count in bit and byte units. |
| [`CBACSVConverterScreen`](CBACSVConverterScreen.cs) | class | Rewrites the dates of a bank CSV export through the Framework `CsvReader` without breaking quoted fields. |
| [`ChartGalleryScreen`](ChartGalleryScreen.cs) | class | Browses the `ChartGallerySamples` data-visualization charts by family, with exploration, zoom, palettes, PNG export and C# example generation. |
| [`ChartTestScreen`](ChartTestScreen.cs) | class | Live and manual samples in `GraphChart` line and area charts and `BarChart` column and bar charts, with scenarios and optional gaps. |
| [`CompressionTestScreen`](CompressionTestScreen.cs) | class | Compresses and decompresses a chosen file in owned temporary files off the UI thread and compares the content hashes. |
| [`ConnectionBarTestScreen`](ConnectionBarTestScreen.cs) | class | Hosts a `DatabaseConnectionBar` and tests its connection only on request. |
| [`ConnectionPanelTestScreen`](ConnectionPanelTestScreen.cs) | class | Hosts a `DatabaseConnectionPanel` and tests its connection only on request. |
| [`ControlCatalogEntry`](ControlCatalogEntry.cs) | class | One catalog example, holding the control type, category, instructions and a factory that creates the example only when selected. |
| [`ControlCatalogHostedScreen`](ControlCatalogHostedScreen.cs) | class | Isolated screen that the catalog hosts to demonstrate screen-host and detached-window controls. |
| [`ControlCatalogSamples`](ControlCatalogSamples.cs) | static class | Builds the explicit list of `ControlCatalogEntry` examples for every public native control and form. |
| [`ControlCatalogScreen`](ControlCatalogScreen.cs) | class | Searchable catalog that creates each control example on selection, with a live property grid. |
| [`CustomComboBoxScreen`](CustomComboBoxScreen.cs) | class | `CustomComboBox` dropdowns hosting a table, a composite editor and rich text. |
| [`DecayGaugeScreen`](DecayGaugeScreen.cs) | class | `DecayGauge` driven by manual events and by optional keyboard and mouse hooks. |
| [`DraggableControlsTestScreen`](DraggableControlsTestScreen.cs) | class | Picture boxes made movable with the `Draggable` control extension. |
| [`EmailTestScreen`](EmailTestScreen.cs) | class | Composes and sends SMTP messages through `Tools.Mail`, keeping credentials and drafts out of saved preferences. |
| [`Employee`](Employee.cs) | class | Sample entity with name, dates, address, salary, supervisor flag and manager, used by the grid and CRUD examples. |
| [`EncryptedCompressionTestScreen`](EncryptedCompressionTestScreen.cs) | class | Password-protected compression round trip of a chosen file in owned temporary files, verified by content hash. |
| [`EnumComboScreen`](EnumComboScreen.cs) | class | `EnumComboBox` choices with description attributes and an explicit empty option. |
| [`ExpandoTesterScreen`](ExpandoTesterScreen.cs) | class | Animated `ExplorerBar` and `Expando` groups holding commands, filters and tree navigation. |
| [`ExplorerBarDesignerScreen`](ExplorerBarDesignerScreen.cs) | class | Edits a complete explorer bar theme, including its block selector, in a property grid; previews it on a task pane above sample block buttons, starts from Luna or Classic, applies it to the window's navigation sidebar and exports it as XML. |
| [`ExplorerBarSettings`](ExplorerBarDesignerScreen.cs) | class | Flat property-grid model of every explorer bar theme setting, converted to and from `ExplorerBarInfoSurrogate` by `ExplorerBarDesignerScreen`. |
| [`ExplorerBarThemeExportForm`](ExplorerBarThemeExportForm.cs) | class | Shows an exported explorer bar theme as XML for copying. |
| [`FakeTransparentForm`](FakeTransparentForm.cs) | class | Form that imitates transparency by copying the screen behind it into its background image. |
| [`FlagsCheckedBoxListScreen`](FlagsCheckedBoxListScreen.cs) | class | `FlagsCheckedListBox` with single, combined and zero-valued flags and a live bit-mask readout. |
| [`HexEditorTestScreen`](HexEditorTestScreen.cs) | class | `HexEditorControl` over a file, a small in-memory sample and a 512 MiB `SyntheticByteStream`. |
| [`HooksScreen`](HooksScreen.cs) | class | Starts and stops owned `WindowsKeyboardHook` and `WindowsMouseHook` observation with bounded output. |
| [`ImageResizeScreen`](ImageResizeScreen.cs) | class | Compares image resizing and alignment modes, disposing each generated bitmap. |
| [`LoadingCircleTestScreen`](LoadingCircleTestScreen.cs) | class | `LoadingCircle` styles with live appearance settings and animations scoped to the work they show. |
| [`LoggingTestScreen`](LoggingTestScreen.cs) | class | Writes sample messages and exceptions to the output chosen in a `LogOutputSelectorControl` and a text-box preview logger. |
| [`MerkleTreeTestScreen`](MerkleTreeTestScreen.cs) | class | Builds Merkle trees, shows partition arithmetic and runs cancellable comparisons of the tree implementations. |
| [`MiscTestScreen`](MiscTestScreen.cs) | class | Assorted experiments with list operations, scoped contexts, AES, compression, path templates, database creation, MAC addresses and member mapping. |
| [`MyAppointmentBookDataSource`](MyAppointmentBookDataSource.cs) | class | Synthetic in-memory `IAppointmentBookDataSource` of resources and appointments. |
| [`ObjectSpaceScreen`](ObjectSpaceScreen.cs) | class | Creates and edits a memory-stream ObjectSpace of sample accounts and identities, and saves or reopens it as a snapshot file. |
| [`ObjectSpaceScreenBase`](ObjectSpaceScreenBase.cs) | class | Designer base that types `ScreenSettings` as the ObjectSpace screen's nested settings class. |
| [`ObservableCollectionsTestScreen`](ObservableCollectionsTestScreen.cs) | class | Logs single-item and range notifications from an `ObservableExtendedList` and an `ObservableDictionary` beside their values. |
| [`PadLockTestScreen`](PadLockTestScreen.cs) | class | `PadLockButton` that toggles a text box between read-only and editable and reports its state events. |
| [`ParagraphBuilderScreen`](ParagraphBuilderScreen.cs) | class | Formats sentences and paragraph breaks through `ParagraphBuilder`. |
| [`PasswordDialogTestScreen`](PasswordDialogTestScreen.cs) | class | Password dialog policies and a local asynchronous sign-in that never displays the entered credentials. |
| [`PathSelectorTestScreen`](PathSelectorTestScreen.cs) | class | The four `PathSelectorControl` modes and their validation. |
| [`PlaceHolderTestScreen`](PlaceHolderTestScreen.cs) | class | Sets cue text through the placeholder extender on standard text, combo, masked and rich text boxes, beside Framework editors such as `TextBoxEx`, `MoneyBox` and `SearchTextBox`. |
| [`RegionToolTestScreen`](RegionToolTestScreen.cs) | class | Browses installed cultures and their `RegionInfo` without changing the machine locale. |
| [`ScheduleTestScreen`](ScheduleTestScreen.cs) | class | Schedules `TestJob` and delegate jobs under selectable scheduler policies, with XML serialization and log output owned by the screen. |
| [`ScreenA`](ScreenA.cs) | class | Navigation sample with a calendar and a contextual toolbar owned by the screen instance. |
| [`ScreenB`](ScreenB.cs) | class | Navigation sample combining expandable task-pane groups with a `ListMerger`. |
| [`ScreenC`](ScreenC.cs) | class | Navigation sample with independently populated grids in expandable task-pane groups. |
| [`ScreenHostingDesignTestScreen`](ScreenHostingDesignTestScreen.cs) | class | Lifecycle screen that can add typed inspector preferences, two Visual Grids and a log output, used by the Design, CPU and GPU registrations. |
| [`ScreenHostingEmptyMultiTestScreen`](ScreenHostingEmptyMultiTestScreen.cs) | class | Empty-workspace screen created afresh each time the host needs its fallback. |
| [`ScreenHostingEmptyTestScreen`](ScreenHostingEmptyTestScreen.cs) | class | Single-instance empty-workspace screen that the host retains between uses. |
| [`ScreenHostingPermanentTestScreen`](ScreenHostingPermanentTestScreen.cs) | class | Permanent notes screen whose lifetime the host controls. |
| [`ScreenHostingPlainTestScreen`](ScreenHostingPlainTestScreen.cs) | class | Content-only screen for checking docking without menu or toolbar rows. |
| [`ScreenHostingPreferencesEditor`](ScreenHostingPreferencesEditor.cs) | class | `DefaultCrudEntityEditor` with a live preview, used as the GPU inspector's settings editor. |
| [`ScreenHostingSettingsTestScreen`](ScreenHostingSettingsTestScreen.cs) | class | Single-instance lifecycle screen registered as the Workspace default. |
| [`ScreenHostingTestScreen`](ScreenHostingTestScreen.cs) | class | Designable lifecycle screen shared by the instance, permanent and empty-screen examples, showing its instance number, show and hide events and a checkbox that cancels hiding. |
| [`SettingsTest`](SettingsTest.cs) | class | Screen preferences declared through attributes beside standalone user and global `Test1Settings` records. |
| [`SourceGridGalleryRow`](SourceGridGalleryRow.cs) | class | Editable row values shared by the raw SourceGrid binding examples. |
| [`SourceGridGalleryScreen`](SourceGridGalleryScreen.cs) | class | Raw SourceGrid cell, bound, virtual and planning grids, with CSV, HTML and image export. |
| [`SyntheticByteStream`](SyntheticByteStream.cs) | class | Seekable stream whose bytes are computed from their offset, with writes kept in a sparse overlay and read statistics. |
| [`TabControlTestControl`](TabControlTestControl.cs) | class | User control whose draft input survives tab changes. |
| [`TabControlTestScreen`](TabControlTestScreen.cs) | class | Tab pages holding nested `TabControlTestControl` instances and ordinary grouped controls. |
| [`TelemetryChartTestScreen`](TelemetryChartTestScreen.cs) | class | Simulated CPU, memory, throughput and temperature samples in four `TelemetryChart` controls, with interval and gap options. |
| [`Test1Settings`](Test1Settings.cs) | class | Standalone `SettingsObject` with database, language, run-count and timestamp values for the user and global persistence example. |
| [`TestArtificialKeysScreen`](TestArtificialKeysScreen.cs) | class | Builds `ArtificialKeys` database key descriptions and round-trips them through XML. |
| [`TestCrudDataSource`](TestCrudDataSource.cs) | class | `ListDataSource<Employee>` with search, sorting, random selection and switches that inject create, update and delete errors. |
| [`TestJob`](TestJob.cs) | class | `ISchedulerJob` that logs, sleeps for 500 ms and logs again, used by the scheduling examples. |
| [`TestSoundsScreen`](TestSoundsScreen.cs) | class | Plays the embedded click sounds through `SoundPlayer` and DirectSound, disposing every device, stream and buffer. |
| [`TextAreaTestsScreen`](TextAreaTestsScreen.cs) | class | Times large text appends with and without a deferred rendering scope and generates EPASA password characters and regular-expression samples. |
| [`TransactionalCollectionScreen`](TransactionalCollectionScreen.cs) | class | Writes to transactional and stream-mapped collections over memory, paged and file buffers, with chosen policies and explicit commit or rollback. |
| [`UrlIDTestScreen`](UrlIDTestScreen.cs) | class | Shows numbers beside their `UrlID` encodings in the selected format. |
| [`UserControl1`](CustomTestDropDownControl.cs) | class | Composite selection editor hosted in the custom dropdown example. |
| [`ValidationIndicatorTestScreen`](ValidationIndicatorTestScreen.cs) | class | `ValidationIndicator` states set manually and by completed asynchronous validation, updated on the UI thread. |
| [`VisualGridDetailSamplePanel`](VisualGridDetailSamplePanel.cs) | class | Designer-editable image-and-description panel shown as a Visual Grid row detail. |
| [`VisualGridDetailsDemo`](VisualGridDetailsDemo.cs) | class | Visual Grid with expandable row details holding recursive child grids, the sample panel and an editable text box. |
| [`VisualGridTestScreen`](VisualGridTestScreen.cs) | class | Employee CRUD in a `VisualGrid` with reference editors, live snapshots from a polling data source and saved grid preferences. |
| [`VisualInheritanceFixerBaseForm`](VisualInheritanceFixerBaseForm.cs) | class | Base form with designer-defined menu, toolbar and status strip items. |
| [`VisualInheritanceFixerSubForm`](VisualInheritanceFixerSubForm.cs) | class | Form derived from `VisualInheritanceFixerBaseForm` that adds its own menu, toolbar and status items. |
| [`WAMSTestScreen`](WAMSTestScreen.cs) | class | Compares WAMS and WAMS# signature sizes and signing and verification rates across hash, Winternitz and height parameters in background work. |

### Charts

| Type | Kind | Purpose |
|---|---|---|
| [`ChartGalleryRenderer`](Charts/ChartGalleryRenderer.cs) | static class | Applies a gallery sample and common styling to a `Chart`, and captures its XML configuration or a C# factory for it. |
| [`ChartGallerySample`](Charts/ChartGallerySample.cs) | record | Identifier, category, name, description and configuration delegate of one gallery chart. |
| [`ChartGallerySamples`](Charts/ChartGallerySamples.cs) | static class | Deterministic simulated datasets for each gallery chart, built only when the chart is selected. |
| [`ChartGalleryTile`](Charts/ChartGalleryTile.cs) | class | User control that previews one gallery sample and raises `ExploreRequested` when clicked. |

### Communications

| Type | Kind | Purpose |
|---|---|---|
| [`CommunicationsTestScreen`](Communications/CommunicationsTestScreen.cs) | class | Sends messages over TCP, UDP and WebSocket channels and serves `TestClass` rows through a server WebSocket data source. |
| [`DataSource1<TItem>`](Communications/TestDataSource.cs) | class | Paged in-memory `SyncBatchDataSourceBase<TestClass>` of 73 generated rows. |
| [`TestClass`](Communications/TestClass.cs) | class | Sample row with ID, name, colour, date, age and notes, filled with random test data. |
