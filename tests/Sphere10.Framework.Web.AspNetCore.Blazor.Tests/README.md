# Sphere10.Framework.Web.AspNetCore.Blazor.Tests

.NET 10 NUnit coverage for the Blazor library, its application shell and the local gallery samples. The project references both [the library](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) and [the tester](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md), because the sample routes, plugin definitions and grid data source live in the utility.

The default suite runs without a browser driver, external service, database or WebAssembly runtime. Component tests use ASP.NET Core's `HtmlRenderer`; managed interop lifecycle tests supply fake `IJSRuntime`/`IJSObjectReference` implementations. The explicitly selected `Browser` fixtures use Playwright against a running local tester.

## Run the tests

Install the .NET 10 SDK and run these commands **from the repository root**. `dotnet test` restores and builds the required projects before running NUnit. `BuildRevision=0` supplies an explicit local build revision.

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0
```

Run the grid implementation and sample-source fixtures together:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~BlazorGrid|FullyQualifiedName~GridDemoDataSource"
```

Run one fixture while developing:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~BlazorGridControllerTests"
```

For test discovery or a saved result file:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --list-tests
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --logger "trx;LogFileName=blazor-tests.trx" --results-directory obj/test-results/blazor
```

The `--filter` value is quoted so shell characters such as `|` remain part of the test expression. Do not add `--no-build` after changing source; it would run the previously compiled assembly.

## Grid regression coverage

| Fixture | What it protects |
| --- | --- |
| [BlazorGridControllerTests.cs](BlazorGridControllerTests.cs) | Full capability intersection, source-driven zero-based query paging, sequential reads and stale-result rejection, source replacement, selection, buffered edits, validation/write rollback, creation, deletion and page repair, changing permissions, disposal and source ownership. |
| [BlazorGridColumnTests.cs](BlazorGridColumnTests.cs) | Typed property factories, inherited/boxed accessors, metadata and read-only members, independent automatic definitions, scalar parsing/formatting, nullable/enum/date values, invariant culture and custom reference identity. |
| [BlazorGridRenderingTests.cs](BlazorGridRenderingTests.cs) | Capability-controlled commands, explicit edit mode, checkbox/read-only display, Save/Cancel, validation retry, create drafts, propertyless rows with legacy actions, parameter updates, reference-picker restrictions and managed JavaScript initialization/retry/disposal. |
| [BlazorGridBrowserTests.cs](BlazorGridBrowserTests.cs) | Explicit real-browser checks for settled grid geometry after fractional zoom/device scaling, row-three editing, nested reference popup and manual column resizing. |
| [BlazorGridReferencePickerTests.cs](BlazorGridReferencePickerTests.cs) | Compact accessible popover, dismissal and reopening generations, source replacement, disabled state, disposal and JavaScript import races. |
| [GridDemoDataSourceTests.cs](GridDemoDataSourceTests.cs) | Demo name/age validation, protected deletion, batch rejection before mutation, search, typed sort order and stable ID tie-breaks, page clamping, new-item persistence and related-record identity. |
| [GalleryRenderingTests.cs](GalleryRenderingTests.cs) | Common demo layouts/routes, separate modal hosts, scoped service isolation, navigation icons and the gallery's local grid integration. |
| [ThemeTests.cs](ThemeTests.cs) | Scoped theme isolation, validated changes and notifications, custom registrations and decorator behavior. |
| [ThemeRenderingTests.cs](ThemeRenderingTests.cs) | Provider attributes, accessible selector/toggle state, retained child/draft state, renderer dispatch and subscription disposal. |

Controller tests exercise `IDataSource<T>` behavior without rendering a component. Column tests isolate conversion and metadata. Rendering tests verify the connection between those contracts and actual component markup/callbacks. Sample-source tests cover the rules of `TestClassDataSource`, including searching fields that are not directly editable grid cells.

The interop cases check that concurrent loads share initialization, disconnected initialization releases its callback before retry, and disposal waits for pending initialization without updating the removed grid. Their fake runtime does not execute the JavaScript module.

## Application shell and recovered components

