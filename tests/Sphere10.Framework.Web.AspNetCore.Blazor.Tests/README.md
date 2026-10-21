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
| [BlazorGridBrowserTests.cs](BlazorGridBrowserTests.cs) | Explicit real-browser checks for settled grid geometry after fractional zoom/device scaling, row-three editing, nested reference popup and manual column resizing, accessible icon paging, page-size dropdowns and edit guards at desktop/mobile widths. |
| [BlazorGridReferencePickerTests.cs](BlazorGridReferencePickerTests.cs) | Compact accessible popover, dismissal and reopening generations, source replacement, disabled state, disposal and JavaScript import races. |
| [GridDemoDataSourceTests.cs](GridDemoDataSourceTests.cs) | Demo name/age validation, protected deletion, batch rejection before mutation, search, typed/natural sort order and stable ID tie-breaks, page clamping, new-item persistence and related-record identity; immutable live-snapshot replacement, targeted fresh-row identity, source removal, filtered paging and all-matches reads. |
| [GalleryRenderingTests.cs](GalleryRenderingTests.cs) | Every compatibility URL renders its registered screen inside the sole ApplicationShell; registration-backed menus, icons, provider/modal ownership, scoped isolation and local grid integration. |
| [ThemeTests.cs](ThemeTests.cs) | All catalogue presets, defensive metadata arrays, scoped theme isolation, validated changes and notifications, custom registrations and decorator behavior. |
| [ThemeRenderingTests.cs](ThemeRenderingTests.cs) | Per-template stylesheet links and provider attributes, catalogue-driven selector/toggle state, retained child/draft state, renderer dispatch and subscription disposal. |

Controller tests exercise `IDataSource<T>` behavior without rendering a component. Column tests isolate conversion and metadata. Rendering tests verify the connection between those contracts and actual component markup/callbacks. Sample-source tests cover the rules of `TestClassDataSource`, including searching fields that are not directly editable grid cells, and `GridDemoSnapshotDataSource`, the actual read-only source demonstrated in the live-grid section. These managed source tests do not execute DOM measurement, header dragging or browser storage.

The interop cases check that concurrent loads share initialization, disconnected initialization releases its callback before retry, and disposal waits for pending initialization without updating the removed grid. Their fake runtime does not execute the JavaScript module.

## Application shell and recovered components

