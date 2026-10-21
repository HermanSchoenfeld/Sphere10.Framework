# WinForms Tester

Build and run `Sphere10.Framework.Utils.WinFormsTester.csproj` on Windows. The application starts with `BlockMainForm.ScreenMode = ScreenMode.MultiView` configured through `UseMainForm`. It uses `HighDpiMode.DpiUnaware` so Windows bitmap-scales the complete interface, preserving the legacy screen and dialog layouts. Shared build settings keep `ForceDesignerDPIUnaware=true` for the Visual Studio designer. `UseMainFormSettings()` enables per-user window placement: resize or move the main window, close it through an accepted exit, then restart to restore its size, monitor, maximized state, and left menu width. Settings are written once by the framework shutdown task after accepted exit. The menu starts at 320 logical pixels and can expand to 480, with a smaller limit when the content needs room. Drag the divider to test your own width, including closing with the menu collapsed; moving or resizing does not write settings.

`Program.cs` loads two application plugins through `AddWinFormsApplicationPlugin`. **Workspace demonstrations**, configured by `WinFormsDemoPlugin.Configure`, contributes Workspace and Notes. **Component demonstrations**, configured by `WinFormsComponentDemoPlugin.Configure`, contributes Controls, Data And Collections, Integration, and Utilities. Both appear in `IWinFormsApplication.LoadedPlugins`; each block's `Source` identifies its contributing plugin. The application's live `Blocks` array supplies six independent bottom-left navigation buttons in the same order. Startup selects Settings from the Workspace block.

## Plugin lifecycle and splash

Each plugin calls `WithName(...)` before registering two initializers and two finalizers from the [Lifecycle folder](Lifecycle) through `RegisterInitializer<T>()` and `RegisterFinalizer<T>()`. Calling either helper, including its factory overload, before declaring a name throws `InvalidOperationException` immediately. Registrations use the built plugin's name as their service key; duplicate plugin names are rejected during loading.

| Plugin | Initializers | Finalizers |
|---|---|---|
| Workspace demonstrations | `DemoPluginInitializer`, `DemoWorkspaceInitializer` | `DemoPluginFinalizer`, `DemoWorkspaceFinalizer` |
| Component demonstrations | `DemoComponentInitializer`, `DemoIntegrationInitializer` | `DemoComponentFinalizer`, `DemoIntegrationFinalizer` |

The eight tasks derive from `AsyncApplicationInitializerBase` or `AsyncApplicationFinalizerBase` and override only the async operation; the directional bases provide their synchronous counterparts. Each logs its plugin and phase through `IApplication.Logger` around a cancellable `Task.Delay`. Startup therefore demonstrates four plugin initialization steps alongside the original process tasks; shutdown demonstrates four plugin finalization steps alongside the original process finalizers. The application's initializer and finalizer collections remain protected implementation details.

The application resolves and caches these tasks from its own service provider for successful `LoadedPlugins`. The combined inventory starts with host-owned application registrations, then plugins in `LoadedPlugins` order and each plugin's tasks in declaration order. Initializer priorities apply within the sequential and parallelizable phases; finalizers execute in combined inventory order. Building a plugin or resolving the application does not execute its tasks.

The runner starts the framework, installs the blocks, and awaits `application.InitializeAsync()` under the startup splash before showing the main form. `ModuleConfiguration.cs` retains its original unkeyed usage initializer, and the original process startup tasks and splash fades remain in their existing phases.

**File > Exit** and closing the main window defer to `application.ExitAsync()`. After confirmation and screen/block permissions succeed, the shutdown splash appears while the main window remains open with interaction disabled. It displays application finalization and framework shutdown tasks; the main window closes as the last step after those tasks, followed by service disposal and splash cleanup. Declining confirmation or a permission guard leaves the application running. Forced exit skips those decisions but still completes shutdown tasks. The application coordinates repeated requests and runs plugin tasks once.