| Location or fixture | Coverage |
| --- | --- |
| [ApplicationBlockTests.cs](ApplicationBlockTests.cs), [ApplicationScreenHostTests.cs](ApplicationScreenHostTests.cs) | Registration/catalog behavior, activation policies, scoped actions, screen guards, host state, cancellation and block removal. |
| [PluginBuilderTests.cs](PluginBuilderTests.cs) | Fluent plugin/block configuration, independent metadata snapshots, composed startup callbacks, load-time definitions and menu subscribers, registration validation and circuit-scoped services. |
| [ApplicationRenderingTests.cs](ApplicationRenderingTests.cs) | Retained rendered components, lifecycle/disposal, bookmark/history resolution and overlapping asynchronous route changes. |
| [DemoPluginRegistrationTests.cs](DemoPluginRegistrationTests.cs) | Actual tester startup validates its complete service graph, registers both fluent plugins and their blocks once, supplies working gallery dependencies, and isolates action, screen and endpoint state between scopes. |
| [SearchInputTests.cs](SearchInputTests.cs) | Search result links, throttling, stale completion/error rejection, provider replacement, dismissal and pending-search disposal. |
| [EndpointSelectorTests.cs](EndpointSelectorTests.cs) | Restored endpoint selector registration, two-way server-page synchronization, circuit isolation, renderer dispatch and subscription disposal. |
| [BlockDockTests.cs](BlockDockTests.cs) | Compact menu compatibility, builder-supplied icons, scoped catalog changes, workspace route guards and subscription disposal. |
| [DemoNavigationTests.cs](DemoNavigationTests.cs) | Header search uses the shared catalog of current and original demo routes. |
| [ApplicationMenuEventTests.cs](ApplicationMenuEventTests.cs) | Menu event and callback behavior. |
| [SharedPresentationAdapterTests.cs](SharedPresentationAdapterTests.cs), [BlazorApplicationScreenContractTests.cs](BlazorApplicationScreenContractTests.cs) | Alias-free shared `Application.UI` adapters and explicit/public lifecycle dispatch through `IBlazorApplicationScreen`. |
| [ComponentParameterLifecycleTests.cs](ComponentParameterLifecycleTests.cs) | Parameter changes, stale provider responses, stream replacement, modal completion, cancellation during host teardown and disposal and final-step wizard validation. |
| [WizardCancellationTests.cs](WizardCancellationTests.cs) | Default and disabled cancellation, wizard and step permissions, Cancel/close callback guards, action availability, and revisiting conditional wizard branches. |
| [Components](Components) | Recovered paged, virtual and streaming table view-model tests and original wizard fixtures. |
| [Components/StreamingAndPagingRegressionTests.cs](Components/StreamingAndPagingRegressionTests.cs) | Dispatcher delivery, cancellation/disposal and zero-based page boundaries. |
| [Loader](Loader) | Plugin routing, menu merging, app navigation and registration. `BlockMenuTests` renders the formerly commented-out component case through `HtmlRenderer`. |

