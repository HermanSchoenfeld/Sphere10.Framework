# Sphere10.Framework.Application.Tests

.NET 10 NUnit tests for the UI-independent application foundation in `Sphere10.Framework.Application.UI`. The project references only [Sphere10.Framework.Application](../../src/Sphere10.Framework.Application/README.md); it runs without a browser, ASP.NET Core host or Windows desktop runtime.

The suite protects the contracts shared by the Blazor and WinForms application frameworks: plugins, blocks, menus, parent ownership, metadata snapshots, builders, definition-owned activation metadata, actions, application interactions, host-owned block navigation, actual screen identity and guarded transitions. It also covers the application root's initialization, finalization, exit, logger and owned resources separately from process framework tasks. Small neutral classes stand in for the screen contracts and product factories supplied by a UI adapter.

## Run the tests

Install the .NET 10 SDK and run commands **from the repository root**. The test command restores and builds dependencies, then runs NUnit. `BuildRevision=0` supplies an explicit local build revision.

```powershell
dotnet test tests/Sphere10.Framework.Application.Tests/Sphere10.Framework.Application.Tests.csproj -c Release -p:BuildRevision=0
```

Run the metadata and builder fixtures while changing shared definitions:

```powershell
dotnet test tests/Sphere10.Framework.Application.Tests/Sphere10.Framework.Application.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~ApplicationMetadataTests|FullyQualifiedName~ApplicationBuilderTests"
```

Check the dependency boundary separately:

```powershell
dotnet test tests/Sphere10.Framework.Application.Tests/Sphere10.Framework.Application.Tests.csproj -c Release -p:BuildRevision=0 --filter "TestCategory=Architecture"
```

List discovered cases or save NUnit execution results through the standard test runner:

```powershell
dotnet test tests/Sphere10.Framework.Application.Tests/Sphere10.Framework.Application.Tests.csproj -c Release -p:BuildRevision=0 --list-tests
dotnet test tests/Sphere10.Framework.Application.Tests/Sphere10.Framework.Application.Tests.csproj -c Release -p:BuildRevision=0 --logger "trx;LogFileName=application-tests.trx" --results-directory obj/test-results/application
```

Quote combined filter expressions so the shell does not interpret `|`. Use `--no-build` only when the matching configuration has already been built with the current source.

## Fixture guide