Startup registration also rejects recursive attempts to load the same plugin name. If registration fails, newly added lifecycle task descriptors under that name are discarded so a later successful plugin cannot inherit abandoned tasks; ordinary `ConfigureServices` side effects retain their existing behavior.

Run from the repository root:

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.WinFormsTester -c Release -p:BuildRevision=0
```

## Demo organization

| Block | Demonstrations |
|---|---|
| Workspace | Screen Hosting, single/multi-view switching, navigation samples, and wizard. The empty workspace is a hidden screen definition in this block. |
| Notes | Permanent Notes and guarded removal of the Notes block. |
| Controls | Input controls, layout, and visual feedback. |
| Data And Collections | CRUD Grid, ObjectSpace, transactional/observable collections, Merkle trees, and Bloom filters. |
| Integration | Email, WebSockets, database connection controls, Windows input/hooks, sounds, and application services. |
| Utilities | Compression, cryptography, text/image/file transformations, logging output, user settings, scheduling, and visual inheritance. |

Choose a block from the bottom navigation buttons, then open a screen from its menus. Block selection changes the available navigation without changing the active screen. The former generic Block 1/Block 2 and repeated Opt entries are replaced by these functional groups; the original Screen A/B/C examples are under **Workspace > Navigation Samples** as **Calendar And Toolbar**, **Expandable Panels**, and **Task Pane Grids**.

The shared `IApplicationScreenHost` owns block registration, navigation and screen lifecycle. It exposes actual native controls by reference. The form's `IUserInterfaceServices` provides interactions and object-based menu execution through `ExecuteMenuItemAsync(item)`. Each item reaches its declaring block through `item.Parent.Parent`; each registered block reaches the application through `Parent` and its contributor through `Source`. Screen examples use their own `Host` and `Block` to navigate or close themselves, including when detached, and can log through `Block.Parent.Logger`. Typed hosting uses the generic base contract; native tab and docking commands remain on `IWinFormsApplicationScreenHost`. `SharedScreenHostIntegrationTests` verifies service identity, shared reference operations and native presentation behavior.

Blocks own their `ScreenDefinitions` independently of navigation. Each screen menu item references the same immutable activation metadata held by its block; the default is a `DefaultScreenDefinition` reference, and the definition's `Activators` array records its attached menu commands. Definitions carry lifetime and screen-kind policies directly. Native menus merge by their visible text and commands by title, with no string IDs in the shared model. The shared metadata classes are abstract `ApplicationBlockBase`, `ApplicationMenuBase<TItem>`, and `ApplicationMenuItemBase`; native `WinFormsApplicationMenuItemBase` and `WinFormsLinkMenuItemBase` remain abstract intermediates. Concrete screen and action items add images, placement, and resource ownership.

## Default, permanent, and empty screens

The normal profile selects **Settings** as the first declared default and also opens **Permanent Notes**. Select the permanent tab, type notes, and try normal screen switching: selection may change, but its instance remains. It has no close glyph; middle-click, context-menu close, undocking, and **File > Close screen** cannot remove it. Switch to SingleView and back to MultiView to verify that its notes and instance number survive.

Select the **Notes** block and choose **Remove Notes Block**. This uses the framework's guarded block removal. Its cancellation checkbox can prevent removal; clear it and retry. Then select **Workspace > Screen Hosting > Close Ordinary Screens**. The cached empty screen appears without a tab and reports `Kind: Empty`. Edit its notes, open Settings, and close the ordinary screens again: the same empty instance and notes return. A permanent or detached ordinary screen prevents the placeholder from appearing.

The alternative profile starts directly on a multi-instance empty screen and omits the Notes block:

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.WinFormsTester -c Release -p:BuildRevision=0 -- --empty-multi
```

Open Settings, then choose **Workspace > Screen Hosting > Close Ordinary Screens**. The returned placeholder has a new instance number and fresh notes because the previous instance was disposed on navigation. Both profiles use the same host and shared policies; `WinFormsDemoPlugin.Configure` sets the Workspace default, hidden placeholder lifetime, and optional Notes block together.