| Location or fixture | Coverage |
| --- | --- |
| [ApplicationBlockTests.cs](ApplicationBlockTests.cs), [ApplicationScreenHostTests.cs](ApplicationScreenHostTests.cs) | UI registration/snapshot behavior, activation policies, scoped actions, screen guards, host state, cancellation and block removal. |
| [ApplicationScreenHostHarmonizationTests.cs](ApplicationScreenHostHarmonizationTests.cs) | Shared/typed DI identity, reference-based host navigation and lifecycle, Blazor proxy identity, generic decorator dispatch, attachment cancellation and renderer ownership, scoped commands, asynchronous modal interaction and controller dispatch lifetime. |
| [ApplicationGraphOwnershipTests.cs](ApplicationGraphOwnershipTests.cs) | Canonical graph cloning, typed ownership bridges, repeated builders, grouped and toolbar commands, borrowed presentation references, cached generated activators, attachment rollback and cleanup, and circuit-local application parents with original plugin sources. |
| [BlazorApplicationLifecycleTests.cs](BlazorApplicationLifecycleTests.cs) | Awaited circuit startup/finalization and async scope disposal, reconnect retention, keyed task and logger resolution, fresh tasks per scope, SSR nonexecution, process-task isolation, and borrowed host/shared plugin ownership. |
| [BlazorApplicationExitTests.cs](BlazorApplicationExitTests.cs) | Runtime block guards across snapshots, screen vetoes, synchronous renderer rejection and completed exit, worker-thread confirmation, real demo exit-command confirmation, terminal rendering and renderer cleanup, forced prompt dismissal, canceled import retry, legacy delegation and navigation after exit. |
| [ApplicationNavigationTests.cs](ApplicationNavigationTests.cs) | Stable per-session navigation paths without ephemeral queries, callback evaluation against current selection, application-base URI validation, direct programmatic activation in a rendered shell, incoming asynchronous route precedence, unchanged default query routing and guard preservation without duplicate checks. |
| [ApplicationScreenActivationTests.cs](ApplicationScreenActivationTests.cs) | Runtime parameter snapshots and defaults, keyed reuse across concurrent activation, registration/circuit isolation, policy precedence, guard/cancellation behavior, decorator forwarding and key release after closure. `ApplicationRenderingTests` additionally verifies independent keyed component parameters, retained drafts and renderer-owned disposal. |
| [PluginBuilderTests.cs](PluginBuilderTests.cs) | Shared-backed fluent plugin/block configuration, shared/platform DI identity, explicit legacy contract forwarding, independent metadata snapshots, composed startup callbacks, load-time definitions and menu subscribers, registration validation, duplicate plugin-name rejection, and circuit-scoped services. |
| [ApplicationRenderingTests.cs](ApplicationRenderingTests.cs) | Retained rendered components, lifecycle/disposal, bookmark/history resolution and overlapping asynchronous route changes. |
| [DemoPluginRegistrationTests.cs](DemoPluginRegistrationTests.cs) | Actual tester startup validates its complete service graph, registers all four fluent plugins and their blocks once, supplies working gallery dependencies, and isolates action, screen and endpoint state between scopes. |
| [DemoPluginLifecycleTests.cs](DemoPluginLifecycleTests.cs) | Actual tester configuration registers one initializer and finalizer under each of its four plugin names, resolves separate task instances per circuit, renders the completed startup count, skips prerender scopes, retains startup through reconnects, and awaits once-only shutdown on exit/circuit closure/disposal. |
| [IdentityControlTests.cs](IdentityControlTests.cs) | Claims/anonymous captions, template context, identity/menu changes, existing command/separator callbacks, async duplicate suppression and failure recovery. |
| [ApplicationToolBarContentTests.cs](ApplicationToolBarContentTests.cs) | Shell and standalone command bar retain toolbar content when no command items exist. |
| [SearchInputTests.cs](SearchInputTests.cs) | Search result links, throttling, stale completion/error rejection, provider replacement, dismissal and pending-search disposal. |
| [EndpointSelectorTests.cs](EndpointSelectorTests.cs) | Restored endpoint selector registration, two-way server-page synchronization, circuit isolation, renderer dispatch and subscription disposal. |
| [BlockDockTests.cs](BlockDockTests.cs) | Compact menu compatibility, builder-supplied icons, menu-only block browsing, scoped UI registration changes and subscription disposal. |
| [DemoNavigationTests.cs](DemoNavigationTests.cs) | Compatibility alias resolution and defensive arrays; header search follows the host's registered screen blocks and returns canonical application URLs. |
| [ApplicationScreenPolicyTests.cs](ApplicationScreenPolicyTests.cs) | Plugin/block startup order; menu defaults; permanent sessions and atomic close rejection; empty-screen lifetimes, guards, renderer retention/disposal, administrative removal and cancellation. |
| [ApplicationStartupScreenTests.cs](ApplicationStartupScreenTests.cs) | Canonical startup identity, static versus session activation parameters, component/host exclusions, unavailable or forged settings, permanent-screen retention, explicit-route precedence, deferred browser preferences and failed-storage recovery. |
| [ApplicationStartupScreenBrowserTests.cs](ApplicationStartupScreenBrowserTests.cs) | Opt-in actual tab-context save/check state, browser storage, root startup before unrelated default creation, explicit bookmark precedence and corrupt-preference fallback. |
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