| Fixture | What it protects |
| --- | --- |
| [UI/ApplicationPluginTests.cs](UI/ApplicationPluginTests.cs) | Startup callback/event ordering, plugin ownership validation, defensive arrays, typed block storage through object initializers, independent builders, loaded-plugin provenance and live application blocks. |
| [UI/ApplicationPluginRegistrationTests.cs](UI/ApplicationPluginRegistrationTests.cs) | Captured registration membership, original plugin/block identities, defensive receipt arrays, receipt validation and startup ordering without rereading mutable plugin blocks. |
| [UI/ApplicationPluginLifecycleRegistrationTests.cs](UI/ApplicationPluginLifecycleRegistrationTests.cs) | Plugin name required before all task helper overloads, deferred type/factory registrations, built-product name keys, independent transient tasks, factory scope, repeated builds and declaration order. |
| [UI/ApplicationLoadedPluginLifecycleTests.cs](UI/ApplicationLoadedPluginLifecycleTests.cs) | Cached task inventory from successful loaded plugins, application injection, independent scoped instances, host-owned versus plugin versus process registration, combined priority and finalizer order. |
| [ApplicationAggregateTests.cs](ApplicationAggregateTests.cs) | Shared running-application contract, defensive command membership, notifications and decorator forwarding. |
| [UI/ApplicationLifecycleTests.cs](UI/ApplicationLifecycleTests.cs) | Application task ownership, initialization and shutdown once, lazy keyed tasks with application injection, provider-disposal fallback, startup failure cleanup, owned resources, logger lifetime and decorator notifications. |
| [UI/ApplicationAsyncLifecycleTests.cs](UI/ApplicationAsyncLifecycleTests.cs) | Awaited task and resource completion, paired sync/async dispatch, pending-call coalescing, startup and shutdown cancellation, readiness retry, async disposal, failure cleanup and protected task inventories. |
| [UI/ApplicationLifecycleDirectionTests.cs](UI/ApplicationLifecycleDirectionTests.cs) | Abstract neutral lifecycle contracts, sync-first and async-first dispatch, blocking completion and original errors under a caller context, and direction-specific owned disposal. |
| [UI/ApplicationExitTests.cs](UI/ApplicationExitTests.cs) | Awaited confirmation, all retained screen guards, active block permissions, runtime application context, force escalation, concurrent requests, cancellation, retries, lifecycle cleanup and decorator dispatch. |
| [UI/ApplicationExitDirectionTests.cs](UI/ApplicationExitDirectionTests.cs) | Authoritative sync/async exit primitives and hooks, caller and worker contexts, awaited shutdown and owned cleanup, synchronous confirmation, and pending async-only guard veto, cancellation and forced override. |
| [UI/ApplicationExitFinalizationTests.cs](UI/ApplicationExitFinalizationTests.cs) | Pending cleanup under a synchronous caller context, finalizer self-exit rejection, and retry after overridden finalization rejects work before owned cleanup begins. |
| [Lifecycle/ApplicationTaskRunnerTests.cs](Lifecycle/ApplicationTaskRunnerTests.cs) | Existing initializer phases and finalizer order, task progress/logging, failure propagation and completing every shutdown task. |
| [Lifecycle/ApplicationAsyncTaskRunnerTests.cs](Lifecycle/ApplicationAsyncTaskRunnerTests.cs) | Sync-task adapters, awaited priority and parallel phases, aggregate failures, finalizer order and progress after completion. |
| [Lifecycle/ApplicationTaskBaseTests.cs](Lifecycle/ApplicationTaskBaseTests.cs) | Explicit dual task contracts, directional bases, worker execution, blocking awaited completion and errors, caller-context safety and the actual license initializer. |
| [Lifecycle/FrameworkFinalizationTests.cs](Lifecycle/FrameworkFinalizationTests.cs) | Once-only process shutdown tasks before deferred service disposal, failure cleanup, borrowed providers, restart and reentrant shutdown protection. |
| [Lifecycle/ApplicationTaskRegistrationTests.cs](Lifecycle/ApplicationTaskRegistrationTests.cs) | Keyed application tasks remaining separate from existing unkeyed process tasks and real framework startup/shutdown. |
| [UI/ApplicationCommandMergeTests.cs](UI/ApplicationCommandMergeTests.cs) | Application-level command protection, contextual block-to-screen replacement, callback identity, owner selection, menu extension, stable ordering, Help placement and separator normalization. |
| [UI/WizardTests.cs](UI/WizardTests.cs) | Navigation and branch updates, owned step arrays, cancellation, retry, concurrent terminal requests, completion-once and decorator forwarding. |
| [UI/ApplicationMetadataTests.cs](UI/ApplicationMetadataTests.cs) | Abstract block storage through `IApplicationMenu`, defensive membership arrays, independent snapshot graphs with canonical definition references and activators, malformed factories and toolbar commands, reference-based selection, ordering, copied subscriptions and typed-screen validation. |
| [UI/ApplicationBuilderTests.cs](UI/ApplicationBuilderTests.cs) | Independent products from shared builder factories, exact screen-definition references across caption configuration order, synchronous/asynchronous action replacement, screen/action exclusivity and incomplete-definition rejection. |
| [UI/SharedFluentBuilderTests.cs](UI/SharedFluentBuilderTests.cs) | Typed composition, callback order, canonical convenience-default inference, explicit overrides, independent owning graphs on repeated builds and wizard reset behavior. |
| [UI/ApplicationScreenDefinitionTests.cs](UI/ApplicationScreenDefinitionTests.cs) | Plugin/block reference order, exact default references, menu-independent definitions, empty selection, permanent lifetime classification and invalid policy combinations. |
| [UI/ScreenActivationPolicyTests.cs](UI/ScreenActivationPolicyTests.cs) | Pure definition validation, compatible aliases, conflicting modes/kinds, platform type validation, atomic host indexing, retained instance lifetime and invalid type/mode rejection. |
| [UI/ApplicationParentOwnershipTests.cs](UI/ApplicationParentOwnershipTests.cs) | Required writable ownership contracts, application/block/menu/item parents, persistent startup source, transfer guards, reverse activators, screen lifecycle attachment and removal, disposal and explicit command parent navigation. |
| [UI/ApplicationActionTests.cs](UI/ApplicationActionTests.cs) | Resolving services from the executing scope, awaited callback completion, cancellation and observable failures. |
| [UI/ApplicationScreenContractTests.cs](UI/ApplicationScreenContractTests.cs) | Default help metadata, navigation permission and cancellation-aware lifecycle methods without UI infrastructure. |
| [UI/ApplicationScreenHostTests.cs](UI/ApplicationScreenHostTests.cs) | Resolved actual screen hosting, singleton reuse, async guards and cancellation, atomic batch closure, hidden dirty screens, permanent/empty fallback, snapshots, selection notifications, interface/decorator dispatch and platform view ownership. |
| [UI/ApplicationScreenHostCapabilityTests.cs](UI/ApplicationScreenHostCapabilityTests.cs) | Application-to-host composition, single and multiple host interface boundaries, capability decorators, guarded current-screen closure and safe synchronous versus awaited runtime mode changes. |
| [UI/ApplicationUserInterfaceServicesTests.cs](UI/ApplicationUserInterfaceServicesTests.cs) | Reentrant menu actions through the borrowed host, executing UI-service identity, interaction notifications, host composition, lazy owned fallback services and independent DI-service lifetime. |
| [UI/ApplicationGenericContractTests.cs](UI/ApplicationGenericContractTests.cs) | Explicit typed application host/plugin/block/menu members projecting through the neutral interfaces; typed multiple-host decorator overrides and removable notifications. |
| [UI/UserInterfaceServicesDispatchTests.cs](UI/UserInterfaceServicesDispatchTests.cs) | Asynchronous interaction and UI dispatch through the shared interface and decorator without platform UI dependencies. |
| [UI/ApplicationScreenIdentityTests.cs](UI/ApplicationScreenIdentityTests.cs) | Actual screen reference identity, value-equal instances remaining distinct, rejection of unhosted references, dynamic metadata and an abstract API without IDs, renderer sessions or GUIDs. |
| [UI/ApplicationScreenHostNavigationTests.cs](UI/ApplicationScreenHostNavigationTests.cs) | Host-owned block selection and membership, configured defaults, immutable membership arrays, guarded permanent registration/removal and committed notifications. |
| [Architecture/ApplicationDependencyTests.cs](Architecture/ApplicationDependencyTests.cs) | Application assembly dependency closure remaining independent of UI assemblies. |