## Screen Hosting walkthrough

Ordinary screens use the framework's **MultiInstance** default: every menu activation opens an independent instance, including each **Workspace > Navigation Samples** item. **Settings** explicitly uses SingleInstance so repeated clicks reuse its tab. The permanent and empty-screen test profiles keep their declared policies.

1. In **Workspace > Screen Hosting**, click **Settings** repeatedly. Settings is a singleton screen type; the same tab and instance number remain selected.
2. Click **New Design** twice. Design is a multi-instance screen type; each tab has its own instance number, editable notes, counter, and event log.
3. Type different notes in each tab. Switch between tabs and use **Settings N** / **Design N > Actions**, or its counter toolbar button. Both must operate on the selected instance, and only its menu and toolbar should be merged into the main window.
4. Right-click a tab and choose **Undock**, or drag a selected tab away from the tab headers and release. Try the rightmost tab and the last remaining tab as well. Its notes and counter remain intact. The compact detached window shows its own **File** and **Actions** menus directly, and retains the screen's original toolbar. Check its caption icons for **Re-dock**, **Minimize**, **Maximize/Restore**, and **Close**. Minimize and restore through the taskbar, double-click the title to maximize/restore, and resize using the window edges. The main window continues to show the selected docked screen's controls, or an empty content panel when all screens are detached.
5. Undock Settings and click **Settings** in the main window's navigation again. This should focus the existing detached window without creating another tab.
6. Move the detached window's title bar close to the main tab strip. A highlighted tab-sized **Release to dock** preview and text hint show the insertion point and screen title, using the navigation pane's blue. The tabs should stay stationary without flickering while the pointer remains at that position. Moving the screen body over the workspace should not offer docking. Release to dock, or move away to dismiss the preview. The caption's **Re-dock** icon or **Ctrl+Shift+D** also returns it directly.
7. Reorder tabs by dragging along the tab bar. Tabs move while the mouse is held down; Escape restores the original position. Close tabs with their close button, middle-click, or context menu. Closing a detached window also closes that instance.
8. Select **Use SingleView** to close other ordinary open views and hide the tab bar. Permanent screens remain retained. **Use MultiView** shows the selected screen as a tab again. SingleView caches single-instance screens when navigating away and disposes multi-instance screens.
9. Check **Block switching away…** in a workspace. Attempts to leave, close, undock, redock, or remove that screen through a mode change should be cancelled. Clear the checkbox to continue. Watch the event log; the instance number and notes should survive cancelled operations.
10. Use the **sidebar icon** at the start of the main toolbar, or **Ctrl+Alt+M**, to collapse and restore the entire blue navigation menu. Its tooltip changes between **Hide sidebar** and **Show sidebar**. Its width and collapsed preference survive screen changes; no extra panel or gutter should surround the content when collapsed.
11. With cancellation unchecked, choose **File > Exit** or close the main window, then choose **Yes**. Watch the shutdown splash show both plugins' finalization messages while the main window remains behind it. The main window and detached windows close after shutdown work finishes. Choosing **No** keeps the application open. The confirmation uses native WinForms async modality.
12. Use **Rename tab...** in a screen's toolbar or **File** menu to try short, medium, and long titles. Tabs should grow to fit up to their maximum width, then show an ellipsis. Hover to read the full title. When detached, verify **File > Close screen** closes that screen and honors its cancellation checkbox.
13. Move the application and detached screens between monitors at different scaling settings (for example 100%, 150%, and 200%). Windows should bitmap-scale the complete windows. Check that navigation labels and dialog buttons remain visible and that tab titles, close buttons, docking previews, and menu collapse/restore retain their layout.
14. Open **Plain Screen (no bars)** and detach it. Its content should start directly below the caption, with no empty menu, toolbar, or tab row. Edit the notes, re-dock, and detach again to check that the same content survives. Also check a regular Settings or Design screen: only their actual menus and toolbar should occupy space.