Run the application lifecycle and exit integration cases together:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~BlazorApplicationLifecycleTests|FullyQualifiedName~BlazorApplicationExitTests|FullyQualifiedName~DemoPluginLifecycleTests"
```

These fixtures distinguish host-wide plugin loading from circuit-local application initialization, screen-host startup selection and renderer-owned screen disposal. Register plugin tasks through the builder helpers after `WithName(...)`; keep unkeyed process tasks separate. The shared [ApplicationPluginLifecycleRegistrationTests](../Sphere10.Framework.Application.Tests/UI/ApplicationPluginLifecycleRegistrationTests.cs) verifies immediate name enforcement on all type/factory overloads. Exercise object-based command execution with canonical parent relationships, and reserve ID resolution for browser navigation tests.

## Application patterns coverage

The [Application patterns showcase](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md) composes reusable library controls into generic collection editors, guided setup flows, native commands and a complete theme preview.

| Fixture | What it protects |
| --- | --- |
| [CollectionShowcaseTests.cs](CollectionShowcaseTests.cs) | Detached catalogue drafts, normalized saves and notifications, scoped data isolation, composed filters before paging, typed sorting and stable identity. |
| [GuidedWorkspaceWizardTests.cs](GuidedWorkspaceWizardTests.cs) | Solo/Team route changes, retained draft values, preset-detour return, settings validation, cancellation and terminal completion rules. |
| [ShowcaseBrowserTests.cs](ShowcaseBrowserTests.cs) | Native keyboard submenus, guarded and hidden commands, screen-scoped menu merging, theme-preserved drafts and chart selection, card/table filtering, keyed editor reuse and Save/Discard guards, embedded/modal wizard validation, branching, detours, cancellation and one completed snapshot. Desktop/mobile containment and screenshots are included. |

Run the two managed fixtures without a browser:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~CollectionShowcaseTests|FullyQualifiedName~GuidedWorkspaceWizardTests"
```