The shared `ApplicationBlockBase` stores `IApplicationMenu` directly, so different menu implementations can coexist. Concrete test models derive from the abstract storage foundations in [UI/TestApplicationModels.cs](UI/TestApplicationModels.cs); test construction does not require unused concrete framework shims. Collection-valued metadata properties return arrays. The tests mutate returned slots and verify that stored membership, parent links and host selection remain intact. Blocks own canonical `ApplicationScreenDefinition` references; commands, defaults and reverse activators refer to those exact objects within a graph. Snapshot factories clone that graph so independent products cannot reparent each other's commands. Presentation composition borrows references without changing their parents. Names and captions do not identify blocks or screens. Direct test screens and blocks implement the required writable ownership slots rather than relying on empty default implementations.

## Extend coverage without adding a UI dependency

Add a focused case beside the relevant fixture. Derive a small test builder from the shared builder base when testing factory behavior, or implement the shared screen contract directly when testing policies and lifecycle defaults.

For example, a screen with the default lifecycle needs no component or control base class:

```csharp
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ExampleScreenTests {
	[Test]
	public async Task DefaultScreenAllowsDeactivation() {
		IApplicationScreen screen = new ExampleScreen();
		Assert.That(await screen.CanDeactivateAsync(), Is.True);
	}

	private sealed class ExampleScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }
		public IApplicationBlock Block { get; set; }
	}
}
```

This illustrates the pattern already covered by `ApplicationScreenContractTests`; add new scenarios for changed behavior rather than duplicating that case. Include the repository license header in new files and follow [the NUnit test guidance](../../agents/skills/unit-testing/SKILL.md).

Use independent fixture state and controlled task completion for asynchronous behavior. Test invariants such as rejected definitions leaving the host's type index and retained screens unchanged, or a copied graph retaining its own parent and activator membership after the source changes. Keep actual component/control activation, UI dispatch, retained-screen rendering and platform disposal in the adapter test projects.

For plugin lifecycle changes, cover all four helper overloads and verify that an unnamed builder throws immediately without adding a registration. Use the actual built plugin name as the key; identical task types under different plugin names must remain independent. Separate unkeyed process tasks, `typeof(IApplication)` host-owned tasks and successfully loaded plugin tasks. Keep task arrays protected and verify once-only startup/finalization, cleanup after failures, and direction-specific dispatch through `Sync...Base` or `Async...Base`. Exit coverage must include refusal, retained screen and block guards, forced override, and cleanup completion before platform closure.

## Dependency and adapter validation

`ApplicationDependencyTests` traverses referenced Sphere10 assemblies from the Application assembly and rejects UI assembly families, including ASP.NET Core, JS interop, WinForms, WPF, MAUI and platform-specific framework projects. An `Architecture` failure should be investigated as a dependency boundary change; adding a UI reference to this test project is not a substitute for an adapter test.

The separate [NuGet consumer validation](../../scripts/package-consumers/README.md) checks the resolved package and framework references of an external Application-only consumer. It complements assembly-reference tests by examining the packaged dependency graph.

When shared contracts change, also run the [Blazor adapter suite](../Sphere10.Framework.Web.AspNetCore.Blazor.Tests/README.md):

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~SharedPresentationAdapterTests|FullyQualifiedName~ApplicationBlockTests"
```

On Windows, run the native adapter fixture as well:

```powershell
dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~ApplicationPresentationAdapterTests"
```

See [the Application library guide](../../src/Sphere10.Framework.Application/README.md), [the Blazor guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) and [the WinForms guide](../../src/Sphere10.Framework.Windows.Forms/README.md) for their respective integration patterns. Both solutions and CI include this shared suite.

If a filter matches no tests, inspect `--list-tests`; fixture namespaces do not necessarily mirror the `UI` folder. SDK and restore failures happen before NUnit execution, so check `dotnet --info` and the first reported build error before investigating a fixture.