The remaining demonstrations are grouped by function as listed above and can be opened side by side. **Integration > Connections > WebSockets** polls reports on a component-owned UI timer that stops when its handle is destroyed and resumes when recreated. Switching, detaching, re-docking, and closing that screen must not leave a callback invoking a missing handle. The **Integration > Windows Integration > Input Hooks** screen batches worker events for a UI timer, stops its hooks while its handle is unavailable, and releases hook subscriptions and resources on close or direct disposal. Reopening or re-docking it must not leave callbacks updating a disposed screen.

## Chart showcases

Open **Controls > Charts** for three complementary screens. Every dataset is
simulated; these examples do not read the computer's hardware, network or business
records. The gallery demonstrates the native chart engine, while the two
dashboards demonstrate reusable Framework controls over application-owned data.

| Screen | What to explore |
| --- | --- |
| **Data Visualization Gallery** | 39 examples: every one of the 35 native chart types, plus four examples combining axes, areas, logarithmic scales and annotations. |
| **Graph And Bar Charts** | A four-chart dashboard comparing the same three series as lines, translucent areas, grouped columns and horizontal bars. |
| **Live Telemetry** | Timestamped percent, bytes, bytes-per-second and Celsius histories, with different chart presentations and a shared simulated capture. |

### Data Visualization Gallery

The **Gallery** tab displays six actual chart previews per page. The first page
includes a funnel, radar, pie, columns, candlesticks and a dual-axis example.
Choose a **Chart Family**, then use **Previous** and **Next** to browse that
family. Click a chart or its **Explore Example** action to open the full-size
**Explore** tab. The example selector also switches charts within the current
family. Each example includes an explanation of the data and the chart's meaning.

| Chart family | Native types | Catalog |
| --- | ---: | --- |
| Trends | 8 | Line, Spline, StepLine, FastLine, Area, SplineArea, StackedArea, StackedArea100 |
| Comparisons | 6 | Column, Bar, StackedColumn, StackedColumn100, StackedBar, StackedBar100 |
| Composition | 4 | Pie, Doughnut, Funnel, Pyramid |
| Distribution | 5 | Point, FastPoint, Bubble, ErrorBar, BoxPlot |
| Ranges | 4 | Range, SplineRange, RangeColumn, RangeBar |
| Finance | 6 | Stock, Candlestick, Renko, ThreeLineBreak, Kagi, PointAndFigure |
| Circular | 2 | Radar, Polar |
| Advanced | — | Revenue And Growth: secondary Y axis; Price And Volume Panels: aligned chart areas; Orders Of Magnitude: logarithmic axis; Annotated Service Threshold: warning band and callout |

The first seven families cover all 35 `SeriesChartType` values; Advanced adds four
compositions. Examples supply the observation shape their renderer needs,
including high/low/open/close financial values, interval limits, bubble size,
confidence bounds and box-plot statistics.

In **Explore**:

1. Toggle **Legend** and **Value Labels**, then select a palette. **Example Colors**
   restores the example's original settings and colors. Other palettes replace
   the current series and point colors.
2. Enable **3D** on a supported example. The option is disabled when the selected
   renderer does not support this presentation.
3. Hover for values and click a data point to display its series, category or X
   coordinate, and supplied Y values below the chart. Multivalue observations
   retain all their values.
4. Enable **Drag To Zoom** on a numeric chart, drag a selection, and use the axis
   scrollbars. **Reset Zoom** restores the complete range. Circular and
   part-to-whole charts disable this option.
5. Choose **Save PNG** to render the current full-size explorer chart and write
   the selected file asynchronously. The gallery preview grid is not exported.
6. Choose **C# Example** to review, copy or save a pasteable `CreateExampleChart`
   factory. It uses `Sphere10.Framework.Windows.Forms.DataVisualization` and embeds
   the current chart's complete XML configuration and sample data, including
   multiple axes, chart areas and annotations. This recreates the configured
   chart rather than supplying only a chart-type assignment.
