# Sphere10.Framework.Web.AspNetCore.Blazor.Tests

NUnit coverage for the Blazor library, its application shell and the local gallery samples. The project references both [the library](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) and [the tester](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md), because the sample routes, plugin definitions and grid data source live in the utility.

The default suite runs without a browser driver, external service, database or WebAssembly runtime. Component tests use ASP.NET Core's `HtmlRenderer`. Managed interop lifecycle tests supply fake `IJSRuntime` and `IJSObjectReference` implementations. The explicitly selected `Browser` fixtures use Playwright against a running local tester.

## Run the tests

Install the .NET 10 SDK and run these commands **from the repository root**. A machine with a .NET 11 SDK also builds and runs the `net11.0` target. `dotnet test` restores and builds the required projects before running NUnit. `BuildRevision=0` supplies an explicit local build revision.

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

Run application shell cases independently:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~ApplicationRenderingTests|FullyQualifiedName~ApplicationScreenHostTests"
```

Run the application lifecycle and exit integration cases together:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~ApplicationLifecycleTests|FullyQualifiedName~ApplicationExitTests|FullyQualifiedName~DemoPluginLifecycleTests"
```

Run the two managed application-pattern fixtures without a browser:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~CollectionShowcaseTests|FullyQualifiedName~GuidedWorkspaceWizardTests"
```

For test discovery or a saved result file:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --list-tests
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --logger "trx;LogFileName=blazor-tests.trx" --results-directory obj/test-results/blazor
```

The `--filter` value is quoted so shell characters such as `|` remain part of the test expression. Do not add `--no-build` after changing source, because it would run the previously compiled assembly.

## Requirements