After the build/start sequence in [Opt-in browser regression checks](#opt-in-browser-regression-checks), select the showcase fixture using the same local runsettings:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --no-build --filter "FullyQualifiedName~ShowcaseBrowserTests" --settings obj/browser-tests/browser.runsettings --logger "trx;LogFileName=showcase-browser.trx" --results-directory obj/test-results/showcase-browser
```

Browser evidence is attached to the NUnit results under `ShowcaseBrowserTests` in the test work directory. The fixture requires the existing local tester and Chromium setup; ordinary test runs skip it.

## Add a regression test

Use NUnit constraint assertions (`Assert.That`) and a fixture focused on the behavior being changed. Follow [the testing practices](../../docs/guidelines/patterns/testing.md).

- For data operations, use a small source derived from the framework's sync/async data-source hierarchy. Use controlled task completion for overlapping reads or writes, rather than sleeps.
- For rendering, reuse the component-capture host pattern in `ComponentParameterLifecycleTests` and the service setup in `BlazorGridRenderingTests`. Render and invoke component APIs on the renderer dispatcher.
- Await asynchronous operations and use `await Assert.ThatAsync(...)` for asynchronous exception checks on the renderer dispatcher. Dispose renderers, providers, callbacks and pending test resources.
- Keep custom editor assertions on buffered values: the entity should change only after a successful Save. Check that failed validation retains a retryable draft and Cancel preserves the original values.
- Keep permission tests on both source capabilities and the allowed mask, including changes after binding.

## Browser verification and troubleshooting

`HtmlRenderer` confirms rendered markup and managed behavior. It does not establish an Interactive Server connection or execute DOM events, focus handling, column drag resizing, viewport measurements or the JavaScript module. An HTTP 200 response alone also does not prove those interactions work.

After changing Razor event handling, CSS or interop, run the [Blazor tester](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md) and complete its editable-grid walkthrough. Check keyboard editing, Save/Cancel, reference selection, modal focus, header resizing and any automatic page sizing configuration in a real browser. Change the top-right **Theme** selector during a grid edit and on workspace screens: buffered values and component state must survive. Verify theme retention across navigation, readable inputs/tables/dialogs in all catalogue presets, and both dialog generations through their respective demo pages. Inspect the browser console and network requests for circuit/module failures.

If discovery finds no matching tests, use `--list-tests` and check the fixture name/filter. If a build cannot replace tester files, stop the running tester before rerunning this project. SDK or NuGet errors occur before NUnit execution; inspect the first failure and confirm a .NET 10 SDK is installed.

## Shared runtime, tabs and wizard coverage

`ApplicationAggregateTests` checks that the shared and Blazor application contracts resolve to one scoped runtime, canonical live block references and defensive membership arrays, standalone navigation, scope isolation, and startup plugins that are not loaded again. `LoadedPluginRegistrationTests` distinguishes successful startup receipts from ordinary DI registrations, checks failed/duplicate loads, and preserves captured startup contribution order when source membership changes. `BlockDockTests` verifies that one loaded plugin can contribute several independent block icons and menus. Configuration runs once per scope. `ApplicationTabsTests` covers ordered tab membership, guarded batch closure, SingleView retention, mode transitions and screen-command execution through canonical command objects and their parent blocks, including rejection of detached and foreign-circuit commands. `ApplicationCommandStateTests` checks command availability through the object-based UI-service entry point. `ApplicationCommandRenderingTests` verifies that active-screen menu and toolbar contributions replace the appropriate broader commands and restore on switching. Browser navigation tests cover route-ID resolution separately from command execution.

The portable `Application` test project owns wizard navigation, mutation, terminal state and merge-algorithm tests. `SharedWizardAdapterTests`, existing wizard cancellation/completion fixtures, and native wizard adapter tests verify platform integration. [DynamicWizardStepTests.cs](DynamicWizardStepTests.cs) renders both Blazor generations to verify Next for a step whose successor is not registered yet, successful injection and final completion, missing-successor errors and retry, validation rejection, Back, live traits, replacement and resolver restoration on disposal. JavaScript test doubles follow the same PascalCase module calls as the actual components; browser tests execute the real modules.

## Opt-in browser regression checks

[ApplicationTabsBrowserTests.cs](ApplicationTabsBrowserTests.cs) exercises singleton session reuse, distinct multi-instance IDs and local state, per-instance switching locks with independent block browsing, selection, guard vetoes (including restoring the Open Screens dropdown after a rejected choice), batch closure, renaming, drag/keyboard reordering, focus, history, View-menu screen modes and merged File-menu/toolbar commands in the actual workspace. [WizardBrowserTests.cs](WizardBrowserTests.cs) opens both wizard generations through registered block/sidebar controls, then tests validation, non-cancellable Escape, successful completion and cancellable reopening with focus restoration. Run all browser fixtures with `--filter "FullyQualifiedName~BrowserTests"` and the runsettings below.

[ApplicationStartupScreenBrowserTests.cs](ApplicationStartupScreenBrowserTests.cs) uses the same tester and runsettings. Run it together with `ApplicationTabsBrowserTests` using `--filter "FullyQualifiedName~ApplicationStartupScreenBrowserTests|FullyQualifiedName~ApplicationTabsBrowserTests"`. It saves the actual tab-context startup choice in an isolated browser context, opens the root URL in a fresh circuit, checks that the unrelated default never mounted, verifies explicit bookmark precedence, and corrupts only that context's preference to exercise safe fallback. It does not use a personal browser profile or change server-wide user settings.

[DemoShellBrowserTests.cs](DemoShellBrowserTests.cs) verifies the sole full-page ApplicationShell: top menu/toolbar geometry, left block menus and dock, tabs above the screen body, configured title/favicon delivery, theme/endpoint state, menu-only block browsing, grid drafts retained across hosted screens and in-content compatibility links, guarded screen navigation and browser history. Each catalogue preset is also exercised at desktop and mobile widths: its own stylesheet and palette must load, an unsaved grid draft and route must survive the switch, and the shell must stay within the viewport. Screenshots are attached to NUnit results for visual review. Run it using the same server and runsettings below, with `--filter "FullyQualifiedName~DemoShellBrowserTests"`; combine both browser fixtures with `--filter "FullyQualifiedName~DemoShellBrowserTests|FullyQualifiedName~BlazorGridBrowserTests"`.
[BlazorGridBrowserTests.cs](BlazorGridBrowserTests.cs) is an `[Explicit]`, `[Category("Browser")]` NUnit fixture. It launches an isolated headless Chromium context with real scrollbars enabled, opens `/components/grid`, verifies its canonical hosted grid and single shell, then samples 150 animation frames after each interaction. The last 60 frames must have stable grid/column/row geometry and no continuing style writes or resize notifications. Cases combine fractional CSS zoom with device scale, edit the third row, open its nested reference picker and drag a column resize grip. The popup must stay inside the viewport, and its Cancel action must work with a normal browser click. The main grid must also fit its viewport when its configured column widths fit; this catches fractional rounding that creates an artificial horizontal scrollbar even after sizing settles. Narrow viewports with intentional overflow are excluded from that fit assertion. Separate desktop/mobile cases navigate with all four icon buttons, change the page-size dropdown, verify boundary disabling and edit guards, and attach focused pager screenshots. These assertions catch feedback loops that a static render cannot detect; they do not require one fixed set of pixel widths.

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

The centrally pinned [Microsoft.Playwright package](https://www.nuget.org/packages/Microsoft.Playwright/1.63.0) supplies the .NET browser library; see the [official library guide](https://playwright.dev/dotnet/docs/library) for browser setup. Geometry samples use CSS zoom and `DeviceScaleFactor` to exercise fractional layout; this is separate from manually testing native browser zoom controls. The tester's independent **Live snapshots and layout** section supplies the configured Auto Paged, image-layout and no-indicator scenarios described in its walkthrough; the original editable-grid geometry cases alone do not establish those behaviors.

## Retained migration references

The two superseded archived test project files under `Components` and `Loader` remain inactive migration references pending cleanup approval. Use the root test project shown in the commands above; that is the project included in solutions and CI. The old `TopbarMenuTests` source contained no tests and remains an empty fixture source. No backend-specific fixture was silently dropped during restoration.

### Screen policy browser profiles

`ApplicationScreenPoliciesBrowserTests` verifies the real empty/permanent controls and attaches successful screenshots. Start the tester first with the chosen [launch profile](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md#screen-lifetime-profiles), then pass `BlazorTesterUrl` and `BrowserExecutablePath` in the existing browser runsettings.

- Default / `Empty singleton`: run `FullyQualifiedName~ApplicationScreenPoliciesBrowserTests.EmptyScreen`.
- `Empty instance`: run the same case; it verifies the displayed MultiInstance policy creates a new component and clears the previous draft.
- `Permanent singleton`: run `FullyQualifiedName~ApplicationScreenPoliciesBrowserTests.PermanentScreen` with the additional test parameter `<Parameter name="PermanentProfile" value="true" />`.

The ordinary full browser suite targets the default profile. The permanent case is skipped unless its profile parameter is supplied, because a permanent normal screen intentionally prevents an empty workspace.

## Theme component browser coverage

`ThemeComponentsBrowserTests` runs the registered `/components/themes` gallery for every preset at desktop and mobile widths. It checks semantic solid/outline/disabled buttons, badge fills and contrast text, independent combined color utilities, readable alerts/contextual tables, input/card surfaces, keyboard focus and hover, working menus/accordion/dialogs, and state retention through theme changes. Nested providers and recursive CSS imports are included. Run it with the same browser runsettings above:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --no-build --filter "FullyQualifiedName~ThemeComponentsBrowserTests" --settings obj/browser-tests/browser.runsettings
```