7. Use **Reset Example** to restore the selected example's original data and
   presentation. Resize, detach and re-dock the screen to inspect the same native
   chart at different sizes.

The catalog and rendering helpers are in
[ChartGallerySamples](Screens/Charts/ChartGallerySamples.cs) and
[ChartGalleryRenderer](Screens/Charts/ChartGalleryRenderer.cs); the real screen is
[ChartGalleryScreen](Screens/ChartGalleryScreen.cs). These are new Framework
demonstrations with generated datasets. The native renderer comes from the
Microsoft/.NET Foundation DataVisualization project through the retained upstream
continuation; its original notices and licensing remain in the
[DataVisualization project](../../src/Sphere10.Framework.Windows.Forms.DataVisualization/README.md)
and [provenance record](../../src/Sphere10.Framework.Windows.Forms.DataVisualization/PROVENANCE.md).

### Graph And Bar Charts

The [Graph And Bar Charts screen](Screens/ChartTestScreen.cs) starts with 48
observations in each of three series. The upper charts show the complete retained
history as **Line** and **Area**; the lower **Column** and **Bar** charts compare
the latest six labeled periods in groups. All four controls remain present while
the scenario changes.

| Scenario | Data and presentation |
| --- | --- |
| Service Activity | Web requests, API calls and background jobs across simulated minutes, with a zero-based requests-per-minute scale. |
| Quarterly Revenue | Subscriptions, professional services and licensing across fiscal quarters, with currency formatting and grouped quarter comparisons. |
| Signed Cash Flow | Operating, investing and financing values above and below zero; both positive and negative amounts share the comparison scale. |
| Missing Observations | Rainfall at three stations; unavailable readings remain gaps, while measured dry days retain a real zero. |

Choose a **Scenario** and an interval of **250 ms**, **500 ms**, **1 sec** or
**2 sec**. **Add Sample** advances once; **Live** repeats only while the screen is
visible. Each series retains at most 120 observations. Hiding the screen pauses
the demo timer, showing it resumes a selected Live capture, and disposal releases
the timer.

Use **Legends**, **Value Labels**, **Tooltips** and **Fixed Range** to compare
presentations. Value labels apply to the category comparisons; disabling Fixed
Range lets the chart determine its Y bounds. **Missing Samples** starts a new
capture with or without simulated outages. **Reset Capture** restores the seeded
scenario. **Clear** pauses Live and removes all four datasets, showing their
centered **NO DATA** state; Add Sample starts filling them again. The status line
distinguishes captured, retained, missing and real-zero observations.

### Live Telemetry

The [Live Telemetry screen](Screens/TelemetryChartTestScreen.cs) presents four
reusable `TelemetryChart` controls over the same capture timestamps:

| Chart | Demonstrated behavior |
| --- | --- |
| Processor | Percent values, a filled area, current-reading header and fixed 0–100% scale. |
| Memory | Byte values shown on an automatic GiB scale with a line history. |
| Network Throughput | Bytes per second, compact area presentation, and formatted MiB/s hover values. |
| Temperature | Celsius values including a simulated sub-zero calibration sweep, preserving signed measurements. |

The initial display contains 80 samples. **Add Sample**, **Live** and the interval
selector control the shared simulated capture; each history retains 180 points.
**Gaps** compares continuous signals with missing observations without changing
their capture timestamps. Hover to inspect a reading and its capture time.
**Reset Capture** starts a new empty capture while retaining the Live selection;
**Clear** empties the charts and pauses Live. The timer runs only while the screen
is visible, has a handle and Live is selected; closing the screen disposes it.

In both dashboards, the screen owns capture and retention. The chart controls
render supplied snapshots and do not collect telemetry themselves.

## CRUD Grid walkthrough

Choose **Data And Collections > Data Editing > CRUD Grid** to open a fresh screen each time; each tab has its own data and grid settings.