- The default suite needs only the .NET SDK.
- Fixtures marked `[Explicit]` and `[Category("Browser")]` need a running local tester, supplied as the `BlazorTesterUrl` test parameter, and an installed Chromium or Edge, optionally supplied as `BrowserExecutablePath`. [Opt-in browser regression checks](#opt-in-browser-regression-checks) describes the runner script and the manual setup.
- Fixtures in the `Integration` category start the actual tester configuration in process. They need no external service.

## Contents

### Project root

Fixtures for the library's application shell, grid, themes, wizards and settings, and for the tester's samples.

| Fixture | Purpose |
| --- | --- |
| [ApplicationAggregateTests](ApplicationAggregateTests.cs) | Shared and Blazor application contracts resolve to one scoped runtime per circuit, with live block membership, standalone blocks and frozen command snapshots. |
| [ApplicationBlockTests](ApplicationBlockTests.cs) | Block and menu registration snapshots, stable ID validation, host ordering, independent activation policies and screen type validation. |
| [ApplicationBuilderPatternTests](ApplicationBuilderPatternTests.cs) | Block builder screen and action shortcuts, policy validation, synchronous and asynchronous actions and toolbar items. |
| [ApplicationBuilderSharingTests](ApplicationBuilderSharingTests.cs) | Shared builder configuration reaches Blazor sessions, generic screen validation and wizard builder chains. |
| [ApplicationBuilderTests](ApplicationBuilderTests.cs) | Product information and branding from the builder source, head metadata, build freezing and the tester's configured branding. |
| [ApplicationCommandRenderingTests](ApplicationCommandRenderingTests.cs) | Active-screen menu and toolbar contributions replace broader commands and restore on switching, alongside plugin commands, SingleView mounting and tab state. |
| [ApplicationCommandScopeTests](ApplicationCommandScopeTests.cs) | Block activation replaces block commands, nested group overrides persist and dock selection honours guards. |
| [ApplicationCommandStateTests](ApplicationCommandStateTests.cs) | Hidden registrations, disabled and predicate-enabled commands, toolbar groups and dropdowns, and coalesced selection. |
| [ApplicationExitTests](ApplicationExitTests.cs) | Block guards across snapshots, screen vetoes, synchronous renderer rejection, worker confirmation, the demo exit command, forced dismissal and navigation after exit. |
| [ApplicationGraphOwnershipTests](ApplicationGraphOwnershipTests.cs) | Canonical graph cloning, typed ownership bridges, grouped and toolbar commands, borrowed references, attachment rollback and circuit-local application parents. |
| [ApplicationLifecycleTests](ApplicationLifecycleTests.cs) | Awaited circuit startup and finalization, fresh keyed tasks per circuit, server prerendering without lifecycle, and borrowed host and plugin ownership. |
| [ApplicationMenuEventTests](ApplicationMenuEventTests.cs) | Menu selection events through snapshots, default activation, guard vetoes, action success and failure, and frozen menu text. |
| [ApplicationNavigationTests](ApplicationNavigationTests.cs) | Stable per-session navigation paths, application-base URI validation, programmatic activation, incoming route precedence and guard preservation. |
| [ApplicationNestedMenuTests](ApplicationNestedMenuTests.cs) | Nested menu groups' click and keyboard expansion, dismissal, data refresh and ID collisions. |
| [ApplicationOwnerSettingsTests](ApplicationOwnerSettingsTests.cs) | Block and plugin settings changes refresh a screen without its own settings and stay circuit-local. |
| [ApplicationRenderingTests](ApplicationRenderingTests.cs) | Retained rendered components, keyed parameters and drafts, bookmarks and history, overlapping route changes and drawer behaviour. |
| [ApplicationScreenActivationTests](ApplicationScreenActivationTests.cs) | Runtime parameter snapshots, keyed reuse under concurrent activation, policy precedence, guards, cancellation and key release after closure. |
| [ApplicationScreenContractTests](ApplicationScreenContractTests.cs) | Explicit and public lifecycle dispatch through the shared `IApplicationScreen` and Blazor contracts. |
| [ApplicationScreenHostHarmonizationTests](ApplicationScreenHostHarmonizationTests.cs) | Shared and typed DI identity, reference-based host navigation, proxy identity, decorator dispatch, attachment rollback and modal interaction. |
| [ApplicationScreenHostTests](ApplicationScreenHostTests.cs) | Host navigation selection, single and multi-instance sessions, guards and cancellation, block removal, actions and disposal. |
| [ApplicationScreenNotificationTests](ApplicationScreenNotificationTests.cs) | Late notifications after close and reopen, and notifications after a rejected close. |
| [ApplicationScreenPoliciesBrowserTests](ApplicationScreenPoliciesBrowserTests.cs) | Browser checks of empty and permanent screens in the running tester (explicit). |
| [ApplicationScreenPolicyTests](ApplicationScreenPolicyTests.cs) | Plugin and block startup order, menu defaults, permanent sessions, empty-screen lifetimes, administrative removal and cancellation. |
| [ApplicationSettingsBrowserTests](ApplicationSettingsBrowserTests.cs) | Browser checks of Options drafts, validation, apply, reset and persistence across a browser restart (explicit). |
| [ApplicationSettingsMenuTests](ApplicationSettingsMenuTests.cs) | The Options command reuses the contextual menu heading and keeps the product menu order. |
| [ApplicationSettingsTests](ApplicationSettingsTests.cs) | Settings ownership, the Options dialog, browser-backed persistence and hydration, editor validation and renderer notifications. |
| [ApplicationStartupScreenBrowserTests](ApplicationStartupScreenBrowserTests.cs) | Browser checks of saved startup choices, root startup, bookmark precedence and corrupt-preference fallback (explicit). |
| [ApplicationStartupScreenTests](ApplicationStartupScreenTests.cs) | Canonical startup identity, persistable parameters, exclusions, forged settings, explicit-route precedence and failed-storage recovery. |
| [ApplicationTabsBrowserTests](ApplicationTabsBrowserTests.cs) | Browser checks of tab sessions, switching locks, guards, batch close, renaming, reordering, focus and View-menu modes (explicit). |
| [ApplicationTabsTests](ApplicationTabsTests.cs) | Tab ordering, guarded and batch closure, SingleView retention and screen-command execution through canonical commands. |
| [ApplicationToolBarContentTests](ApplicationToolBarContentTests.cs) | Toolbar content renders when no command items exist. |
| [BlazorDefaultInterfaceDispatchTests](BlazorDefaultInterfaceDispatchTests.cs) | Blazor application and plugin overrides project through the shared interfaces. |
| [BlazorGridBrowserTests](BlazorGridBrowserTests.cs) | Browser checks of settled grid geometry under fractional scaling, editing, the reference popup, column resizing and paging (explicit). |
| [BlazorGridColumnTests](BlazorGridColumnTests.cs) | Typed property factories, automatic definitions, metadata, scalar parsing and formatting, invariant culture and reference identity. |
| [BlazorGridControllerTests](BlazorGridControllerTests.cs) | Capability intersection, zero-based query paging, stale-result rejection, buffered edits, validation and write rollback, creation, deletion and source ownership. |
| [BlazorGridReferencePickerTests](BlazorGridReferencePickerTests.cs) | The compact reference popover, dismissal generations, rebinding, disabled state, disposal and JavaScript import races. |
| [BlazorGridRenderingTests](BlazorGridRenderingTests.cs) | Capability-controlled commands, editing, validation, paging controls, stored preferences, column widths and JavaScript initialization and disposal. |
| [BlazorGridSettingsTests](BlazorGridSettingsTests.cs) | Grid settings JSON shape, declared column widths and validation. |
| [BlazorScreenRegistrationTests](BlazorScreenRegistrationTests.cs) | Independent same-type registrations, activation preparation, exact references, menu type shortcuts, graph-local monikers and stable startup identity. |
| [BlockDockTests](BlockDockTests.cs) | Compact block menus, builder-supplied icons, menu-only block browsing, scoped registration changes and subscription disposal. |
| [BuilderConvenienceTests](BuilderConvenienceTests.cs) | Inline screen, menu and toolbar builder overloads against graph-local monikers and renderer-owned activation. |
| [CollectionShowcaseTests](CollectionShowcaseTests.cs) | Detached catalogue drafts, normalized saves and notifications, scoped data isolation, filters before paging, typed sorting and stable identity. |
| [ComponentParameterLifecycleTests](ComponentParameterLifecycleTests.cs) | Parameter changes in pagers, page-size selectors, virtual providers and streams, modal completion, host teardown and final-step wizard validation. |
| [DemoNavigationTests](DemoNavigationTests.cs) | Compatibility alias resolution and header search over the host's registered screen blocks. |
| [DemoPluginLifecycleTests](DemoPluginLifecycleTests.cs) | The tester registers one initializer and finalizer per plugin, runs them once per circuit, skips prerendering and awaits shutdown. |
| [DemoPluginRegistrationTests](DemoPluginRegistrationTests.cs) | The tester's startup validates its service graph, registers its plugins and blocks once and isolates scoped state. |
| [DemoShellBrowserTests](DemoShellBrowserTests.cs) | Browser checks of the full-page shell, every theme preset, grid drafts, block browsing, endpoints and history (explicit). |
| [DynamicWizardStepTests](DynamicWizardStepTests.cs) | Both wizard generations with successors registered at run time, validation, Back, live traits, replacement and resolver restoration. |
| [EndpointSelectorTests](EndpointSelectorTests.cs) | Endpoint selector registration, two-way synchronization with the servers page, background notifications and disposal. |
| [GalleryRenderingTests](GalleryRenderingTests.cs) | Every compatibility URL renders its registered screen inside the sole application shell, with icons, modal isolation and local grid data. |
| [GallerySettingsMigrationTests](GallerySettingsMigrationTests.cs) | Gallery settings and legacy import are restored before the first grid query. |
| [GridDemoDataSourceTests](GridDemoDataSourceTests.cs) | Demo data source validation, protected deletion, search, typed sorting, paging, new items and live-snapshot replacement. |
| [GuidedWorkspaceWizardTests](GuidedWorkspaceWizardTests.cs) | Solo and Team route changes, retained drafts, the preset detour, validation and completion rules. |
| [IdentityControlTests](IdentityControlTests.cs) | Identity captions, templates, menu changes, duplicate suppression, failures and keyboard interop disposal. |
| [LoadedPluginRegistrationTests](LoadedPluginRegistrationTests.cs) | Loaded plugin receipts versus DI registrations, failed and duplicate loads, nested registrations and contribution order. |
| [PluginBuilderTests](PluginBuilderTests.cs) | Fluent plugin and block configuration, independent builds, name validation, duplicate blocks and service-only plugins. |
| [ScreenRegistrationBrowserTests](ScreenRegistrationBrowserTests.cs) | Browser check of same-type registrations with independent drafts and a menu-free bookmark after reload (explicit). |
| [ScreenSettingsOwnershipTests](ScreenSettingsOwnershipTests.cs) | Web screens declare their own nested settings without activation. |
| [SearchInputTests](SearchInputTests.cs) | Search result links, stale response rejection, clearing, dismissal, provider replacement, failure and disposal. |
| [SharedPresentationAdapterTests](SharedPresentationAdapterTests.cs) | Shared `Application.UI` adapters, typed host dispatch and rejection of screens without component support. |
| [SharedWizardAdapterTests](SharedWizardAdapterTests.cs) | Both Blazor wizard generations use the shared workflow, and the builders copy and reset shared configuration. |
| [ShowcaseBrowserTests](ShowcaseBrowserTests.cs) | Browser checks of the application patterns showcase commands, themes, collections, guided wizard and block icons (explicit). |
| [ThemeComponentsBrowserTests](ThemeComponentsBrowserTests.cs) | Browser checks of the Bootstrap theme component gallery for every preset (explicit). |
| [ThemeRenderingTests](ThemeRenderingTests.cs) | Stylesheet links and provider attributes, selector and toggle state, retained child state and subscription disposal. |
| [ThemeTests](ThemeTests.cs) | Theme presets, scoped isolation, validated changes and notifications, custom registrations and the decorator. |
| [UserSettingsCircuitTests](UserSettingsCircuitTests.cs) | The `UserSettings` facade binds to the circuit's browser provider across activities, lifecycle and failures. |
| [WizardBrowserTests](WizardBrowserTests.cs) | Browser checks of validation, completion and modal cancellation in both wizard generations (explicit). |
| [WizardCancellationTests](WizardCancellationTests.cs) | Default and disabled cancellation, wizard and step permissions, cancel guards and conditional branches. |
| [WizardNavigationTests](WizardNavigationTests.cs) | External dynamic navigation, apply-and-return steps and Back validation guards. |

### Components project

The `Components` folder holds the nested project file [`Sphere10.Framework.DApp.Presentation.Tests.csproj`](Components/Sphere10.Framework.DApp.Presentation.Tests.csproj). It targets `net8.0` and references `src/Sphere10.Framework.DApp.Presentation`, a project that is not in this repository, so it cannot build on its own. Its recovered table view-model and wizard fixtures compile into the root project and run there. `VirtualPagedTableViewModelTests` declares an internal `TestDataService<T>` helper.

| Fixture | Purpose |
| --- | --- |
| [PagedTableTests](Components/PagedTableViewModelTests.cs) | Paged table view model page progression, next and previous, page size and defensive arrays. |
| [RapidTableViewModelTests](Components/RapidTableViewModelTests.cs) | Streamed table view model initialization, item limits, disposal and defensive arrays. |
| [StreamingAndPagingRegressionTests](Components/StreamingAndPagingRegressionTests.cs) | Renderer dispatcher delivery, stream cancellation and disposal, and zero-based virtual page boundaries. |
| [VirtualPagedTableViewModelTests](Components/VirtualPagedTableViewModelTests.cs) | Virtual paged table view model first page, next and previous, page size, provider arrays and disposal. |
| [DefaultWizardTests](Components/Wizard/DefaultWizardTests.cs) | `DefaultWizard` navigation, step injection, replacement, removal, finish and cancel. |

### Loader project

The `Loader` folder holds the nested project file [`Sphere10.Framework.DApp.Presentation.Loader.Tests.csproj`](Loader/Sphere10.Framework.DApp.Presentation.Loader.Tests.csproj). It targets `net8.0` and references three `src/Sphere10.Framework.DApp.*` projects that are not in this repository, so it cannot build on its own. Its plugin loader fixtures and helpers compile into the root project and run there. The internal `TestPluginLocator` and `TestViewModel` helpers support them.

| Fixture | Purpose |
| --- | --- |
| [TestNavigationManager](Loader/TestNavigationManager.cs) | A `NavigationManager` with a configurable base URI for loader tests (helper, not a fixture). |
| [TestPlugin](Loader/TestPlugin.cs) | A routed plugin with sample applications, blocks, pages and menu items (helper, not a fixture). |
| [BlockMenuTests](Loader/ComponentTests/BlockMenuTests.cs) | The block menu renders the selected application's links through `HtmlRenderer`. |
| [AppMenuTests](Loader/NavigationTests/AppMenuTests.cs) | The application menu is initialized with the loaded applications. |
| [DefaultAppManagerTests](Loader/NavigationTests/DefaultAppManagerTests.cs) | Application loading, default selection and navigation to applications and pages. |
| [DefaultPluginManagerTests](Loader/NavigationTests/DefaultPluginManagerTests.cs) | Plugin loading and service registration. |
| [TopbarMenuTests](Loader/NavigationTests/TopbarMenuTests.cs) | An empty class retained from the original source. It contains no tests. |
| [MenuItemTests](Loader/PluginTests/MenuItemTests.cs) | Menu item child copies, defensive metadata arrays, merging and copying. |
| [RoutingPathExtensionTests](Loader/PluginTests/RoutingPathExtensionTests.cs) | Application path extraction and query trimming for relative routes. |

## Coverage notes

Controller tests exercise `IDataSource<T>` behaviour without rendering a component. Column tests isolate conversion and metadata. Rendering tests verify the connection between those contracts and actual component markup and callbacks. Sample-source tests cover the rules of `TestClassDataSource`, including searching fields that are not directly editable grid cells, and `GridDemoDataSource`, the loadable list source demonstrated in the live-grid section, with `GridDemoRefreshableDataSource` serving its revisions. These managed source tests do not execute DOM measurement, header dragging or browser storage.

The interop cases check that concurrent loads share initialization, disconnected initialization releases its callback before retry, and disposal waits for pending initialization without updating the removed grid. Their fake runtime does not execute the JavaScript module.

UI-independent metadata, builders and policies have their own [Application test project](../Sphere10.Framework.Application.Tests/README.md). Keep platform rendering and component-lifetime assertions here. The shared [ApplicationPluginLifecycleRegistrationTests](../Sphere10.Framework.Application.Tests/UI/ApplicationPluginLifecycleRegistrationTests.cs) verifies immediate name enforcement on all type and factory overloads. Register plugin tasks through the builder helpers after `WithName(...)`, and keep unkeyed process tasks separate.

Screen metadata belongs to the block's `AddScreen` registrations. Commands retain their own presentation and callbacks. Parameter defaults, activation policy and screen kind are asserted on the canonical registration. Menu-free activation has no generated command, and same-type registrations retain independent singleton and keyed-instance identity. A menu type shortcut selects the first matching registration in its block, and exact references distinguish a particular registration.

The portable `Application` test project owns wizard navigation, mutation, terminal state and merge-algorithm tests. The wizard fixtures here verify platform integration. JavaScript test doubles follow the same PascalCase module calls as the actual components. Browser tests execute the real modules.

The [Application patterns showcase](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md) composes reusable library controls into generic collection editors, guided setup flows, native commands and a complete theme preview. `CollectionShowcaseTests`, `GuidedWorkspaceWizardTests` and `ShowcaseBrowserTests` cover it.

## Add a regression test

Use NUnit constraint assertions (`Assert.That`) and a fixture focused on the behaviour being changed. Follow [the testing practices](../../docs/guidelines/patterns/testing.md).

- For data operations, use a small source derived from the framework's sync and async data-source hierarchy. Use controlled task completion for overlapping reads or writes, rather than sleeps.
- For rendering, reuse the component-capture host pattern in `ComponentParameterLifecycleTests` and the service setup in `BlazorGridRenderingTests`. Render and invoke component APIs on the renderer dispatcher.
- Await asynchronous operations and use `await Assert.ThatAsync(...)` for asynchronous exception checks on the renderer dispatcher. Dispose renderers, providers, callbacks and pending test resources.
- Keep custom editor assertions on buffered values. The entity should change only after a successful Save. Check that failed validation retains a retryable draft and Cancel preserves the original values.
- Keep permission tests on both source capabilities and the allowed mask, including changes after binding.

## Browser verification and troubleshooting

`HtmlRenderer` confirms rendered markup and managed behaviour. It does not establish an Interactive Server connection or execute DOM events, focus handling, column drag resizing, viewport measurements or the JavaScript module. An HTTP 200 response alone also does not prove those interactions work.

After changing Razor event handling, CSS or interop, run the [Blazor tester](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md) and complete its editable-grid walkthrough. Check keyboard editing, Save and Cancel, reference selection, modal focus, header resizing and any automatic page sizing configuration in a real browser. Change the top-right **Theme** selector during a grid edit and on workspace screens. Buffered values and component state must survive. Verify theme retention across navigation, readable inputs, tables and dialogs in all catalogue presets, and both dialog generations through their demo pages. Inspect the browser console and network requests for circuit and module failures.

If discovery finds no matching tests, use `--list-tests` and check the fixture name and filter. If a build cannot replace tester files, stop the running tester before rerunning this project. SDK or NuGet errors occur before NUnit execution. Inspect the first failure and confirm a .NET 10 SDK is installed.

## Opt-in browser regression checks

The `OneTimeSetUp: Requires BlazorTesterUrl...` message in an ordinary test run is the fixture's `[Explicit]` skip reason, not a failed setup method. Browser fixtures remain opt-in so the default suite needs no running tester or browser.

Run the wizard browser cases with one command from the repository root using PowerShell 7.2 or later:

```powershell
./scripts/test-blazor-browser.ps1
```

The [browser runner](../../scripts/test-blazor-browser.ps1) builds into an isolated artifact directory, starts its own tester on a free loopback port, supplies `BlazorTesterUrl` and the browser path through temporary runsettings, runs the selected fixtures and stops only its own server afterwards. Its default filter is `FullyQualifiedName~WizardBrowserTests`. It prefers an installed Edge or Chrome on Windows, otherwise it uses separately installed Playwright Chromium. It does not download browsers or use personal browser profiles. A run in which every case was skipped is rejected. Logs, screenshots and TRX results remain in the reported temporary directory. Pass `-outputDirectory` to choose another location.

The [build workflow](../../.github/workflows/main.yml) runs the wizard cases in a separate **Wizard browser tests** job using this runner and retains its logs, screenshots and TRX results. The ordinary unit-test job runs without a browser.

Select another browser fixture or an executable explicitly when needed:

```powershell
./scripts/test-blazor-browser.ps1 -filter 'FullyQualifiedName~ScreenRegistrationBrowserTests'
./scripts/test-blazor-browser.ps1 -filter 'FullyQualifiedName~BlazorGridBrowserTests' -browserExecutablePath 'C:\Program Files\Google\Chrome\Application\chrome.exe'
```

To run against a tester you start yourself, build the tests from the repository root while the tester is stopped:

```powershell
dotnet build tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0
```

Start that tester build in a **separate terminal**, using a free port:

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.BlazorTester/Sphere10.Framework.Utils.BlazorTester.csproj -c Release -p:BuildRevision=0 --no-build --no-launch-profile -- --urls http://127.0.0.1:5187
```

Run the explicit fixture against that build. `BlazorTesterUrl` is required and must identify the local running tester. `BrowserExecutablePath` is optional. When it is omitted, Playwright uses its separately installed Chromium. To use an existing Edge installation without downloading a browser, create a local runsettings file and set its actual executable path:

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

Run all browser fixtures with `--filter "FullyQualifiedName~BrowserTests"` and the same runsettings. Browser evidence, such as the showcase and shell screenshots, is attached to the NUnit results.

Other fixtures use the same build, tester and runsettings. These commands run the showcase fixture with its own result file, the full-page shell, and the startup-screen and tab fixtures together:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --no-build --filter "FullyQualifiedName~ShowcaseBrowserTests" --settings obj/browser-tests/browser.runsettings --logger "trx;LogFileName=showcase-browser.trx" --results-directory obj/test-results/showcase-browser
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --no-build --filter "FullyQualifiedName~DemoShellBrowserTests" --settings obj/browser-tests/browser.runsettings
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --no-build --filter "FullyQualifiedName~ApplicationStartupScreenBrowserTests|FullyQualifiedName~ApplicationTabsBrowserTests" --settings obj/browser-tests/browser.runsettings
```

`ShowcaseBrowserTests` also writes its screenshots to a `ShowcaseBrowserTests` folder in the test work directory. The startup-screen fixture saves its startup choice in an isolated browser context, so it changes no personal profile or server-wide setting.

After source changes, stop the tester and repeat the build and start sequence. The fixtures do not start or stop the tester, download browsers, access an existing browser profile or persist sample changes. Default test runs skip them. A stopped tester, unavailable executable or missing Playwright browser is a setup failure when a fixture is explicitly selected.

The centrally pinned [Microsoft.Playwright package](https://www.nuget.org/packages/Microsoft.Playwright) supplies the .NET browser library. See the [official library guide](https://playwright.dev/dotnet/docs/library) for browser setup. Geometry samples use CSS zoom and `DeviceScaleFactor` to exercise fractional layout, which is separate from manually testing native browser zoom controls.

### Screen policy browser profiles

`ApplicationScreenPoliciesBrowserTests` verifies the real empty and permanent controls and attaches successful screenshots. Start the tester first with the chosen [launch profile](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md#screen-lifetime-profiles), then pass `BlazorTesterUrl` and `BrowserExecutablePath` in the browser runsettings.

- Default or `Empty singleton`: run `FullyQualifiedName~ApplicationScreenPoliciesBrowserTests.EmptyScreen`.
- `Empty instance`: run the same case. It verifies that the MultiInstance policy creates a new component and clears the previous draft.
- `Permanent singleton`: run `FullyQualifiedName~ApplicationScreenPoliciesBrowserTests.PermanentScreen` with the additional test parameter `<Parameter name="PermanentProfile" value="true" />`.

The ordinary browser suite targets the default profile. The permanent case is skipped unless its profile parameter is supplied, because a permanent normal screen intentionally prevents an empty workspace.

### Theme component browser coverage

`ThemeComponentsBrowserTests` runs the registered `/components/themes` gallery for every preset at desktop and mobile widths. Run it with the same browser runsettings:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --no-build --filter "FullyQualifiedName~ThemeComponentsBrowserTests" --settings obj/browser-tests/browser.runsettings
```

## Retained migration references

The superseded project files `Components/Sphere10.Framework.DApp.Presentation.Tests.csproj` and `Loader/Sphere10.Framework.DApp.Presentation.Loader.Tests.csproj` remain inactive migration references pending cleanup approval. The `.cs` files beside them compile into this project. Use the root test project shown in the commands above, which is the project included in solutions and CI.