Run application shell cases independently, for example:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~ApplicationRenderingTests|FullyQualifiedName~ApplicationScreenHostTests"
```

UI-independent metadata, builders and policies have their own [Application test project](../Sphere10.Framework.Application.Tests/README.md). Keep platform rendering and component-lifetime assertions here.

## Add a regression test

Use NUnit constraint assertions (`Assert.That`) and a fixture focused on the behavior being changed. Follow [the repository test guidance](../../.github/skills/unit-testing/SKILL.md).

- For data operations, use a small source derived from the framework's sync/async data-source hierarchy. Use controlled task completion for overlapping reads or writes, rather than sleeps.
- For rendering, reuse the component-capture host pattern in `ComponentParameterLifecycleTests` and the service setup in `BlazorGridRenderingTests`. Render and invoke component APIs on the renderer dispatcher.
- Await asynchronous operations and use `await Assert.ThatAsync(...)` for asynchronous exception checks on the renderer dispatcher. Dispose renderers, providers, callbacks and pending test resources.
- Keep custom editor assertions on buffered values: the entity should change only after a successful Save. Check that failed validation retains a retryable draft and Cancel preserves the original values.
- Keep permission tests on both source capabilities and the allowed mask, including changes after binding.

## Browser verification and troubleshooting

`HtmlRenderer` confirms rendered markup and managed behavior. It does not establish an Interactive Server connection or execute DOM events, focus handling, column drag resizing, viewport measurements or the JavaScript module. An HTTP 200 response alone also does not prove those interactions work.

After changing Razor event handling, CSS or interop, run the [Blazor tester](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md) and complete its editable-grid walkthrough. Check keyboard editing, Save/Cancel, reference selection, modal focus, header resizing and any automatic page sizing configuration in a real browser. Change the top-right **Theme** selector during a grid edit and on workspace screens: buffered values and component state must survive. Verify theme retention across navigation, readable inputs/tables/dialogs in all four themes, and both dialog generations through their respective demo pages. Inspect the browser console and network requests for circuit/module failures.

If discovery finds no matching tests, use `--list-tests` and check the fixture name/filter. If a build cannot replace tester files, stop the running tester before rerunning this project. SDK or NuGet errors occur before NUnit execution; inspect the first failure and confirm a .NET 10 SDK is installed.

## Opt-in browser regression checks

[DemoShellBrowserTests.cs](DemoShellBrowserTests.cs) exercises the active merged demo: distinct Classic blue/Blue styling and block icons, switching among all four themes during a grid draft, responsive navigation, shared endpoint selection, guarded block switching and retained screen history. Screenshots are attached to NUnit results for visual review. Run it using the same server and runsettings below, with `--filter "FullyQualifiedName~DemoShellBrowserTests"`; combine both browser fixtures with `--filter "FullyQualifiedName~DemoShellBrowserTests|FullyQualifiedName~BlazorGridBrowserTests"`.
[BlazorGridBrowserTests.cs](BlazorGridBrowserTests.cs) is an `[Explicit]`, `[Category("Browser")]` NUnit fixture. It launches an isolated headless Chromium context with real scrollbars enabled, visits `/components/grid`, and samples 150 animation frames after each interaction. The last 60 frames must have stable grid/column/row geometry and no continuing style writes or resize notifications. Cases combine fractional CSS zoom with device scale, edit the third row, open its nested reference picker and drag a column resize grip. The popup must stay inside the viewport, and its Cancel action must work with a normal browser click. The main grid must also fit its viewport when its configured column widths fit; this catches fractional rounding that creates an artificial horizontal scrollbar even after sizing settles. Narrow viewports with intentional overflow are excluded from that fit assertion. These assertions catch feedback loops that a static render cannot detect; they do not require one fixed set of pixel widths.

From the repository root, build the tests while the tester is stopped:

```powershell
dotnet build tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0
```

Start that tester build in a **separate terminal**, using a free port:

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.BlazorTester/Sphere10.Framework.Utils.BlazorTester.csproj -c Release -p:BuildRevision=0 --no-build --no-launch-profile -- --urls http://127.0.0.1:5187
```

Run the explicit fixture against that build. `BlazorTesterUrl` is required and must identify the local running tester. `BrowserExecutablePath` is optional: when omitted, Playwright uses its separately installed Chromium. To use an existing Edge installation without downloading a browser, create a local runsettings file and set its actual executable path:

```powershell
New-Item -ItemType Directory -Force obj/browser-tests | Out-Null
@'
<RunSettings>
  <TestRunParameters>
    <Parameter name="BlazorTesterUrl" value="http://127.0.0.1:5187" />
    <Parameter name="BrowserExecutablePath" value="C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" />
  </TestRunParameters>
</RunSettings>
'@ | Set-Content obj/browser-tests/browser.runsettings

dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --no-build --filter "FullyQualifiedName~BlazorGridBrowserTests" --settings obj/browser-tests/browser.runsettings
```

After source changes, stop the tester and repeat the build/start sequence. The fixture does not start or stop the tester, download browsers, access an existing browser profile or persist sample changes. Default test runs skip it. A stopped tester, unavailable executable or missing Playwright browser is a setup failure when the fixture is explicitly selected.

The centrally pinned [Microsoft.Playwright package](https://www.nuget.org/packages/Microsoft.Playwright/1.63.0) supplies the .NET browser library; see the [official library guide](https://playwright.dev/dotnet/docs/library) for browser setup. Geometry samples use CSS zoom and `DeviceScaleFactor` to exercise fractional layout; this is separate from manually testing native browser zoom controls. Auto page sizing requires its own configured demo and is not covered by this fixture.

## Retained migration references

The two superseded archived test project files under `Components` and `Loader` remain inactive migration references pending cleanup approval. Use the root test project shown in the commands above; that is the project included in solutions and CI. The old `TopbarMenuTests` source contained no tests and remains an empty fixture source. No backend-specific fixture was silently dropped during restoration.