The **Editable Records** tab retains the employee editor and reference-picker demonstrations. Its options scroll independently when the window is short. Enable **Allow cell editing** and **CanUpdate** to edit **Address (Street)** and **Notes** immediately. The street editor creates an address when needed and preserves its other fields, which remain visible in **Location**. Turning editing or update capability off also closes an active cell editor.

Enable **Left click to deselect** to select an unselected row with one click and clear a selected row with the next click, including slow presses and quick switches between rows. When **Allow cell editing** is also enabled, double-click the editable cell or press **F2** to edit while keeping its row selected. A single click on a checkbox selects or deselects its row; double-click or **F2** changes its value.

The manual **Page Size** accepts **1–9999** in steps of one. **Delete** appears only when deletion is allowed and a row is selected.

The footer dropdown selects **Paged**, **Auto Paged** or **Single Paged**. Paged shows the
manual size and navigator; Auto Paged fits complete rows and keeps the navigator while
hiding the manual size; Single Paged displays all matching rows in one scrollable page.
The footer returns to one compact line when the screen has enough width, including
after narrowing and widening it. Columns keep their widths across page changes
and refresh; right-clicking a data row selects it before its context menu opens.

Enable **Suppress loading indicator** to refresh the main grid and dropdown
immediately without the spinner or temporary disabling. Use **Refresh Grid** to
compare this with the default loading feedback. Pending reads still join and
retain their stale-query protection in either mode.

The **Live Snapshots** tab demonstrates a separate read-only Text/Image grid backed by `RefreshableSnapshotDataSource<T,TKey>` and `CrudGridSnapshotController<T>`. **Capture Snapshot** creates 500 immutable synthetic rows on a worker, including a deliberate asynchronous capture delay. No operating-system telemetry or files are read. **Live Updates (750 ms)** repeats capture while this workspace is visible; disposal stops the timer and cancels pending collection. The status line shows the capture and UI thread IDs. Employee sample generation also runs on a worker before its first binding.

Live updates default to `SuppressLoadingIndicator = true`, `PrepareReadOnlyCellsInBackground = true` and `AutoSizeRowsOnRefresh = false`. Keep clicking different rows or resizing while capture runs: the committed grid remains enabled, compatible native cell slots are reused, and stable row IDs transfer selection to replacement instances. **Show Loading Feedback**, **Prepare Cells On Worker**, and **Measure Rows On Each Refresh** let you compare the supported paths. The first binding still uses the grid's normal full-size loading surface.

Select a row and choose **Refresh Selected Row** to replace only that row through the source's item-refresh contract. **Simulate Disappearance** pauses automatic capture and returns null for the selected identity, removing that row while retaining the remaining page; the next complete capture restores it. Search, sorting and paging query the published snapshot rather than capturing again. The **Page Mode** selector demonstrates all three modes; each grid's footer remains usable directly. Resize at different Windows scaling settings to inspect Auto Paged fitting with the deliberately mixed 18, 28 and 40 pixel images, horizontal scrollbar space and complete rows. Sort a short header such as **ID** to check that its indicator has reserved space, then resize a column and confirm its width survives later captures.

Use **Title** to try left, center and right aligned grid titles. **Image Alignment** offers all nine Drawing `ResizeAlignment` values; **Image Layout** switches between original size and the four Drawing `ResizeMethod` modes. Image columns start centered at their original size, and the sample screen owns the images for the grid's lifetime. **Select On Mouse Up** and **Left Click To Deselect** exercise selection timing and toggling during those same background updates. The employee grid continues to demonstrate editable checkbox/date/currency cells, row context commands, owned properties and the existing CRUD reference dropdown.

Open a record's edit dialog and expand **Address** to edit Street, City, State, and PostCode. For a missing address, choose **(Create new)** first. **Manager** opens a read-only CRUD dropdown configured at 520 × 300 containing the full employee data source and showing only **ID**, **Name**, and **Unsigned Int Field**. The same picker opens from an inline Manager cell. The grid fills the dropdown; **Name** is configured with `ExpandsToFit = true` to take spare width, while ID and Unsigned Int Field keep their content widths. Searching and changing pages keep the popup size stable. Resize it to check that Name follows the available width and automatic page sizing fits complete rows; selecting a row assigns that employee, **Clear** removes the manager, and **Cancel** or Escape keeps the previous value. Manager does not expand recursively. **Cancel** restores the original nested values and shared object references.

## Logging output walkthrough

Choose **Utilities > Application Utilities > Logging**. On first use the compact **Log Output** checkbox starts unchecked and exposes a `NoOpLogger` while off. Checking it opens a wizard: select **File**, **Application**, **Standard Output** or **Standard Error**, configure the applicable file/rolling options and log formatting, then review and Finish. Cancellation leaves output unchanged and creates no new output file. Uncheck to stop; check again to reuse the accepted configuration. **...** (Configure Log Output) opens a details dialog, and **Change** launches the wizard without stopping output just to edit its options.

Choose **Remember After Close** in the formatting step to restore the accepted configuration and enabled choice the next time this screen opens. Unchecking Log Output before closing remembers Off while retaining those options. Leaving it on remembers On; the screen binds its application logger before explicitly restoring the selected output. Disabling Remember deletes only this logging selector's preferences. File output that cannot be reopened stays off and exposes the failure so the **...** button can repair it.

Choose a level (**Debug**, **Info**, **Warning**, **Error** or **Exception**), enter a message and click **Send**. The selected output receives the message according to its configured level filters. **Application** uses the actual owning application's logger. Standard streams are visible when the process has a console or redirected stream. File output appends to the chosen file; rolling output retains the selected total file count, including the active file.

The **TextBoxLogger Preview** receives each sent message independently of the output checkbox. It demonstrates the existing timestamped asynchronous `TextBoxLogger` alongside the new reusable selector. **Clear Preview** clears only that textbox. Unchecking Log Output completes file writes and closes its owned file; the preview continues to work. Closing the screen disposes the selected output. The same public `LogOutputSelectorControl` is available from the WinForms library to other applications.

## Demo Wizard walkthrough

Choose **Workspace > Wizard > Demo Wizard**. The first step requires a name and the confirmation checkbox; the age step requires a non-negative whole number, including zero. Invalid **Next** displays validation errors and keeps the current step and model unchanged. Accepted **Next** saves the input. Use **Previous** from the age step to revisit and edit the name. The third step hides Previous as its instructions describe. The confirmation screen shows the collected name and age, and **Finish** passes that same model to the completion action.

Automated coverage for hosting, dragging, navigation, exit, native async dialogs, CRUD grids, and wizard validation and collection is in `tests/Sphere10.Framework.Windows.Forms.Tests`:

```powershell
dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj -c Release -p:BuildRevision=0
```

`DemoPluginRegistrationTests` runs the actual two-plugin configuration and verifies all four initializers and four finalizers execute once, report through the application's logger, and remain separate from unkeyed framework tasks. `WinFormsApplicationLifecycleTests` and `WinFormsApplicationExitTests` cover splash ordering, permissions, confirmation, cancellation and finalization before the real window closes. See the [test fixture guide](../../tests/Sphere10.Framework.Windows.Forms.Tests/README.md).

`ScreenPolicyTests` covers ordered defaults, permanent close/undock protection, guarded block registration/removal, view-mode retention, and tabless Empty screen lifetimes. Run it independently with `--filter FullyQualifiedName~ScreenPolicyTests` appended to the test command above.


The application-level **Views > Toggle Screen Mode** command is also a toolbar button. It remains available when switching blocks, uses the guarded native host transition and toggles between the live single/multi-view implementations. Try it with a screen's cancellation checkbox selected to verify that vetoed changes leave the workspace intact.
